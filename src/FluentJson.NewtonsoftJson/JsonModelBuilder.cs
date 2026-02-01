using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentJson.Builders;
using FluentJson.Definitions;
using FluentJson.NewtonsoftJson.Converters;
using FluentJson.NewtonsoftJson.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace FluentJson.NewtonsoftJson;

public sealed class JsonModelBuilder : JsonModelBuilderBase<JsonSerializerSettings>
{
    private readonly Dictionary<Type, JsonConverter> _globalConverters = [];
    private readonly List<JsonConverter> _standardConverters = [];

    private readonly FluentContractResolver _resolver;
    private readonly JsonSerializerSettings _settings;

    public JsonModelBuilder()
    {
        _resolver = new FluentContractResolver();

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
        _settings.Formatting = Formatting.Indented;
        return this;
    }

    public JsonModelBuilder UseMinification()
    {
        EnsureNotBuilt();
        _settings.Formatting = Formatting.None;
        return this;
    }

    #endregion

    #region Converters

    public void ApplyConvertersFromAssemblies(params Assembly[] assemblies)
        => ApplyConvertersFromAssemblies(null, assemblies);

    public void ApplyConvertersFromAssemblies(Func<Type, object>? serviceFactory, params Assembly[] assemblies)
    {
        EnsureNotBuilt();
        foreach (Assembly assembly in assemblies)
        {
            IEnumerable<Type> types = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && typeof(JsonConverter).IsAssignableFrom(t));

            foreach (Type type in types)
            {
                JsonConverter? instance = serviceFactory != null
                    ? (JsonConverter?)serviceFactory(type)
                    : (JsonConverter?)Activator.CreateInstance(type);

                if (instance is null)
                {
                    continue;
                }

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

    protected override JsonSerializerSettings BuildEngineSettings(IServiceProvider? serviceProvider)
    {
        _resolver.SetServiceProvider(serviceProvider);

        _resolver.NamingStrategy = _namingConvention switch
        {
            NamingConvention.CamelCase => new CamelCaseNamingStrategy(),
            NamingConvention.SnakeCase => new SnakeCaseNamingStrategy(),
            _ => null
        };

        foreach (KeyValuePair<Type, JsonEntityDefinition> kvp in _scannedDefinitions)
        {
            JsonEntityDefinition def = kvp.Value;

            _resolver.RegisterDefinition(kvp.Key, def);

            if (def.Polymorphism != null)
            {
                foreach (KeyValuePair<Type, object> sub in def.Polymorphism.SubTypes)
                {
                    _resolver.RegisterDiscriminatorValue(sub.Key, def.Polymorphism.DiscriminatorProperty, sub.Value);
                }

                _settings.Converters.Add(new PolymorphicJsonConverter(kvp.Key, def.Polymorphism));
            }
        }

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
