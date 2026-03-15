using System;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using FluentJson.Core.Metadata;

namespace FluentJson.SystemTextJson;

/// <summary>
/// A System.Text.Json type modifier that injects FluentJson configuration into the native JSON resolution process.
/// </summary>
/// <remarks>
/// Crucially replaces default object instantiation with our compiled expression factories (bypassing GetUninitializedObject) to ensure Domain invariants remain protected.
/// </remarks>
public class FluentJsonTypeModifier
{
    private readonly IJsonModel _model;

    /// <summary>
    /// Initializes a new instance of the <see cref="FluentJsonTypeModifier"/> class using a pre-compiled JSON model.
    /// </summary>
    /// <param name="model">The frozen metadata model defining the serialization rules.</param>
    public FluentJsonTypeModifier(IJsonModel model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <summary>
    /// The action executed by System.Text.Json to modify the type info during the resolution process.
    /// </summary>
    /// <param name="typeInfo">The native JSON type info being manipulated.</param>
    public void Modify(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        var entity = _model.Entities.FirstOrDefault(e => e.EntityType == typeInfo.Type);
        if (entity != null)
        {
            if (entity.DiscriminatorPropertyName != null)
            {
                // Polymorphism is now handled by FluentJsonStjPolymorphicConverterFactory injected in Options
            }

            // Crucial DDD Fix: Override STJ's default factory (which might use GetUninitializedObject)
            // with our highly optimized, compiled expression factory that respects invariants!
            // Note: Abstract classes throw on ConstructorFactory invocation, which is the expected behavior.
            if (!typeInfo.Type.IsAbstract)
            {
                typeInfo.CreateObject = entity.ConstructorFactory;
            }

            // Remove ignored properties or those not in our model
            for (int i = typeInfo.Properties.Count - 1; i >= 0; i--)
            {
                var propertyInfo = typeInfo.Properties[i];
                var fluentProperty = FindFluentProperty(typeInfo.Type, propertyInfo.Name);
                if (fluentProperty == null || fluentProperty.IsIgnored)
                {
                    propertyInfo.Set = null;
                    propertyInfo.Get = null;
                }
            }

            // Sync properties and add missing fields from the hierarchy
            var allFluentProperties = GetAllFluentProperties(typeInfo.Type);
            foreach (var fluentProperty in allFluentProperties)
            {
                if (fluentProperty.IsIgnored) continue;

                var propertyInfo = typeInfo.Properties.FirstOrDefault(p => p.Name == fluentProperty.Name || (fluentProperty.MemberInfo != null && p.Name == fluentProperty.MemberInfo.Name));

                if (propertyInfo == null)
                {
                    var propertyTypeInfo = typeInfo.Options.GetTypeInfo(fluentProperty.PropertyType);
                    propertyInfo = typeInfo.CreateJsonPropertyInfo(fluentProperty.PropertyType, fluentProperty.Name);
                    typeInfo.Properties.Add(propertyInfo);
                }
                else
                {
                    propertyInfo.Name = fluentProperty.Name;
                }

                // STJ needs special handling to bind to private setters and fields
                bool canWrite = fluentProperty.MemberInfo switch
                {
                    PropertyInfo p => p.CanWrite || p.GetSetMethod(true) != null,
                    FieldInfo f => true,
                    _ => false
                };

                if (canWrite)
                {
                    propertyInfo.Set = (obj, value) =>
                    {
                        if (fluentProperty.MemberInfo is PropertyInfo pInfo)
                            pInfo.SetValue(obj, value);
                        else if (fluentProperty.MemberInfo is FieldInfo fInfo)
                            fInfo.SetValue(obj, value);
                    };
                }
                else if (fluentProperty.MemberInfo is PropertyInfo pInfo && !pInfo.CanWrite)
                {
                    var backingField = typeInfo.Type.GetField($"<{pInfo.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (backingField != null)
                    {
                        propertyInfo.Set = (obj, value) => backingField.SetValue(obj, value);
                    }
                    else
                    {
                        // Dummy setter to prevent STJ from throwing InvalidOperationException on Required properties
                        propertyInfo.Set = (obj, value) => { };
                    }
                }
                
                // Allow getting values from fields and private getters as well
                propertyInfo.Get = (obj) =>
                {
                    return fluentProperty.MemberInfo switch
                    {
                        PropertyInfo p => p.GetValue(obj),
                        FieldInfo f => f.GetValue(obj),
                        _ => null
                    };
                };
                propertyInfo.IsRequired = fluentProperty.IsRequired;

                // Epic 17: Flattening/Conversion support
                if (fluentProperty.ConvertedType != null)
                {
                    var converterType = typeof(FluentJsonStjValueConverter<,>).MakeGenericType(fluentProperty.PropertyType, fluentProperty.ConvertedType);
                    propertyInfo.CustomConverter = (System.Text.Json.Serialization.JsonConverter)Activator.CreateInstance(converterType, fluentProperty.SerializeFunc, fluentProperty.DeserializeFunc)!;
                }
            }
        }
    }

    private IJsonProperty? FindFluentProperty(Type currentType, string memberName)
    {
        var type = currentType;
        while (type != null && type != typeof(object))
        {
            var entity = _model.Entities.FirstOrDefault(e => e.EntityType == type);
            if (entity != null)
            {
                var prop = entity.Properties.FirstOrDefault(p => p.MemberInfo.Name == memberName);
                if (prop != null) return prop;
            }
            type = type.BaseType;
        }
        return null;
    }

    private System.Collections.Generic.IEnumerable<IJsonProperty> GetAllFluentProperties(Type currentType)
    {
        var properties = new System.Collections.Generic.List<IJsonProperty>();
        var type = currentType;
        while (type != null && type != typeof(object))
        {
            var entity = _model.Entities.FirstOrDefault(e => e.EntityType == type);
            if (entity != null)
            {
                foreach (var prop in entity.Properties)
                {
                    if (!properties.Any(p => p.MemberInfo.Name == prop.MemberInfo.Name))
                    {
                        properties.Add(prop);
                    }
                }
            }
            type = type.BaseType;
        }
        return properties;
    }
}
