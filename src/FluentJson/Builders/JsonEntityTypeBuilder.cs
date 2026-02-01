using FluentJson.Definitions;
using FluentJson.Internal;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace FluentJson.Builders;

/// <summary>
/// Serves as the primary entry point for the fluent configuration API of a specific entity type <typeparamref name="T"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Fluent Builder / Facade.
/// </para>
/// <para>
/// This class exposes a strongly-typed API to define serialization rules (property mappings, exclusions, 
/// polymorphism) for the domain entity <typeparamref name="T"/>. It abstracts the complexity of the underlying 
/// <see cref="JsonEntityDefinition"/> container, allowing users to write readable, declarative configuration code.
/// </para>
/// </remarks>
/// <typeparam name="T">The entity type being configured. Must be a class.</typeparam>
public class JsonEntityTypeBuilder<T> : IJsonEntityTypeBuilderAccessor where T : class
{
    /// <summary>
    /// Initializes a new instance of the builder with a fresh, empty definition container.
    /// </summary>
    public JsonEntityTypeBuilder()
    {
        Definition = new JsonEntityDefinition(typeof(T));
    }

    // Explicit implementation to hide the internal definition from the public API surface.
    JsonEntityDefinition IJsonEntityTypeBuilderAccessor.Definition => Definition;

    internal JsonEntityDefinition Definition { get; }

    /// <summary>
    /// Configures the property used as the discriminator to identify derived types during polymorphic deserialization.
    /// </summary>
    /// <typeparam name="TProp">The type of the selected property.</typeparam>
    /// <param name="propertyExpression">
    /// A lambda expression selecting the property (e.g., <c>x => x.Type</c>). 
    /// This ensures refactoring safety: renaming the property in the IDE will automatically update the string name used internally.
    /// </param>
    /// <returns>
    /// A specialized <see cref="JsonDiscriminatorBuilder{T}"/> instance. 
    /// This return type guides the user to the next logical step: mapping the subtypes.
    /// </returns>
    public JsonDiscriminatorBuilder<T> HasDiscriminator<TProp>(Expression<Func<T, TProp>> propertyExpression)
    {
        // Internal Note: TypeHelper handles complex expression trees (e.g., boxing/casting) to reliably extract MemberInfo.
        MemberInfo member = TypeHelper.GetMember(propertyExpression);
        return HasDiscriminator(member.Name);
    }

    /// <summary>
    /// Configures the discriminator property by its raw name.
    /// </summary>
    /// <param name="propertyName">The case-sensitive name of the JSON property acting as the discriminator.</param>
    /// <returns>A specialized <see cref="JsonDiscriminatorBuilder{T}"/> instance to continue the configuration flow.</returns>
    /// <remarks>
    /// <para>
    /// Use this overload when the discriminator does not map to a public property on the class 
    /// (e.g., a "type" field that exists only in the JSON payload, or a private field).
    /// </para>
    /// </remarks>
    public JsonDiscriminatorBuilder<T> HasDiscriminator(string propertyName)
    {
        Definition.EnablePolymorphism(propertyName);

        // Architectural Note:
        // We return a specialized builder instead of 'this' to enforce a "Wizard-like" flow.
        // It prevents invalid states (e.g., declaring a discriminator but forgetting to add subtypes).
        return new JsonDiscriminatorBuilder<T>(Definition.Polymorphism!);
    }

    /// <summary>
    /// Excludes the specified property from both serialization and deserialization.
    /// </summary>
    /// <typeparam name="TProp">The type of the property.</typeparam>
    /// <param name="propertyExpression">A lambda expression selecting the property to ignore.</param>
    public void Ignore<TProp>(Expression<Func<T, TProp>> propertyExpression)
    {
        MemberInfo member = TypeHelper.GetMember(propertyExpression);
        Definition.GetOrCreateMember(member).Ignored = true;
    }

    /// <summary>
    /// Configures a discriminator that exists only in the JSON payload, not on the CLR class.
    /// </summary>
    public JsonDiscriminatorBuilder<T> HasShadowDiscriminator(string jsonPropertyName)
    {
        Definition.EnablePolymorphism(jsonPropertyName, isShadowProperty: true);
        return new JsonDiscriminatorBuilder<T>(Definition.Polymorphism!);
    }

    /// <summary>
    /// Selects a specific property to configure its serialization settings (renaming, ordering, converters).
    /// </summary>
    /// <typeparam name="TProp">The type of the property being configured.</typeparam>
    /// <param name="propertyExpression">A lambda expression selecting the property.</param>
    /// <returns>A specialized <see cref="JsonPropertyBuilder{T, TProp}"/> instance focused on the selected property.</returns>
    public JsonPropertyBuilder<T, TProp> Property<TProp>(Expression<Func<T, TProp>> propertyExpression)
    {
        MemberInfo member = TypeHelper.GetMember(propertyExpression);
        return new JsonPropertyBuilder<T, TProp>(Definition, member);
    }
}
