using System;
using System.Diagnostics;
using System.Reflection;
using FluentJson.Abstractions;

namespace FluentJson.Definitions;

/// <summary>
/// Represents the configuration rules for a specific property or field within an entity.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Data Transfer Object (DTO) / Metadata Container.
/// </para>
/// <para>
/// This class holds the mutable configuration state (name overrides, converters, ordering, etc.) 
/// accumulated by the <see cref="FluentJson.Builders.JsonPropertyBuilder{T, TProp}"/>. 
/// It acts as the "source of truth" that the concrete serializer adapters (Newtonsoft/STJ) read 
/// to configure their internal mapping tables.
/// </para>
/// <para>
/// <strong>Thread Safety:</strong>
/// Inherits from <see cref="FreezableBase"/>. Once the configuration phase is finished and <see cref="Freeze"/> 
/// is called, this object becomes immutable, allowing safe concurrent access by the serializer engine.
/// </para>
/// </remarks>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class JsonPropertyDefinition : FreezableBase
{
    private FieldInfo? _backingField;
    private IConverterDefinition? _converterDefinition;
    private string? _jsonName;
    private bool _ignored;
    private bool? _isRequired;
    private int? _order;

    /// <summary>
    /// Gets the reflection metadata (PropertyInfo or FieldInfo) identifying the member being configured.
    /// </summary>
    public MemberInfo Member { get; }

    /// <summary>
    /// Initializes a new instance of the property definition for the specified member.
    /// </summary>
    /// <param name="member">The reflection metadata of the property or field.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="member"/> is null.</exception>
    public JsonPropertyDefinition(MemberInfo member)
    {
        if (member is null)
        {
            throw new ArgumentNullException(nameof(member));
        }
        Member = member;
    }

    /// <summary>
    /// Locks the definition to prevent further modifications.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the critical <see cref="Member"/> metadata is missing.</exception>
    public override void Freeze()
    {
        // Sanity check to protect against invalid internal states
        if (Member == null)
        {
            throw new InvalidOperationException("Member metadata must be present to freeze.");
        }
        base.Freeze();
    }

    /// <summary>
    /// Gets or sets the private backing field used for data access.
    /// </summary>
    /// <remarks>
    /// If set, the serializer will bypass the property's public getter/setter and read/write 
    /// this field directly. This enables encapsulation patterns common in DDD.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if the definition is frozen.</exception>
    public FieldInfo? BackingField
    {
        get => _backingField;
        set
        {
            ThrowIfFrozen();
            _backingField = value;
        }
    }

    /// <summary>
    /// Gets or sets the custom conversion strategy for this property.
    /// </summary>
    /// <value>
    /// An <see cref="IConverterDefinition"/> representing either a type-based converter 
    /// or a lambda-based inline conversion.
    /// </value>
    /// <exception cref="InvalidOperationException">Thrown if the definition is frozen.</exception>
    public IConverterDefinition? ConverterDefinition
    {
        get => _converterDefinition;
        set
        {
            ThrowIfFrozen();
            _converterDefinition = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this property is explicitly excluded from serialization.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the definition is frozen.</exception>
    public bool Ignored
    {
        get => _ignored;
        set
        {
            ThrowIfFrozen();
            _ignored = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating if the property is mandatory in the JSON payload.
    /// </summary>
    /// <value>
    /// <c>true</c> forces validation; <c>false</c> makes it optional; <c>null</c> defers to the serializer's default behavior.
    /// </value>
    /// <exception cref="InvalidOperationException">Thrown if the definition is frozen.</exception>
    public bool? IsRequired
    {
        get => _isRequired;
        set
        {
            ThrowIfFrozen();
            _isRequired = value;
        }
    }

    /// <summary>
    /// Gets or sets the custom key name to be used in the JSON output.
    /// </summary>
    /// <value>
    /// The string name to map to, or <c>null</c> to use the default naming convention (e.g., CamelCase).
    /// </value>
    /// <exception cref="InvalidOperationException">Thrown if the definition is frozen.</exception>
    public string? JsonName
    {
        get => _jsonName;
        set
        {
            ThrowIfFrozen();
            _jsonName = value;
        }
    }

    /// <summary>
    /// Gets or sets the explicit serialization order.
    /// </summary>
    /// <remarks>
    /// Properties are serialized in ascending order. If <c>null</c>, the order is undefined (or default).
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if the definition is frozen.</exception>
    public int? Order
    {
        get => _order;
        set
        {
            ThrowIfFrozen();
            _order = value;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay
    {
        get
        {
            string state = Ignored ? " [IGNORED]" : "";
            string mapping = _jsonName != null ? $" -> \"{_jsonName}\"" : "";
            string required = _isRequired.HasValue ? (_isRequired.Value ? " [Required]" : " [Optional]") : "";
            string backingfield = _backingField != null ? $" (Field: {_backingField.Name})" : "";

            return $"{Member.Name}{mapping}{state}{required}{backingfield}";
        }
    }
}
