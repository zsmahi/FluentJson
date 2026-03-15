using System;
using System.Linq;
using FluentJson.Core.Metadata;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace FluentJson.Newtonsoft;

/// <summary>
/// A custom Newtonsoft.Json contract resolver that integrates FluentJson metadata conventions.
/// </summary>
/// <remarks>
/// Overrides default behaviors to support DDD patterns such as mapping to private fields and instantiating via private constructors, while avoiding properties not explicitly mapped.
/// </remarks>
public class FluentJsonContractResolver : DefaultContractResolver
{
    private readonly IJsonModel _model;

    /// <summary>
    /// Initializes a new instance of the <see cref="FluentJsonContractResolver"/> class using a pre-compiled JSON model.
    /// </summary>
    /// <param name="model">The frozen metadata model defining the serialization rules.</param>
    public FluentJsonContractResolver(IJsonModel model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    protected override JsonObjectContract CreateObjectContract(Type objectType)
    {
        var contract = base.CreateObjectContract(objectType);
        
        var entity = _model.Entities.FirstOrDefault(e => e.EntityType == objectType);
        if (entity != null)
        {
            // By overriding DefaultCreator with our pre-compiled ConstructorFactory, 
            // we bypass Newtonsoft's standard instantiation (which might fail on private constructors)
            if (!objectType.IsAbstract)
            {
                contract.DefaultCreator = () => entity.ConstructorFactory();
                contract.OverrideCreator = args => entity.ConstructorFactory();
                contract.CreatorParameters.Clear();
            }

            if (entity.DiscriminatorPropertyName != null)
            {
                contract.Converter = new FluentJsonPolymorphicConverter(entity.DiscriminatorPropertyName, entity.DerivedTypes);
            }

            if (entity.ShouldPreserveReferences)
            {
                contract.IsReference = true;
            }
        }

        return contract;
    }

    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        var properties = base.CreateProperties(type, memberSerialization);
        
        var entity = _model.Entities.FirstOrDefault(e => e.EntityType == type);
        if (entity != null)
        {
            foreach (var fluentProperty in entity.Properties)
            {
                if (fluentProperty.IsIgnored) continue;

                // Check if the property is already discovered (e.g. public properties)
                var existingProperty = properties.FirstOrDefault(p => p.UnderlyingName == fluentProperty.Name || (fluentProperty.MemberInfo != null && p.UnderlyingName == fluentProperty.MemberInfo.Name));

                if (existingProperty == null && fluentProperty.MemberInfo != null)
                {
                    // For private fields or ignored by default members, we must explicitly create and add them
                    var missingProperty = CreateProperty(fluentProperty.MemberInfo, memberSerialization);
                    properties.Add(missingProperty);
                }
            }
        }
        
        return properties;
    }

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);
        
        var entity = _model.Entities.FirstOrDefault(e => e.EntityType == member.DeclaringType);
        if (entity != null)
        {
            var fluentProperty = entity.Properties.FirstOrDefault(p => p.MemberInfo.Name == property.UnderlyingName);
            
            if (fluentProperty == null || fluentProperty.IsIgnored)
            {
                property.Ignored = true;
            }
            else
            {
                property.Ignored = false;
                property.Readable = true;
                property.PropertyName = fluentProperty.Name;
                property.Required = fluentProperty.IsRequired ? Required.Always : Required.Default;
                
                if (memberSerialization != global::Newtonsoft.Json.MemberSerialization.OptIn)
                {
                    // Force Newtonsoft to map fields and properties with private setters
                    bool canWrite = fluentProperty.MemberInfo switch
                    {
                        PropertyInfo p => p.CanWrite || p.GetSetMethod(true) != null,
                        FieldInfo f => true, // Fields can always be written to via reflection
                        _ => false
                    };

                    if (canWrite)
                    {
                        property.Writable = true;
                        // Provide a custom value provider to ensure we use the explicit MemberInfo
                        property.ValueProvider = new global::Newtonsoft.Json.Serialization.ReflectionValueProvider(fluentProperty.MemberInfo);
                    }
                    else if (fluentProperty.MemberInfo is PropertyInfo pInfo && !pInfo.CanWrite)
                    {
                        var declaringType = fluentProperty.MemberInfo.DeclaringType;
                        if (declaringType != null)
                        {
                            var backingField = declaringType.GetField($"<{pInfo.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                            if (backingField != null)
                            {
                                property.Writable = true;
                                property.ValueProvider = new global::Newtonsoft.Json.Serialization.ReflectionValueProvider(backingField);
                            }
                        }
                    }
                }

                // Epic 17: Flattening/Conversion support
                if (fluentProperty.ConvertedType != null)
                {
                    var converterType = typeof(FluentJsonNwValueConverter<,>).MakeGenericType(fluentProperty.PropertyType, fluentProperty.ConvertedType);
                    property.Converter = (JsonConverter)Activator.CreateInstance(converterType, fluentProperty.SerializeFunc, fluentProperty.DeserializeFunc)!;
                }
            }
        }

        return property;
    }
}
