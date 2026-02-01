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
        // 1. Direct Definition (Base Class or specific config)
        if (_scannedDefinitions.TryGetValue(typeInfo.Type, out JsonEntityDefinition? def))
        {
            ApplyDefinition(typeInfo, def);
        }

        // 2. Inheritance Logic (Find Polymorphism configuration in hierarchy)
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
            // A. Suppress Serialization of the Discriminator Property (avoid duplication)
            foreach (JsonPropertyInfo jsonProp in typeInfo.Properties)
            {
                if (jsonProp.Name == polyDef.DiscriminatorProperty)
                {
                    jsonProp.ShouldSerialize = static (obj, val) => false;
                }
            }

            // B. Fix Deserialization: Force the discriminator value on the object
            object? discriminatorValue = null;
            foreach (KeyValuePair<Type, object> kvp in polyDef.SubTypes)
            {
                if (kvp.Key == typeInfo.Type)
                {
                    discriminatorValue = kvp.Value;
                    break;
                }
            }

            if (discriminatorValue != null && typeInfo.CreateObject != null)
            {
                Func<object>? originalFactory = typeInfo.CreateObject;
                string discriminatorPropName = polyDef.DiscriminatorProperty;

                // Wrap factory to inject value immediately after creation
                typeInfo.CreateObject = () =>
                {
                    object obj = originalFactory();

                    PropertyInfo? prop = typeInfo.Type.GetProperty(discriminatorPropName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (prop != null && prop.CanWrite)
                    {
                        try
                        {
                            prop.SetValue(obj, discriminatorValue);
                        }
                        catch { /* Ignore conversion errors */ }
                    }

                    return obj;
                };
            }
        }
    }

    private void ApplyDefinition(JsonTypeInfo typeInfo, JsonEntityDefinition def)
    {
        if (def.Polymorphism != null)
        {
            JsonModelBuilder.ConfigurePolymorphism(typeInfo, def.Polymorphism);
        }

        foreach (JsonPropertyInfo jsonProp in typeInfo.Properties)
        {
            if (jsonProp.AttributeProvider is not MemberInfo member)
            {
                continue;
            }

            if (def.Properties.TryGetValue(member, out JsonPropertyDefinition? propDef))
            {
                ApplyPropertyConfiguration(jsonProp, propDef);
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
            if (subType.Value is int intVal)
            {
                typeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(subType.Key, intVal));
            }
            else
            {
                string strVal = Convert.ToString(subType.Value, System.Globalization.CultureInfo.InvariantCulture)!;
                typeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(subType.Key, strVal));
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

        if (def.JsonName != null)
        {
            jsonProp.Name = def.JsonName;
        }

        if (def.Order.HasValue)
        {
            jsonProp.Order = def.Order.Value;
        }

        if (def.IsRequired.HasValue && def.IsRequired.Value)
        {
            jsonProp.IsRequired = true;
        }

        if (def.ConverterDefinition != null)
        {
            ApplyConverter(jsonProp, def.ConverterDefinition);
        }

        MemberInfo targetMember = (MemberInfo?)def.BackingField ?? def.Member;
        bool needsRedirect = def.BackingField != null;

        if (needsRedirect || jsonProp.Get == null)
        {
            jsonProp.Get = AccessorFactory.CreateGetter(targetMember);
        }

        if (needsRedirect || jsonProp.Set == null)
        {
            jsonProp.Set = AccessorFactory.CreateSetter(targetMember);
        }
    }

    private void ApplyConverter(JsonPropertyInfo jsonProp, IConverterDefinition converterDef)
    {
        if (converterDef is TypeConverterDefinition typeDef)
        {
            JsonConverter converter = _converterInstanceCache.GetOrAdd(typeDef.ConverterType, t =>
            {
                if (_runtimeServiceProvider != null)
                {
                    try
                    {
                        object? service = _runtimeServiceProvider.GetService(t);
                        if (service is JsonConverter diConverter)
                        {
                            return diConverter;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new FluentJsonConfigurationException(
                            $"DI Resolution failed for converter '{t.Name}'.", ex);
                    }
                }

                try
                {
                    return (JsonConverter)Activator.CreateInstance(t)!;
                }
                catch (Exception ex)
                {
                    throw new FluentJsonConfigurationException(
                        $"Failed to instantiate converter '{t.Name}'. Ensure it has a parameterless constructor or register it in DI.", ex);
                }
            });

            jsonProp.CustomConverter = converter;
        }
        else if (converterDef is LambdaConverterDefinition lambdaDef)
        {
            Type converterType = typeof(LambdaJsonConverter<,>)
                .MakeGenericType(lambdaDef.ModelType, lambdaDef.JsonType);

            object converter = Activator.CreateInstance(
                converterType,
                lambdaDef.ConvertToDelegate,
                lambdaDef.ConvertFromDelegate)!;

            jsonProp.CustomConverter = (JsonConverter)converter;
        }
    }
}
