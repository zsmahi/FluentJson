using System;
using System.Collections.Generic;
using System.Diagnostics;
using FluentJson.Abstractions;
using FluentJson.Exceptions;

namespace FluentJson.Definitions;

/// <summary>
/// Stores the configuration strategy for handling polymorphic deserialization.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class PolymorphismDefinition : FreezableBase
{
    private readonly Dictionary<Type, object> _subTypes = [];

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
    /// Gets a value indicating whether the discriminator is a "shadow property".
    /// </summary>
    // ARCHITECTURE FIX (S1): Setter is now internal.
    public bool IsShadowProperty { get; internal set; }

    /// <summary>
    /// Registers a mapping between a concrete derived type and its discriminator value.
    /// Enforces strict uniqueness for both types and values.
    /// </summary>
    public void AddSubType(Type type, object value)
    {
        ThrowIfFrozen();

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        // 1. Validate Discriminator Type
        Type vType = value.GetType();
        if (!vType.IsPrimitive && !vType.IsEnum && vType != typeof(string))
        {
            throw new FluentJsonConfigurationException(
                $"Discriminator value for '{type.Name}' must be a primitive (int, string, bool, enum). Found: '{vType.Name}'.");
        }

        // 2. Validate Type Uniqueness
        if (_subTypes.ContainsKey(type))
        {
            throw new FluentJsonConfigurationException(
                $"The type '{type.Name}' is already registered in the polymorphic hierarchy.");
        }

        // 3. Validate Value Uniqueness
        if (_subTypes.ContainsValue(value))
        {
            throw new FluentJsonConfigurationException(
                $"The discriminator value '{value}' is already assigned to another type in the hierarchy.");
        }

        _subTypes.Add(type, value);
    }

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
