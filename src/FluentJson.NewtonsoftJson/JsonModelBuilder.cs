using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentJson.Builders;
using FluentJson.Definitions;
using FluentJson.NewtonsoftJson.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace FluentJson.NewtonsoftJson;

/// <summary>
/// A fluent builder for configuring and creating a Newtonsoft.Json <see cref="JsonSerializerSettings"/> object.
/// Adapts the agnostic configuration model to the Newtonsoft engine.
/// </summary>
public sealed class JsonModelBuilder : JsonModelBuilderBase<JsonSerializerSettings>
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
    public JsonModelBuilder UseCamelCaseNamingConvention()
    {
        EnsureNotBuilt();
        _namingConvention = NamingConvention.CamelCase;
        return this;
    }

    /// <summary>
    /// Configures the serializer to use snake_case naming (e.g., "first_name") for all properties.
    /// </summary>
    public JsonModelBuilder UseSnakeCaseNamingConvention()
    {
        EnsureNotBuilt();
        _namingConvention = NamingConvention.SnakeCase;
        return this;
    }

    /// <summary>
    /// Enables indented formatting (pretty printing) for the JSON output.
    /// </summary>
    public JsonModelBuilder UsePrettyPrinting()
    {
        EnsureNotBuilt();
        _settings.Formatting = Formatting.Indented;
        return this;
    }

    /// <summary>
    /// Disables formatting to produce compact (minified) JSON output.
    /// </summary>
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
    public void ApplyConvertersFromAssemblies(params Assembly[] assemblies)
        => ApplyConvertersFromAssemblies(null, assemblies);

    /// <summary>
    /// Scans assemblies for converters, using an optional service provider for instantiation.
    /// </summary>
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

                if (instance is null)
                {
                    continue;
                }

                // Auto-detection: Does this converter target a specific generic type? (e.g., JsonConverter<T>)
                Type? targetType = GetTargetTypeFromJsonConverter(type);
                if (targetType != null)
                {
                    HasConversion(targetType, instance);
                }
                else
                {
                    _standardConverters.Add(instance);
                }
            }
        }
    }

    /// <summary>
    /// Registers a specific global converter for a target type.
    /// </summary>
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
            {
                return currentType.GetGenericArguments()[0];
            }

            currentType = currentType.BaseType;
        }
        return null;
    }

    #endregion

    /// <summary>
    /// Implementation of the Template Method hook.
    /// Configures the Newtonsoft-specific settings using the frozen definitions from the base class.
    /// </summary>
    /// <param name="serviceProvider">The DI provider (not fully utilized in this adapter version yet).</param>
    /// <returns>The configured JsonSerializerSettings.</returns>
    protected override JsonSerializerSettings BuildEngineSettings(IServiceProvider? serviceProvider)
    {
        // 1. Configure Naming Strategy
        _resolver.NamingStrategy = _namingConvention switch
        {
            NamingConvention.CamelCase => new CamelCaseNamingStrategy(),
            NamingConvention.SnakeCase => new SnakeCaseNamingStrategy(),
            _ => null
        };

        // 2. Register Definitions (Validated and Frozen by Base)
        // _scannedDefinitions is accessible from the base class
        foreach (KeyValuePair<Type, JsonEntityDefinition> kvp in _scannedDefinitions)
        {
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
            {
                _settings.Converters.Add(conv);
            }
        }

        return _settings;
    }
}
