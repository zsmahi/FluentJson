using FluentJson.Definitions;

namespace FluentJson.Builders;

/// <summary>
/// Exposes the fluent API for registering derived types and their corresponding discriminator values.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Step Builder / Fluent Interface.
/// </para>
/// <para>
/// This builder constitutes a specific phase in the configuration flow. It is instantiated and returned 
/// immediately after a discriminator property is declared, logically constraining the API consumer 
/// to register subtypes as the next step, ensuring a complete polymorphic configuration.
/// </para>
/// </remarks>
/// <typeparam name="T">The base entity type of the polymorphic hierarchy.</typeparam>
public class JsonDiscriminatorBuilder<T>
{
    private readonly PolymorphismDefinition _definition;

    internal JsonDiscriminatorBuilder(PolymorphismDefinition definition)
    {
        _definition = definition;
    }

    /// <summary>
    /// Registers a mapping between a concrete derived type and its unique discriminator value.
    /// </summary>
    /// <typeparam name="TSubType">The derived type to register. It must inherit from <typeparamref name="T"/>.</typeparam>
    /// <param name="discriminatorValue">
    /// The value (e.g., string literal, integer, or enum) expected in the JSON discriminator property 
    /// to identify this specific type.
    /// </param>
    /// <returns>The current builder instance to allow chaining multiple subtype registrations.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Architectural Note:</strong>
    /// We leverage C# generic constraints (<c>where TSubType : T</c>) to enforce the inheritance relationship 
    /// strictly at compile-time. This proactive validation eliminates an entire category of runtime configuration 
    /// errors where an unrelated type might otherwise be accidentally mapped to the hierarchy.
    /// </para>
    /// </remarks>
    public JsonDiscriminatorBuilder<T> HasSubType<TSubType>(object discriminatorValue) where TSubType : T
    {
        _definition.AddSubType(typeof(TSubType), discriminatorValue);
        return this;
    }
}
