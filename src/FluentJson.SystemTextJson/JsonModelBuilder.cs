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

/// <summary>
/// A fluent builder for configuring and creating a System.Text.Json <see cref="JsonSerializerOptions"/> object.
/// Leverages the <see cref="IJsonTypeInfoResolver"/> modifiers to inject configuration into the STJ pipeline.
/// </summary>
public class JsonModelBuilder : JsonModelBuilderBase<JsonSerializerOptions>
{
    private readonly JsonSerializerOptions _options;

    // Instance-level cache to support DI scopes and prevent leakage between different builder instances.
    private readonly ConcurrentDictionary<Type, JsonConverter> _converterInstanceCache = new();

    // Captured provider to resolve dependencies inside the Modifier callback.
    private IServiceProvider? _runtimeServiceProvider;

    /// <summary>
    /// Initializes a new instance of the builder with default modern options.
    /// </summary>
    public JsonModelBuilder()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // Defaults to PascalCase (null), overridden by UseCamelCase...
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            IncludeFields = false
        };
    }

    #region Fluent API Wrappers (Chaining Support)

    /// <summary>
    /// Configures the serializer to use camelCase naming (e.g., "firstName").
    /// </summary>
    public JsonModelBuilder UseCamelCaseNamingConvention()
    {
        EnsureNotBuilt();
        _namingConvention = NamingConvention.CamelCase;
        return this;
    }

    /// <summary>
    /// Configures the serializer to use snake_case naming (e.g., "first_name").
    /// </summary>
    public JsonModelBuilder UseSnakeCaseNamingConvention()
    {
        EnsureNotBuilt();
        _namingConvention = NamingConvention.SnakeCase;
        return this;
    }

    /// <summary>
    /// Enables pretty-printing (indented JSON).
    /// </summary>
    public JsonModelBuilder UsePrettyPrinting()
    {
        EnsureNotBuilt();
        _options.WriteIndented = true;
        return this;
    }

    #endregion

    /// <summary>
    /// Implementation of the Template Method hook.
    /// Configures the System.Text.Json options using the frozen definitions.
    /// </summary>
    /// <param name="serviceProvider">The provider for resolving runtime dependencies (e.g., Converters).</param>
    /// <returns>The configured JsonSerializerOptions.</returns>
    protected override JsonSerializerOptions BuildEngineSettings(IServiceProvider? serviceProvider)
    {
        // Capture provider for the ModifyTypeInfo callback
        _runtimeServiceProvider = serviceProvider;

        // 1. Configure Global Naming Policy
        _options.PropertyNamingPolicy = _namingConvention switch
        {
            NamingConvention.CamelCase => JsonNamingPolicy.CamelCase,
            NamingConvention.SnakeCase => JsonNamingPolicy.SnakeCaseLower,
            _ => null
        };

        // 2. Register the Modifier Strategy
        // We hook into the TypeInfoResolver to apply our configurations dynamically.
        _options.TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { ModifyTypeInfo }
        };

        return _options;
    }
    /// <summary>
    /// The callback method invoked by STJ for every type it encounters.
    /// Injects FluentJson metadata (definitions) into the STJ model.
    /// </summary>
    private void ModifyTypeInfo(JsonTypeInfo typeInfo)
    {
        // Skip types that haven't been configured via FluentJson
        // _scannedDefinitions is accessible from the base class
        if (!_scannedDefinitions.TryGetValue(typeInfo.Type, out JsonEntityDefinition? def))
        {
            return;
        }

        // Apply Polymorphism Settings (Native STJ Support)
        if (def.Polymorphism != null)
        {
            ConfigurePolymorphism(typeInfo, def.Polymorphism);
        }

        // Apply Property Settings
        foreach (JsonPropertyInfo jsonProp in typeInfo.Properties)
        {
            // Link STJ property to our definition via Reflection metadata
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

    /// <summary>
    /// Maps FluentJson polymorphism definitions to STJ's <see cref="JsonPolymorphismOptions"/>.
    /// </summary>
    private void ConfigurePolymorphism(JsonTypeInfo typeInfo, PolymorphismDefinition polyDef)
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
                // Ensure culture-invariant string conversion for discriminators
                string strVal = Convert.ToString(subType.Value, System.Globalization.CultureInfo.InvariantCulture)!;
                typeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(subType.Key, strVal));
            }
        }
    }

    /// <summary>
    /// Maps individual property configurations (renaming, ignoring, converters, private fields).
    /// </summary>
    private void ApplyPropertyConfiguration(JsonPropertyInfo jsonProp, JsonPropertyDefinition def)
    {
        // 1. Ignore
        if (def.Ignored)
        {
            // Conditional serialization delegate that always returns false
            jsonProp.ShouldSerialize = static (obj, val) => false;
            return;
        }

        // 2. Metadata Overrides
        if (def.JsonName != null) jsonProp.Name = def.JsonName;
        if (def.Order.HasValue) jsonProp.Order = def.Order.Value;
        if (def.IsRequired.HasValue && def.IsRequired.Value) jsonProp.IsRequired = true;

        // 3. Converters
        if (def.ConverterDefinition != null)
        {
            ApplyConverter(jsonProp, def.ConverterDefinition);
        }

        // 4. Performance & Private Field Access
        // We replace STJ's native accessors only if necessary (e.g. BackingField or non-public property).
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

    /// <summary>
    /// Instantiates and assigns the appropriate JsonConverter to the property.
    /// Handles Dependency Injection if a provider is available.
    /// </summary>
    private void ApplyConverter(JsonPropertyInfo jsonProp, IConverterDefinition converterDef)
    {
        if (converterDef is TypeConverterDefinition typeDef)
        {
            // Use GetOrAdd to reuse instances within this Options context.
            JsonConverter converter = _converterInstanceCache.GetOrAdd(typeDef.ConverterType, t =>
            {
                // 1. Try DI Resolution
                if (_runtimeServiceProvider != null)
                {
                    try
                    {
                        var service = _runtimeServiceProvider.GetService(t);
                        if (service is JsonConverter diConverter) return diConverter;
                    }
                    catch (Exception ex)
                    {
                        throw new FluentJsonConfigurationException(
                            $"DI Resolution failed for converter '{t.Name}'.", ex);
                    }
                }

                // 2. Fallback to Activator
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
            // Lambda converters are specific to the property/types and cannot be easily cached globally.
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
