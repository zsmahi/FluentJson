using System;
using System.Diagnostics;
using System.Reflection;
using FluentJson.Abstractions;

namespace FluentJson.Definitions;

/// <summary>
/// Represents the configuration and metadata for a specific property or field within a JSON entity.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class JsonPropertyDefinition(MemberInfo member) : FreezableBase
{

    /// <summary>
    /// Gets the reflection metadata for the property or field.
    /// </summary>
    public MemberInfo Member { get; } = member ?? throw new ArgumentNullException(nameof(member));

    /// <summary>
    /// Gets the custom name to be used in the JSON payload.
    /// </summary>
    // ARCHITECTURE FIX (S1): Setter is now internal. Only the Builder can modify this.
    public string? JsonName { get; internal set; }

    /// <summary>
    /// Gets the serialization order. Lower values are processed first.
    /// </summary>
    public int? Order { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether this property should be ignored during serialization/deserialization.
    /// </summary>
    public bool Ignored { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether this property is mandatory.
    /// </summary>
    public bool? IsRequired { get; internal set; }

    /// <summary>
    /// Gets the private backing field to be used for data access, if configured.
    /// </summary>
    public FieldInfo? BackingField { get; internal set; }

    /// <summary>
    /// Gets the strategy for converting values (e.g. TypeConverter or Lambda).
    /// </summary>
    public IConverterDefinition? ConverterDefinition { get; internal set; }

    /// <summary>
    /// Locks the definition to prevent further modifications.
    /// </summary>
    public override void Freeze()
    {
        // ConverterDefinition is immutable by design (constructor init), 
        // but we ensure the structure itself is locked.
        base.Freeze();
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay =>
        $"Member: {Member.Name} | JSON: \"{JsonName ?? Member.Name}\" | Ignored: {Ignored}";
}
