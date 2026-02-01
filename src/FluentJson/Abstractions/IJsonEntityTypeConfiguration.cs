using FluentJson.Builders;

namespace FluentJson.Abstractions;

/// <summary>
/// Defines a dedicated configuration strategy for a specific entity type <typeparamref name="T"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Separation of Concerns / External Configuration.
/// </para>
/// <para>
/// This interface enables the "Code-First" configuration style, allowing domain models (POCOs) to remain 
/// agnostic of serialization concerns (no attributes required). It mirrors the architecture found in 
/// modern ORMs like Entity Framework Core.
/// </para>
/// <para>
/// <strong>Architectural Note:</strong>
/// Implementations are intended to be stateless and discovered automatically via assembly scanning. 
/// The <see cref="Configure"/> method is executed strictly once during the application startup (Build phase), 
/// and the resulting metadata is frozen to ensure high-performance, thread-safe access during runtime.
/// </para>
/// </remarks>
/// <typeparam name="T">The class type being configured.</typeparam>
public interface IJsonEntityTypeConfiguration<T> where T : class
{
    /// <summary>
    /// Applies the serialization rules (property mapping, converters, polymorphism) to the entity builder.
    /// </summary>
    /// <param name="builder">The fluent builder instance exposing the configuration API for <typeparamref name="T"/>.</param>
    void Configure(JsonEntityTypeBuilder<T> builder);
}
