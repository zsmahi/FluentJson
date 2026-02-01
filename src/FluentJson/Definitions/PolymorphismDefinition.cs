using System;
using System.Collections.Generic;
using System.Diagnostics;
using FluentJson.Abstractions;

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
    /// </summary>
    /// <param name="type">The concrete CLR type to instantiate.</param>
    /// <param name="value">The unique value found in the JSON discriminator property.</param>
    /// <remarks>
    /// <para>
    /// <strong>Architectural Constraint:</strong>
    /// To ensure optimal parsing performance and broad compatibility across JSON standards, 
    /// the discriminator <paramref name="value"/> is strictly limited to primitive types 
    /// (<c>string</c>, <c>int</c>, <c>bool</c>) or <c>enum</c> values. Complex objects are not supported as discriminators.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not a primitive, string, or enum.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the definition is frozen.</exception>
    public void AddSubType(Type type, object value)
    {
        ThrowIfFrozen();

        if (value != null)
        {
            Type vType = value.GetType();
            // Validate that the discriminator is lightweight
            if (!vType.IsPrimitive && !vType.IsEnum && vType != typeof(string))
            {
                throw new ArgumentException(
                    $"Discriminator value for '{type.Name}' must be a primitive (int, string, bool, enum). Found: '{vType.Name}'.");
            }

            _subTypes[type] = value;
        }
    }


    /// <summary>
    /// Locks the definition to prevent further modifications.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the discriminator property name is invalid.</exception>
    public override void Freeze()
    {
        // Sanity check ensuring the definition is valid before locking it.
        if (string.IsNullOrWhiteSpace(DiscriminatorProperty))
        {
            throw new InvalidOperationException("DiscriminatorProperty metadata must be present to freeze.");
        }
        base.Freeze();
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay =>
        $"Discriminator: \"{DiscriminatorProperty}\" | SubTypes: {_subTypes.Count}";
}
