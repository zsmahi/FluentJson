using FluentJson.Definitions;
using FluentJson.NewtonsoftJson.Converters;
using FluentJson.NewtonsoftJson.Internal;
using FluentJson.Internal; // Required for TypeSafety
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FluentJson.NewtonsoftJson.Core;

public class FluentContractResolver : DefaultContractResolver
{
    private readonly Dictionary<Type, JsonEntityDefinition> _definitions = [];
    private Dictionary<Type, JsonConverter> _globalConverters = [];
    private readonly Dictionary<Type, (string PropName, object Value)> _discriminatorValues = [];
    private IServiceProvider? _serviceProvider;

    public FluentContractResolver()
    {
        NamingStrategy = new CamelCaseNamingStrategy();
    }

    internal void SetServiceProvider(IServiceProvider? provider) => _serviceProvider = provider;

    public void RegisterGlobalConverters(Dictionary<Type, JsonConverter> converters) => _globalConverters = converters;

    internal void RegisterDefinition(Type type, JsonEntityDefinition definition) => _definitions[type] = definition;

    internal void RegisterDiscriminatorValue(Type subType, string propName, object value) => _discriminatorValues[subType] = (propName, value);

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty property = base.CreateProperty(member, memberSerialization);

        // --- Resilience Check ---
        // Prevent serialization crashes for native types (IntPtr, Pointers, etc.)
        // unless a custom converter has been explicitly defined.
        if (property.PropertyType != null &&
            property.Converter == null &&
            TypeSafety.IsUnsupported(property.PropertyType))
        {
            property.Ignored = true;
            property.ShouldSerialize = _ => false;
            return property;
        }

        if (member.DeclaringType is null || !_definitions.TryGetValue(member.DeclaringType, out JsonEntityDefinition? entityDef))
        {
            // Apply global fallback even if no specific entity definition exists
            ApplyGlobalConverterFallback(property);
            return property;
        }

        // Enable writing for properties with private setters if they are not read-only
        if (!property.Writable && member is PropertyInfo pi && pi.GetSetMethod(true) != null)
        {
            property.Writable = true;
        }

        // Apply specific property configuration from Fluent API
        if (entityDef.Properties.TryGetValue(member, out JsonPropertyDefinition? propDef))
        {
            MemberConfigurator.Apply(property, propDef, member, _serviceProvider);
        }

        ApplyGlobalConverterFallback(property);

        return property;
    }

    private void ApplyGlobalConverterFallback(JsonProperty property)
    {
        if (property.Converter == null && property.PropertyType != null)
        {
            Type? propType = property.PropertyType;
            Type actualType = Nullable.GetUnderlyingType(propType) ?? propType;

            if (_globalConverters.TryGetValue(actualType, out JsonConverter? globalConverter))
            {
                property.Converter = globalConverter;
            }
        }
    }

    protected override JsonConverter? ResolveContractConverter(Type objectType)
    {
        // Handle class-level polymorphism
        if (_definitions.TryGetValue(objectType, out JsonEntityDefinition? def) && def.Polymorphism != null)
        {
            return new PolymorphicJsonConverter(objectType, def.Polymorphism);
        }

        // Handle global converter fallback for the type
        Type targetType = Nullable.GetUnderlyingType(objectType) ?? objectType;
        if (_globalConverters.TryGetValue(targetType, out JsonConverter? globalConverter))
        {
            return globalConverter;
        }

        return base.ResolveContractConverter(objectType);
    }

    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);

        // Polymorphism Discriminator Injection for SubTypes
        if (_discriminatorValues.TryGetValue(type, out (string PropName, object Value) discriminatorInfo))
        {
            JsonProperty? discriminatorProp = properties.FirstOrDefault(p => p.UnderlyingName == discriminatorInfo.PropName);
            if (discriminatorProp != null)
            {
                PropertyInfo? underlyingMember = type.GetProperty(discriminatorProp.UnderlyingName!,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                // Use FixedValueProvider to inject the discriminator value during serialization/deserialization
                discriminatorProp.ValueProvider = new FixedValueProvider(discriminatorInfo.Value, underlyingMember);
                discriminatorProp.Ignored = false;
                discriminatorProp.Readable = true;
                discriminatorProp.Writable = true;
            }
        }
        return properties;
    }
}
