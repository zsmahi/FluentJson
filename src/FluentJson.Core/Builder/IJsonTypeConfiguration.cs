namespace FluentJson.Core.Builder;

/// <summary>
/// Allows configuration for an entity type to be factored into a separate class, promoting modularity and Clean Architecture.
/// </summary>
/// <typeparam name="TEntity">The entity type to be configured.</typeparam>
/// <remarks>
/// Implement this interface to decouple the serialization rules from the Domain Model completely.
/// The standard approach is to use `ApplyConfigurationsFromAssembly` to discover these classes automatically.
/// </remarks>
public interface IJsonTypeConfiguration<TEntity> where TEntity : class
{
    /// <summary>
    /// Configures the entity utilizing the provided builder API.
    /// </summary>
    /// <param name="builder">The builder to be used to map and configure the JSON entity.</param>
    void Configure(EntityTypeBuilder<TEntity> builder);
}
