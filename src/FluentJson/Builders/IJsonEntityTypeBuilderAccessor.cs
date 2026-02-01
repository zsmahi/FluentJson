using FluentJson.Definitions;

namespace FluentJson.Builders;

/// <summary>
/// Defines an internal contract for extracting the constructed configuration model from a builder instance.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Encapsulation / Interface Segregation.
/// </para>
/// <para>
/// This interface serves as a bridge between the public fluent API and the internal configuration engine.
/// It allows the <see cref="JsonEntityTypeBuilder{T}"/> to hide its raw data structure (<see cref="JsonEntityDefinition"/>) 
/// from the end-user's IntelliSense, ensuring a clean "write-only" fluent experience, while granting 
/// the core infrastructure read access to the compiled metadata.
/// </para>
/// </remarks>
internal interface IJsonEntityTypeBuilderAccessor
{
    /// <summary>
    /// Gets the raw definition model containing all accumulated configuration rules (properties, polymorphism, etc.).
    /// </summary>
    JsonEntityDefinition Definition { get; }
}
