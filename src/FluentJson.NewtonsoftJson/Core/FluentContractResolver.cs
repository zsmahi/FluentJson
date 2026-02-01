using FluentJson.Definitions;
using FluentJson.NewtonsoftJson.Converters;
using FluentJson.NewtonsoftJson.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FluentJson.NewtonsoftJson.Core;

/// <summary>
/// A custom contract resolver that translates FluentJson configurations into Newtonsoft.Json contracts.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Adapter / Strategy.
/// </para>
/// <para>
/// This class overrides the default reflection-based behavior of Newtonsoft.Json. It acts as the bridge 
/// where the abstract configuration model (stored in <see cref="JsonEntityDefinition"/>) is applied to 
/// the concrete <see cref="JsonProperty"/> and <see cref="JsonConverter"/> mechanisms.
/// </para>
/// </remarks>
public class FluentContractResolver : DefaultContractResolver
{
    private readonly Dictionary<Type, JsonEntityDefinition> _definitions = [];
    private Dictionary<Type, JsonConverter> _globalConverters = [];
    private readonly Dictionary<Type, (string PropName, object Value)> _discriminatorValues = [];

    // Captured DI provider to resolve converter dependencies
    private IServiceProvider? _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the fluent contract resolver.
    /// </summary>
    public FluentContractResolver()
    {
        // Default to CamelCase for modern JSON standards
        NamingStrategy = new CamelCaseNamingStrategy();
    }

    /// <summary>
    /// Injects the service provider used to resolve dependencies within converters.
    /// </summary>
    /// <param name="provider">The DI container.</param>
    internal void SetServiceProvider(IServiceProvider? provider) => _serviceProvider = provider;

    /// <summary>
    /// Registers a set of global converters to be used as fallbacks when no specific configuration exists.
    /// </summary>
    /// <param name="converters">A dictionary mapping CLR types to their JsonConverter instances.</param>
    public void RegisterGlobalConverters(Dictionary<Type, JsonConverter> converters) => _globalConverters = converters;

    /// <summary>
    /// Registers the configuration definition for a specific entity type.
    /// </summary>
    internal void RegisterDefinition(Type type, JsonEntityDefinition definition) => _definitions[type] = definition;

    /// <summary>
    /// Registers a fixed discriminator value for a derived type in a polymorphic hierarchy.
    /// </summary>
    internal void RegisterDiscriminatorValue(Type subType, string propName, object value) => _discriminatorValues[subType] = (propName, value);

    /// <summary>
    /// Creates a <see cref="JsonProperty"/> for the given member, applying fluent customizations.
    /// </summary>
    /// <param name="member">The member's reflection metadata.</param>
    /// <param name="memberSerialization">The serialization mode.</param>
    /// <returns>A configured JSON property.</returns>
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty property = base.CreateProperty(member, memberSerialization);

        // Check if Entity is Configured
        if (member.DeclaringType is null || !_definitions.TryGetValue(member.DeclaringType, out JsonEntityDefinition? entityDef))
        {
            return property;
        }

        // DDD Support: Enable writing to Private Setters
        // Standard reflection often skips private setters, but DDD requires them for rehydration.
        if (!property.Writable && member is PropertyInfo pi && pi.GetSetMethod(true) != null)
        {
            property.Writable = true;
        }

        // Apply Specific Configuration (Name, Order, Ignore, Converters)
        if (entityDef.Properties.TryGetValue(member, out JsonPropertyDefinition? propDef))
        {
            // Update: Pass the captured ServiceProvider to the configurator
            MemberConfigurator.Apply(property, propDef, member, _serviceProvider);
        }

        // Fallback to Global Converters if no specific converter was applied
        ApplyGlobalConverterFallback(property);

        return property;
    }

    private void ApplyGlobalConverterFallback(JsonProperty property)
    {
        if (property.Converter == null && property.PropertyType != null)
        {
            Type? propType = property.PropertyType;
            // Handle Nullable<T> unwrapping to find the core type
            Type actualType = Nullable.GetUnderlyingType(propType) ?? propType;

            if (_globalConverters.TryGetValue(actualType, out JsonConverter? globalConverter))
            {
                property.Converter = globalConverter;
            }
        }
    }

    /// <summary>
    /// Resolves the converter for a specific type, handling polymorphism and global overrides.
    /// </summary>
    protected override JsonConverter? ResolveContractConverter(Type objectType)
    {
        // Polymorphism Handling:
        // If the type is the base of a configured hierarchy, inject the PolymorphicJsonConverter.
        if (_definitions.TryGetValue(objectType, out JsonEntityDefinition? def) && def.Polymorphism != null)
        {
            return new PolymorphicJsonConverter(
                objectType,
                def.Polymorphism.DiscriminatorProperty,
                def.Polymorphism.SubTypes
            );
        }

        // Global Type Converters
        Type targetType = Nullable.GetUnderlyingType(objectType) ?? objectType;
        if (_globalConverters.TryGetValue(targetType, out JsonConverter? globalConverter))
        {
            return globalConverter;
        }

        return base.ResolveContractConverter(objectType);
    }

    /// <summary>
    /// Creates the collection of properties for the given type, injecting discriminator values where needed.
    /// </summary>
    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);

        // Polymorphism Discriminator Injection:
        // For derived types in a polymorphic hierarchy, we force the discriminator property 
        // to write the configured fixed value (e.g., "circle") instead of reading from the object instance.
        if (_discriminatorValues.TryGetValue(type, out (string PropName, object Value) discriminatorInfo))
        {
            JsonProperty? discriminatorProp = properties.FirstOrDefault(p => p.UnderlyingName == discriminatorInfo.PropName);
            if (discriminatorProp != null)
            {
                discriminatorProp.ValueProvider = new FixedValueProvider(discriminatorInfo.Value);
                discriminatorProp.Ignored = false;
                discriminatorProp.Readable = true;
            }
        }
        return properties;
    }
}
