using FluentJson.Core.Metadata;

namespace FluentJson.Core.Builder;

/// <summary>
/// An internal abstraction representing the un-typed configuration phase of a single JSON entity.
/// </summary>
internal interface IEntityTypeBuilder
{
    /// <summary>
    /// Finalizes the configuration for this entity type into an immutable representation.
    /// </summary>
    /// <returns>The fully configured <see cref="IJsonEntity"/>.</returns>
    public IJsonEntity Build();
}
