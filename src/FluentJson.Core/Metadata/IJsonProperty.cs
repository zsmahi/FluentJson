using System.Reflection;
using System; // Added for Type

namespace FluentJson.Core.Metadata;

/// <summary>
/// Represents the compiled metadata for a specific mapped member (property or field).
/// </summary>
public interface IJsonProperty
{
    /// <summary>
    /// Gets the overridden JSON string name, or the default member name if not overridden.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Gets the underlying reflection <see cref="System.Reflection.MemberInfo"/> representing the property or field.
    /// </summary>
    MemberInfo MemberInfo { get; }
    
    /// <summary>
    /// Gets the precise CLR type of the property or field.
    /// </summary>
    Type PropertyType { get; }
    /// <summary>
    /// Indicates whether the deserializer should throw an error if this property is missing from the JSON payload.
    /// </summary>
    bool IsRequired { get; }
    
    /// <summary>
    /// Indicates whether this specific member should be completely skipped by the serialization engine.
    /// </summary>
    bool IsIgnored { get; }

    /// <summary>
    /// Gets the scalar type that this complex property unwrap/flattens to. Null if no conversion is requested.
    /// </summary>
    Type? ConvertedType { get; }

    /// <summary>
    /// A delegate used to extract the flat scalar value from the complex property.
    /// </summary>
    Delegate? SerializeFunc { get; }

    /// <summary>
    /// A delegate used to reconstruct the complex property from the flat scalar value.
    /// </summary>
    Delegate? DeserializeFunc { get; }
}
