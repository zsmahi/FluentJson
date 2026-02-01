using FluentJson.Definitions;
using FluentJson.Internal;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace FluentJson.Builders;

/// <summary>
/// Serves as the primary entry point for the fluent configuration API of a specific entity type <typeparamref name="T"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Fluent Builder / Facade / Orchestrator.
/// </para>
/// <para>
/// This class exposes a strongly-typed API to define serialization rules.
/// Unlike a simple pass-through wrapper, it acts as a <strong>State Container</strong>. It caches 
/// individual property builders to ensure that multiple calls to configure the same property 
/// (e.g. <c>.HasName()</c> then <c>.HasOrder()</c>) operate on the same builder instance.
/// </para>
/// </remarks>
/// <typeparam name="T">The entity type being configured. Must be a class.</typeparam>
public class JsonEntityTypeBuilder<T> : IJsonEntityTypeBuilderAccessor where T : class
{
    // The master container.
    private readonly JsonEntityDefinition _definition;

    // Builder Cache: Ensures we return the same builder instance for repeated calls on the same property.
    // This maintains state continuity and allows the "Deferred Execution" pattern.
    private readonly Dictionary<MemberInfo, IJsonPropertyBuilderAccessor> _propertyBuilders = [];

    /// <summary>
    /// Initializes a new instance of the builder with a fresh, empty definition container.
    /// </summary>
    public JsonEntityTypeBuilder()
    {
        _definition = new JsonEntityDefinition(typeof(T));
    }

    // Explicit implementation: This is called by the JsonModelBuilder at the end of the configuration phase.
    JsonEntityDefinition IJsonEntityTypeBuilderAccessor.Definition
    {
        get
        {
            // COMMIT PHASE:
            // Before returning the definition to the engine, we flush all pending changes 
            // from the property builders into the definition.
            foreach (IJsonPropertyBuilderAccessor builder in _propertyBuilders.Values)
            {
                builder.Apply(_definition);
            }
            return _definition;
        }
    }

    internal JsonEntityDefinition Definition => _definition;

    /// <summary>
    /// Configures the property used as the discriminator to identify derived types during polymorphic deserialization.
    /// </summary>
    /// <typeparam name="TProp">The type of the selected property.</typeparam>
    /// <param name="propertyExpression">A lambda expression selecting the property.</param>
    /// <returns>A specialized <see cref="JsonDiscriminatorBuilder{T}"/> instance.</returns>
    public JsonDiscriminatorBuilder<T> HasDiscriminator<TProp>(Expression<Func<T, TProp>> propertyExpression)
    {
        MemberInfo member = TypeHelper.GetMember(propertyExpression);
        return HasDiscriminator(member.Name);
    }

    /// <summary>
    /// Configures the discriminator property by its raw name.
    /// </summary>
    public JsonDiscriminatorBuilder<T> HasDiscriminator(string propertyName)
    {
        _definition.EnablePolymorphism(propertyName);
        return new JsonDiscriminatorBuilder<T>(_definition.Polymorphism!);
    }

    /// <summary>
    /// Configures a discriminator that exists ONLY in the JSON payload (Shadow Property).
    /// </summary>
    public JsonDiscriminatorBuilder<T> HasShadowDiscriminator(string jsonPropertyName)
    {
        _definition.EnablePolymorphism(jsonPropertyName, isShadowProperty: true);
        return new JsonDiscriminatorBuilder<T>(_definition.Polymorphism!);
    }

    /// <summary>
    /// Excludes the specified property from both serialization and deserialization.
    /// </summary>
    public void Ignore<TProp>(Expression<Func<T, TProp>> propertyExpression)
    {
        // Delegate to the property builder to ensure consistency with the caching mechanism.
        Property(propertyExpression).Ignore();
    }

    /// <summary>
    /// Selects a specific property to configure its serialization settings.
    /// Uses a caching mechanism to preserve configuration state across multiple calls.
    /// </summary>
    /// <typeparam name="TProp">The type of the property being configured.</typeparam>
    /// <param name="propertyExpression">A lambda expression selecting the property.</param>
    /// <returns>The cached builder instance for the selected property.</returns>
    public JsonPropertyBuilder<T, TProp> Property<TProp>(Expression<Func<T, TProp>> propertyExpression)
    {
        MemberInfo member = TypeHelper.GetMember(propertyExpression);

        // Check Cache: Have we already started configuring this property?
        if (_propertyBuilders.TryGetValue(member, out IJsonPropertyBuilderAccessor? existingBuilder))
        {
            // Safe Cast: We assume the expression selects the same type TProp for the same MemberInfo.
            return (JsonPropertyBuilder<T, TProp>)existingBuilder;
        }

        // Create new State Container (Builder) independent of the Definition
        var builder = new JsonPropertyBuilder<T, TProp>(member);

        // Cache it for future retrieval
        _propertyBuilders[member] = builder;

        return builder;
    }
}
