using System;
using System.Linq.Expressions;
using System.Reflection;
using FluentJson.Abstractions;
using FluentJson.Definitions;
using FluentJson.Exceptions;

namespace FluentJson.Builders;

/// <summary>
/// Provides a fluent API for configuring the serialization behavior of a specific property or field.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Stateful Builder / Deferred Execution.
/// </para>
/// <para>
/// This builder accumulates configuration changes in a temporary state. 
/// Modifications are applied to the underlying <see cref="JsonPropertyDefinition"/> only during the final build phase,
/// ensuring transactional integrity and allowing validation before application.
/// </para>
/// </remarks>
public class JsonPropertyBuilder<T, TProp> : IJsonPropertyBuilderAccessor
{
    private readonly MemberInfo _member;

    // Shadow State (Temporary storage)
    private string? _jsonName;
    private int? _order;
    private bool _ignored;
    private bool? _isRequired;
    private FieldInfo? _backingField;
    private IConverterDefinition? _converterDefinition;

    internal JsonPropertyBuilder(MemberInfo member)
    {
        _member = member;
    }

    // --- Fluent API (Writes to Shadow State) ---

    public JsonPropertyBuilder<T, TProp> HasConversion<TConverter>()
    {
        _converterDefinition = new TypeConverterDefinition(typeof(TConverter));
        return this;
    }

    public JsonPropertyBuilder<T, TProp> HasConversion<TJson>(
        Expression<Func<TProp, TJson>> convertTo,
        Expression<Func<TJson, TProp>> convertFrom)
    {
        _converterDefinition = new LambdaConverterDefinition<TProp, TJson>(
            convertTo.Compile(),
            convertFrom.Compile()
        );
        return this;
    }

    public JsonPropertyBuilder<T, TProp> HasField(string fieldName)
    {
        Type type = _member.DeclaringType!;
        FieldInfo? field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        if (field is null)
        {
            throw new FluentJsonConfigurationException(
                $"The field '{fieldName}' does not exist in class '{type.Name}'. " +
                "Ensure the name is correct (case-sensitive) and the field is defined in this class.");
        }

        _backingField = field;
        return this;
    }

    public JsonPropertyBuilder<T, TProp> HasJsonPropertyName(string name)
    {
        _jsonName = name;
        return this;
    }

    public JsonPropertyBuilder<T, TProp> HasOrder(int order)
    {
        _order = order;
        return this;
    }

    public JsonPropertyBuilder<T, TProp> IsRequired(bool required = true)
    {
        _isRequired = required;
        return this;
    }

    public JsonPropertyBuilder<T, TProp> Ignore()
    {
        _ignored = true;
        return this;
    }

    // --- Internal Application Logic (Commit State) ---

    /// <inheritdoc />
    void IJsonPropertyBuilderAccessor.Apply(JsonEntityDefinition entityDef)
    {
        // Retrieve or create the definition for this member
        JsonPropertyDefinition def = entityDef.GetOrCreateMember(_member);

        // Apply state only if explicitly set (Shadow State -> Final State)
        if (_ignored)
        {
            def.Ignored = true;
        }

        if (_jsonName != null)
        {
            def.JsonName = _jsonName;
        }

        if (_order.HasValue)
        {
            def.Order = _order;
        }

        if (_isRequired.HasValue)
        {
            def.IsRequired = _isRequired;
        }

        if (_backingField != null)
        {
            def.BackingField = _backingField;
        }

        if (_converterDefinition != null)
        {
            def.ConverterDefinition = _converterDefinition;
        }
    }
}
