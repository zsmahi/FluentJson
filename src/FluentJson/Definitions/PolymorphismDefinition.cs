using System;
using System.Collections.Generic;
using System.Diagnostics;
using FluentJson.Abstractions;
using FluentJson.Exceptions;

namespace FluentJson.Definitions;

/// <summary>
/// Stores the configuration strategy for handling polymorphic deserialization.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Metadata Container / Type Map.
/// </para>
/// <para>
/// This definition instructs the serializer on how to distinguish between derived classes within a hierarchy.
/// It works by mapping a specific JSON property (the "discriminator") to concrete CLR types based on its value.
/// </para>
/// <para>
/// <strong>Compatibility Note:</strong>
/// This model is designed to map cleanly to both Newtonsoft.Json's custom converters and 
/// System.Text.Json's native <c>JsonPolymorphismOptions</c>.
/// </para>
/// </remarks>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class PolymorphismDefinition : FreezableBase
{
    private readonly Dictionary<Type, object> _subTypes = [];

    /// <summary>
    /// Initializes a new instance of the polymorphism definition.
    /// </summary>
    /// <param name="discriminatorProperty">
    /// The case-sensitive name of the JSON property that acts as the type identifier (e.g., "$type", "kind").
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="discriminatorProperty"/> is null or empty.</exception>
    public PolymorphismDefinition(string discriminatorProperty)
    {
        if (string.IsNullOrWhiteSpace(discriminatorProperty))
        {
            throw new ArgumentNullException(nameof(discriminatorProperty));
        }

        DiscriminatorProperty = discriminatorProperty;
    }

    /// <summary>
    /// Gets the name of the property in the JSON payload that holds the discriminator value.
    /// </summary>
    public string DiscriminatorProperty { get; }

    /// <summary>
    /// Gets a read-only dictionary mapping concrete CLR types to their unique discriminator values.
    /// </summary>
    public IReadOnlyDictionary<Type, object> SubTypes => _subTypes;

    /// <summary>
    /// Gets a value indicating whether the discriminator is a "shadow property" 
    /// (i.e. it exists in the JSON but not in the C# class).
    /// </summary>
    public bool IsShadowProperty { get; set; }
    /// <summary>
    /// Registers a mapping between a concrete derived type and its discriminator value.
    /// Enforces strict uniqueness for both types and values.
    /// </summary>
    /// <param name="type">The concrete CLR type to instantiate.</param>
    /// <param name="value">The unique value found in the JSON discriminator property.</param>
    /// <exception cref="FluentJsonConfigurationException">Thrown if the type or value is already registered, or if the value type is invalid.</exception>
    public void AddSubType(Type type, object value)
    {
        ThrowIfFrozen();

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        // 1. Validate Discriminator Type (Primitive/String/Enum only)
        Type vType = value.GetType();
        if (!vType.IsPrimitive && !vType.IsEnum && vType != typeof(string))
        {
            throw new FluentJsonConfigurationException(
                $"Discriminator value for '{type.Name}' must be a primitive (int, string, bool, enum). Found: '{vType.Name}'.");
        }

        // 2. Validate Type Uniqueness (No silent override)
        if (_subTypes.ContainsKey(type))
        {
            throw new FluentJsonConfigurationException(
                $"The type '{type.Name}' is already registered in the polymorphic hierarchy.");
        }

        // 3. Validate Value Uniqueness
        // Note: linear search is acceptable here as configuration happens once and hierarchies are generally small.
        if (_subTypes.ContainsValue(value))
        {
            throw new FluentJsonConfigurationException(
                $"The discriminator value '{value}' is already assigned to another type in the hierarchy.");
        }

        _subTypes.Add(type, value);
    }

    /// <summary>
    /// Locks the definition to prevent further modifications.
    /// </summary>
    /// <exception cref="FluentJsonConfigurationException">Thrown if the discriminator property name is invalid.</exception>
    public override void Freeze()
    {
        if (string.IsNullOrWhiteSpace(DiscriminatorProperty))
        {
            throw new FluentJsonConfigurationException("DiscriminatorProperty metadata must be present to freeze.");
        }

        base.Freeze();
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay =>
        $"Discriminator: \"{DiscriminatorProperty}\" | SubTypes: {_subTypes.Count}";
}
