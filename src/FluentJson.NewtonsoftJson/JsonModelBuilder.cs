using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentJson.Builders;
using FluentJson.Definitions;
using FluentJson.Internal;
using FluentJson.NewtonsoftJson.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace FluentJson.NewtonsoftJson;

/// <summary>
/// A fluent builder for configuring and creating a Newtonsoft.Json <see cref="JsonSerializerSettings"/> object.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Builder / Adapter.
/// </para>
/// <para>
/// This class serves as the public API entry point for the Newtonsoft.Json integration. It extends the core 
/// <see cref="JsonModelBuilderBase{T}"/> to leverage shared discovery logic, while adding Newtonsoft-specific 
/// features like global <see cref="JsonConverter"/> registration and naming strategies.
/// </para>
/// </remarks>
public class JsonModelBuilder : JsonModelBuilderBase<JsonSerializerSettings>
{
    // Specific storage for global Newtonsoft converters
    private readonly Dictionary<Type, JsonConverter> _globalConverters = [];
    private readonly List<JsonConverter> _standardConverters = [];

    private readonly FluentContractResolver _resolver;
    private readonly JsonSerializerSettings _settings;

    /// <summary>
    /// Initializes a new instance of the builder with default settings.
    /// </summary>
    public JsonModelBuilder()
    {
        _resolver = new FluentContractResolver();

        // Initialize with sensible defaults for modern APIs
        _settings = new JsonSerializerSettings
        {
            ContractResolver = _resolver,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Include,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };
    }

    #region Fluent API Wrappers (Chaining Support)

    /// <summary>
    /// Configures the serializer to use camelCase naming (e.g., "firstName") for all properties.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public JsonModelBuilder UseCamelCaseNamingConvention()
    {
        EnsureNotBuilt();
        _namingConvention = NamingConvention.CamelCase;
        return this;
    }

    /// <summary>
    /// Configures the serializer to use snake_case naming (e.g., "first_name") for all properties.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public JsonModelBuilder UseSnakeCaseNamingConvention()
    {
        EnsureNotBuilt();
        _namingConvention = NamingConvention.SnakeCase;
        return this;
    }

    /// <summary>
    /// Enables indented formatting (pretty printing) for the JSON output.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public JsonModelBuilder UsePrettyPrinting()
    {
        EnsureNotBuilt();
        _settings.Formatting = Formatting.Indented;
        return this;
    }

    /// <summary>
    /// Disables formatting to produce compact (minified) JSON output.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public JsonModelBuilder UseMinification()
    {
        EnsureNotBuilt();
        _settings.Formatting = Formatting.None;
        return this;
    }

    #endregion

    #region Converters (Newtonsoft Specifics)

    /// <summary>
    /// Scans the specified assemblies for <see cref="JsonConverter"/> implementations and registers them.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    public void ApplyConvertersFromAssemblies(params Assembly[] assemblies)
        => ApplyConvertersFromAssemblies(null, assemblies);

    /// <summary>
    /// Scans assemblies for converters, using an optional service provider for instantiation.
    /// </summary>
    /// <param name="serviceFactory">A delegate to resolve converter instances (e.g., via DI container).</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    public void ApplyConvertersFromAssemblies(Func<Type, object>? serviceFactory, params Assembly[] assemblies)
    {
        EnsureNotBuilt();
        foreach (Assembly assembly in assemblies)
        {
            IEnumerable<Type> types = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && typeof(JsonConverter).IsAssignableFrom(t));

            foreach (Type type in types)
            {
                // Instantiate via Factory or Activator
                JsonConverter? instance = serviceFactory != null
                    ? (JsonConverter)serviceFactory(type)
                    : (JsonConverter?)Activator.CreateInstance(type);

                if (instance == null) continue;

                // Auto-detection: Does this converter target a specific generic type? (e.g., JsonConverter<T>)
                Type? targetType = GetTargetTypeFromJsonConverter(type);
                if (targetType != null)
                    HasConversion(targetType, instance);
                else
                    _standardConverters.Add(instance);
            }
        }
    }

    /// <summary>
    /// Registers a specific global converter for a target type.
    /// </summary>
    /// <param name="type">The type that this converter handles.</param>
    /// <param name="converter">The converter instance.</param>
    public void HasConversion(Type type, JsonConverter converter)
    {
        EnsureNotBuilt();
        _globalConverters[type] = converter;
    }

    private Type? GetTargetTypeFromJsonConverter(Type type)
    {
        Type? currentType = type;
        while (currentType != null && currentType != typeof(object))
        {
            if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(JsonConverter<>))
                return currentType.GetGenericArguments()[0];
            currentType = currentType.BaseType;
        }
        return null;
    }

    #endregion

    /// <summary>
    /// Finalizes the configuration and returns the ready-to-use <see cref="JsonSerializerSettings"/>.
    /// </summary>
    /// <remarks>
    /// This method freezes all definitions to ensure thread safety and configures the internal <see cref="FluentContractResolver"/>.
    /// </remarks>
    /// <returns>The configured settings object.</returns>
    public override JsonSerializerSettings Build()
    {
        if (_isBuilt) return _settings;

        // 1. Configure Naming Strategy
        _resolver.NamingStrategy = _namingConvention switch
        {
            NamingConvention.CamelCase => new CamelCaseNamingStrategy(),
            NamingConvention.SnakeCase => new SnakeCaseNamingStrategy(),
            _ => null
        };

        // 2. Process and Validate Scanned Definitions
        foreach (KeyValuePair<Type, JsonEntityDefinition> kvp in _scannedDefinitions)
        {
            // Validate consistency (fail-fast)
            ModelValidator.ValidateDefinition(kvp.Key, kvp.Value);

            // Lock the definition for thread safety
            kvp.Value.Freeze();

            // Register with the resolver
            _resolver.RegisterDefinition(kvp.Key, kvp.Value);

            // Register polymorphic discriminator values
            if (kvp.Value.Polymorphism != null)
            {
                foreach (KeyValuePair<Type, object> sub in kvp.Value.Polymorphism.SubTypes)
                {
                    _resolver.RegisterDiscriminatorValue(sub.Key, kvp.Value.Polymorphism.DiscriminatorProperty, sub.Value);
                }
            }
        }

        // 3. Register Converters
        _resolver.RegisterGlobalConverters(_globalConverters);

        if (_standardConverters.Count > 0)
        {
            _settings.Converters ??= [];
            foreach (JsonConverter conv in _standardConverters)
                _settings.Converters.Add(conv);
        }

        _isBuilt = true;
        return _settings;
    }
}
