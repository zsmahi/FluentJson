using System;
using System.Collections.Generic;
using System.Linq;

using FluentJson.Core.Metadata;

namespace FluentJson.Core.Builder;

/// <summary>
/// The root builder used to configure the JSON serialization model. 
/// It acts as the entry point to define entities, apply configurations, and eventually build the immutable <see cref="IJsonModel"/>.
/// </summary>
public class JsonModelBuilder
{
    private readonly Dictionary<Type, object> _entityBuilders = new();

    /// <summary>
    /// Builds and freezes the configuration into an immutable <see cref="IJsonModel"/>.
    /// </summary>
    /// <returns>A finalized, read-only JSON metadata model.</returns>
    /// <remarks>
    /// Ensures the configuration is frozen to prevent runtime mutation and guarantee thread safety during serialization.
    /// </remarks>
    public IJsonModel Build()
    {
        var entities = _entityBuilders.Values
            .Cast<IEntityTypeBuilder>()
            .Select(b => b.Build())
            .ToList()
            .AsReadOnly();

        return new JsonModel(entities);
    }

    /// <summary>
    /// Begins configuration for a specific entity type using the Fluent API.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity to configure.</typeparam>
    /// <returns>An <see cref="EntityTypeBuilder{TEntity}"/> used to configure mapping rules.</returns>
    public EntityTypeBuilder<TEntity> Entity<TEntity>()
    {
        var type = typeof(TEntity);
        if (!_entityBuilders.TryGetValue(type, out var builder))
        {
            builder = new EntityTypeBuilder<TEntity>();
            _entityBuilders[type] = builder;
        }

        return (EntityTypeBuilder<TEntity>)builder;
    }

    /// <summary>
    /// Applies configuration from a separate <see cref="IJsonTypeConfiguration{TEntity}"/> instance.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity to configure.</typeparam>
    /// <param name="configuration">The decoupled configuration block to apply.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public JsonModelBuilder ApplyConfiguration<TEntity>(IJsonTypeConfiguration<TEntity> configuration) where TEntity : class
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        configuration.Configure(Entity<TEntity>());
        return this;
    }

    /// <summary>
    /// Scans a given assembly and automatically applies all classes that implement <see cref="IJsonTypeConfiguration{TEntity}"/>.
    /// </summary>
    /// <param name="assembly">The assembly to scan for configurations.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    /// <remarks>
    /// Extremely useful for Enterprise architectures where configurations are distributed across many decoupled modules.
    /// </remarks>
    public JsonModelBuilder ApplyConfigurationsFromAssembly(System.Reflection.Assembly assembly)
    {
        if (assembly == null) throw new ArgumentNullException(nameof(assembly));

        var configType = typeof(IJsonTypeConfiguration<>);

        var types = assembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsClass)
            .SelectMany(t => t.GetInterfaces(), (t, i) => new { Type = t, Interface = i })
            .Where(x => x.Interface.IsGenericType && x.Interface.GetGenericTypeDefinition() == configType)
            .ToList();

        foreach (var typeInfo in types)
        {
            try
            {
                var instance = Activator.CreateInstance(typeInfo.Type);

                var entityType = typeInfo.Interface.GetGenericArguments()[0];

                var applyConfigMethod = typeof(JsonModelBuilder)
                    .GetMethod(nameof(ApplyConfiguration))!
                    .MakeGenericMethod(entityType);

                applyConfigMethod.Invoke(this, new[] { instance });
            }
            catch (MissingMethodException ex)
            {
                throw new ArgumentException($"The configuration type '{typeInfo.Type.Name}' does not have a public parameterless constructor.", nameof(assembly), ex);
            }
        }

        return this;
    }
}
