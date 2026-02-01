using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using FluentJson.Abstractions;
using FluentJson.Builders;
using FluentJson.Definitions;
using FluentJson.Internal;
using FluentJson.SystemTextJson.Converters;
using System.Collections.Concurrent;

namespace FluentJson.SystemTextJson;

/// <summary>
/// A fluent builder for configuring and creating a System.Text.Json <see cref="JsonSerializerOptions"/> object.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Builder / Modifier Strategy.
/// </para>
/// <para>
/// This class configures the System.Text.Json engine using the modern <see cref="IJsonTypeInfoResolver"/> API (available since .NET 7).
/// Instead of writing a custom resolver from scratch, it hooks into the <c>DefaultJsonTypeInfoResolver.Modifiers</c> chain 
/// to apply fluent configurations (renaming, private field access, converters) dynamically during the first serialization pass.
/// </para>
/// </remarks>
public class JsonModelBuilder : JsonModelBuilderBase<JsonSerializerOptions>
{
    private readonly JsonSerializerOptions _options;
    private static readonly ConcurrentDictionary<Type, JsonConverter> _converterInstanceCache = new();
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
    /// <returns>The builder instance.</returns>
    public JsonModelBuilder UseCamelCaseNamingConvention()
    {
        EnsureNotBuilt();
        _namingConvention = NamingConvention.CamelCase;
        return this;
    }

    /// <summary>
    /// Configures the serializer to use snake_case naming (e.g., "first_name").
    /// </summary>
    /// <returns>The builder instance.</returns>
    public JsonModelBuilder UseSnakeCaseNamingConvention()
    {
        EnsureNotBuilt();
        _namingConvention = NamingConvention.SnakeCase;
        return this;
    }

    /// <summary>
    /// Enables pretty-printing (indented JSON).
    /// </summary>
    /// <returns>The builder instance.</returns>
    public JsonModelBuilder UsePrettyPrinting()
    {
        EnsureNotBuilt();
        _options.WriteIndented = true;
        return this;
    }

    #endregion

    /// <summary>
    /// Finalizes the configuration and returns the ready-to-use <see cref="JsonSerializerOptions"/>.
    /// </summary>
    /// <remarks>
    /// This method registers the <c>ModifyTypeInfo</c> callback, validates all definitions, and freezes the configuration to ensure thread safety.
    /// </remarks>
    /// <returns>The configured options.</returns>
    public override JsonSerializerOptions Build()
    {
        if (_isBuilt) return _options;

        // 1. Configure Global Naming Policy
        _options.PropertyNamingPolicy = _namingConvention switch
        {
            NamingConvention.CamelCase => JsonNamingPolicy.CamelCase,
            NamingConvention.SnakeCase => JsonNamingPolicy.SnakeCaseLower,
            _ => null
        };

        // 2. Register the Modifier Strategy
        // This is the core of the STJ integration: we inject our logic into the type resolution pipeline.
        _options.TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { ModifyTypeInfo }
        };

        // 3. Validation and Freezing
        foreach (KeyValuePair<Type, JsonEntityDefinition> kvp in _scannedDefinitions)
        {
            ModelValidator.ValidateDefinition(kvp.Key, kvp.Value);
            kvp.Value.Freeze();
        }

        _isBuilt = true;
        return _options;
    }

    // --- SYSTEM.TEXT.JSON MODIFIER CORE ---

    /// <summary>
    /// The callback method invoked by STJ for every type it encounters.
    /// This is where we inject our custom metadata (definitions) into the STJ model.
    /// </summary>
    /// <param name="typeInfo">The metadata container provided by STJ.</param>
    private void ModifyTypeInfo(JsonTypeInfo typeInfo)
    {
        // Skip types that haven't been configured via FluentJson
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
                // user has explicitly configured un int
                // we force STJ to expect a number in json
                typeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(subType.Key, intVal));
            }
            else
            {
                // for others we use to string
                // HasSubType<B>("b") or HasSubType<C>(MyEnum.C)
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
        // We only replace STJ's native accessors if we have a specific reason to do so:
        // - A BackingField is configured (redirection required).
        // - STJ failed to generate a getter/setter (e.g. non-public property).
        // Otherwise, we keep STJ's optimized IL for standard properties.
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
    /// </summary>
    private void ApplyConverter(JsonPropertyInfo jsonProp, IConverterDefinition converterDef)
    {
        if (converterDef is TypeConverterDefinition typeDef)
        {
            // ARCHITECTURAL NOTE:
            // We use GetOrAdd to implement the Flyweight pattern.
            // Since standard JsonConverters are typically stateless, we can reuse 
            // the same instance across the entire application to reduce memory pressure.
            JsonConverter converter = _converterInstanceCache.GetOrAdd(typeDef.ConverterType, static t =>
            {
                // Validation has already been done in Core (ModelValidator).
                // We can safely assume the constructor exists.
                return (JsonConverter)Activator.CreateInstance(t)!;
            });

            jsonProp.CustomConverter = converter;
        }
        else if (converterDef is LambdaConverterDefinition lambdaDef)
        {
            // NOTE: We cannot cache these instances globally because they contain 
            // specific delegates (closures) unique to this property configuration.

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
