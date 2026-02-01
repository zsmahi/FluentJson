using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using FluentJson.Abstractions;
using FluentJson.Builders;
using FluentJson.Definitions;
using FluentJson.Exceptions;
using FluentJson.Internal;
using FluentJson.SystemTextJson.Converters;

namespace FluentJson.SystemTextJson;

public class JsonModelBuilder : JsonModelBuilderBase<JsonSerializerOptions>
{
    private readonly JsonSerializerOptions _options;
    private readonly ConcurrentDictionary<Type, JsonConverter> _converterInstanceCache = new();
    private IServiceProvider? _runtimeServiceProvider;

    public JsonModelBuilder()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            IncludeFields = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    #region Fluent API Wrappers

    public JsonModelBuilder UseCamelCaseNamingConvention()
    {
        EnsureNotBuilt();
        _namingConvention = NamingConvention.CamelCase;
        return this;
    }

    public JsonModelBuilder UseSnakeCaseNamingConvention()
    {
        EnsureNotBuilt();
        _namingConvention = NamingConvention.SnakeCase;
        return this;
    }

    public JsonModelBuilder UsePrettyPrinting()
    {
        EnsureNotBuilt();
        _options.WriteIndented = true;
        return this;
    }

    #endregion

    protected override JsonSerializerOptions BuildEngineSettings(IServiceProvider? serviceProvider)
    {
        _runtimeServiceProvider = serviceProvider;

        _options.PropertyNamingPolicy = _namingConvention switch
        {
            NamingConvention.CamelCase => JsonNamingPolicy.CamelCase,
            NamingConvention.SnakeCase => JsonNamingPolicy.SnakeCaseLower,
            _ => null
        };

        _options.TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { ModifyTypeInfo }
        };

        return _options;
    }

    private void ModifyTypeInfo(JsonTypeInfo typeInfo)
    {
        // 1. Handle Polymorphism ONLY if configured for this specific type.
        // STJ throws if we add siblings as derived types to a non-base contract.
        if (_scannedDefinitions.TryGetValue(typeInfo.Type, out JsonEntityDefinition? selfDef) && selfDef.Polymorphism != null)
        {
            ConfigurePolymorphism(typeInfo, selfDef.Polymorphism);
        }

        // 2. Aggregate property definitions from the hierarchy.
        // We scan from Base to Derived so that derived overrides take precedence.
        var currentType = typeInfo.Type;
        var hierarchy = new Stack<JsonEntityDefinition>();

        while (currentType != null && currentType != typeof(object))
        {
            if (_scannedDefinitions.TryGetValue(currentType, out JsonEntityDefinition? def))
            {
                hierarchy.Push(def);
            }
            currentType = currentType.BaseType;
        }

        while (hierarchy.Count > 0)
        {
            ApplyProperties(typeInfo, hierarchy.Pop());
        }

        // 3. Special handling for standard/shadow discriminator behavior
        ApplyPolymorphismLogic(typeInfo);
    }

    private void ApplyProperties(JsonTypeInfo typeInfo, JsonEntityDefinition def)
    {
        foreach (JsonPropertyInfo jsonProp in typeInfo.Properties)
        {
            if (jsonProp.AttributeProvider is not MemberInfo member) continue;

            if (def.Properties.TryGetValue(member, out JsonPropertyDefinition? propDef))
            {
                ApplyPropertyConfiguration(jsonProp, propDef);
            }
        }
    }

    private void ApplyPolymorphismLogic(JsonTypeInfo typeInfo)
    {
        Type? currentBase = typeInfo.Type.BaseType;
        PolymorphismDefinition? polyDef = null;

        while (currentBase != null && currentBase != typeof(object))
        {
            if (_scannedDefinitions.TryGetValue(currentBase, out JsonEntityDefinition? baseDef) && baseDef.Polymorphism != null)
            {
                polyDef = baseDef.Polymorphism;
                break;
            }
            currentBase = currentBase.BaseType;
        }

        if (polyDef != null)
        {
            // A. Suppress discriminator property serialization (avoid metadata double-write)
            foreach (JsonPropertyInfo jsonProp in typeInfo.Properties)
            {
                if (jsonProp.Name == polyDef.DiscriminatorProperty)
                {
                    jsonProp.ShouldSerialize = static (obj, val) => false;
                }
            }

            // B. Inject discriminator value after creation (fix STJ token consumption)
            object? discriminatorValue = null;
            foreach (var kvp in polyDef.SubTypes)
            {
                if (kvp.Key == typeInfo.Type)
                {
                    discriminatorValue = kvp.Value;
                    break;
                }
            }

            if (discriminatorValue != null && typeInfo.CreateObject != null)
            {
                Func<object> originalFactory = typeInfo.CreateObject;
                string propName = polyDef.DiscriminatorProperty;

                typeInfo.CreateObject = () =>
                {
                    object obj = originalFactory();
                    PropertyInfo? prop = typeInfo.Type.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (prop != null && prop.CanWrite) prop.SetValue(obj, discriminatorValue);
                    return obj;
                };
            }
        }
    }

    private static void ConfigurePolymorphism(JsonTypeInfo typeInfo, PolymorphismDefinition polyDef)
    {
        typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
        {
            TypeDiscriminatorPropertyName = polyDef.DiscriminatorProperty,
            IgnoreUnrecognizedTypeDiscriminators = false,
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization
        };

        foreach (KeyValuePair<Type, object> subType in polyDef.SubTypes)
        {
            // JsonDerivedType supports both string and int discriminators via overloads.
            // We resolve the correct overload by checking the underlying value type.
            if (subType.Value is int intDiscriminator)
            {
                typeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(subType.Key, intDiscriminator));
            }
            else
            {
                string stringDiscriminator = Convert.ToString(subType.Value, System.Globalization.CultureInfo.InvariantCulture)!;
                typeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(subType.Key, stringDiscriminator));
            }
        }
    }

    private void ApplyPropertyConfiguration(JsonPropertyInfo jsonProp, JsonPropertyDefinition def)
    {
        if (def.Ignored)
        {
            jsonProp.ShouldSerialize = static (obj, val) => false;
            return;
        }

        if (def.JsonName != null) jsonProp.Name = def.JsonName;
        if (def.Order.HasValue) jsonProp.Order = def.Order.Value;
        if (def.IsRequired.HasValue && def.IsRequired.Value) jsonProp.IsRequired = true;

        if (def.ConverterDefinition != null) ApplyConverter(jsonProp, def.ConverterDefinition);

        MemberInfo targetMember = (MemberInfo?)def.BackingField ?? def.Member;
        bool needsRedirect = def.BackingField != null;

        if (needsRedirect || jsonProp.Get == null) jsonProp.Get = AccessorFactory.CreateGetter(targetMember);
        if (needsRedirect || jsonProp.Set == null) jsonProp.Set = AccessorFactory.CreateSetter(targetMember);
    }

    private void ApplyConverter(JsonPropertyInfo jsonProp, IConverterDefinition converterDef)
    {
        if (converterDef is TypeConverterDefinition typeDef)
        {
            jsonProp.CustomConverter = _converterInstanceCache.GetOrAdd(typeDef.ConverterType, t =>
            {
                if (_runtimeServiceProvider != null)
                {
                    var service = _runtimeServiceProvider.GetService(t);
                    if (service is JsonConverter conv) return conv;
                }
                return (JsonConverter)Activator.CreateInstance(t)!;
            });
        }
        else if (converterDef is LambdaConverterDefinition lambdaDef)
        {
            Type converterType = typeof(LambdaJsonConverter<,>).MakeGenericType(lambdaDef.ModelType, lambdaDef.JsonType);
            jsonProp.CustomConverter = (JsonConverter)Activator.CreateInstance(converterType, lambdaDef.ConvertToDelegate, lambdaDef.ConvertFromDelegate)!;
        }
    }
}
