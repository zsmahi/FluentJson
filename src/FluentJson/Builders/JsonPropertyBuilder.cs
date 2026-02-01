using System;
using System.Linq.Expressions;
using System.Reflection;
using FluentJson.Abstractions;
using FluentJson.Definitions;

namespace FluentJson.Builders;

/// <summary>
/// Provides a fluent API for configuring the serialization behavior of a specific property or field.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Builder / Fluent Interface.
/// </para>
/// <para>
/// This builder enables fine-grained control over how individual members are processed, supporting 
/// advanced scenarios such as private backing field access (DDD), inline value conversion, 
/// and metadata overrides (name, order, required state).
/// </para>
/// </remarks>
/// <typeparam name="T">The type of the entity containing the property.</typeparam>
/// <typeparam name="TProp">The type of the property being configured.</typeparam>
public class JsonPropertyBuilder<T, TProp>
{
    private readonly JsonPropertyDefinition _definition;

    internal JsonPropertyBuilder(JsonEntityDefinition entityDef, MemberInfo member)
    {
        _definition = entityDef.GetOrCreateMember(member);
    }

    /// <summary>
    /// Applies a specific converter type to this property.
    /// </summary>
    /// <typeparam name="TConverter">
    /// The concrete type of the converter to instantiate. 
    /// It must be compatible with the underlying JSON engine (e.g., inherit from <c>JsonConverter</c>).
    /// </typeparam>
    /// <returns>The builder instance for chaining.</returns>
    public JsonPropertyBuilder<T, TProp> HasConversion<TConverter>()
    {
        _definition.ConverterDefinition = new TypeConverterDefinition(typeof(TConverter));
        return this;
    }

    /// <summary>
    /// Configures custom conversion logic using lambda expressions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Performance Note:</strong>
    /// The provided expressions are compiled immediately during the configuration phase. 
    /// The resulting delegates are cached, ensuring that the runtime serialization process 
    /// incurs zero overhead from Expression Tree compilation.
    /// </para>
    /// </remarks>
    /// <typeparam name="TJson">The intermediate type used in the JSON representation (e.g., <c>string</c>, <c>int</c>).</typeparam>
    /// <param name="convertTo">An expression defining how to convert the domain property to the JSON value.</param>
    /// <param name="convertFrom">An expression defining how to convert the JSON value back to the domain property.</param>
    /// <returns>The builder instance for chaining.</returns>
    public JsonPropertyBuilder<T, TProp> HasConversion<TJson>(
        Expression<Func<TProp, TJson>> convertTo,
        Expression<Func<TJson, TProp>> convertFrom)
    {
        _definition.ConverterDefinition = new LambdaConverterDefinition<TProp, TJson>(
            convertTo.Compile(),
            convertFrom.Compile()
        );
        return this;
    }

    /// <summary>
    /// Configures the property to access a specific backing field directly, bypassing the property getter/setter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is essential for <strong>Domain-Driven Design (DDD)</strong> scenarios where properties might be read-only 
    /// or have private setters to enforce encapsulation invariants.
    /// </para>
    /// </remarks>
    /// <param name="fieldName">The case-sensitive name of the field.</param>
    /// <returns>The builder instance for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified field cannot be found on the type.</exception>
    public JsonPropertyBuilder<T, TProp> HasField(string fieldName)
    {
        Type type = _definition.Member.DeclaringType!;

        // Scan for both Public and NonPublic fields to support encapsulation
        FieldInfo? field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        if (field is null)
        {
            throw new InvalidOperationException($"The field '{fieldName}' does not exist in class '{type.Name}'.");
        }

        _definition.BackingField = field;
        return this;
    }

    /// <summary>
    /// Overrides the name of the property in the generated JSON.
    /// </summary>
    /// <param name="name">The custom key name to use in the JSON object.</param>
    /// <returns>The builder instance for chaining.</returns>
    public JsonPropertyBuilder<T, TProp> HasJsonPropertyName(string name)
    {
        _definition.JsonName = name;
        return this;
    }

    /// <summary>
    /// Sets the explicit serialization order for this property.
    /// </summary>
    /// <param name="order">The numeric order value. Lower values are serialized first.</param>
    /// <returns>The builder instance for chaining.</returns>
    public JsonPropertyBuilder<T, TProp> HasOrder(int order)
    {
        _definition.Order = order;
        return this;
    }

    /// <summary>
    /// Configures whether the property is mandatory during deserialization.
    /// </summary>
    /// <param name="required">
    /// If <c>true</c>, the deserializer will throw an exception if the property is missing in the JSON payload.
    /// Default is <c>true</c>.
    /// </param>
    /// <returns>The builder instance for chaining.</returns>
    public JsonPropertyBuilder<T, TProp> IsRequired(bool required = true)
    {
        _definition.IsRequired = required;
        return this;
    }

    /// <summary>
    /// Excludes this property entirely from serialization and deserialization.
    /// </summary>
    /// <returns>The builder instance for chaining.</returns>
    public JsonPropertyBuilder<T, TProp> Ignore()
    {
        _definition.Ignored = true;
        return this;
    }
}
