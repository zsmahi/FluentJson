using System;
using System.Collections.Generic;
using System.Reflection;
using FluentJson.Abstractions;
using FluentJson.Definitions;
using FluentJson.Exceptions;
using FluentJson.Internal;

namespace FluentJson.Builders;

/// <summary>
/// Abstract foundation for JSON configuration builders.
/// Implements the "Template Method" pattern to enforce lifecycle safety (Validation -> Freeze -> Build).
/// </summary>
/// <typeparam name="TSettings">The type of the resulting serializer settings object.</typeparam>
public abstract class JsonModelBuilderBase<TSettings>
{
    // Repository of definitions, keyed by the entity type.
    protected readonly Dictionary<Type, JsonEntityDefinition> _scannedDefinitions = [];

    // Shared configuration state.
    protected NamingConvention _namingConvention = NamingConvention.Default;

    // Lifecycle control flag.
    private volatile bool _isBuilt;

    /// <summary>
    /// Compiles the aggregated configurations into a ready-to-use serializer settings object.
    /// This method enforces validation and immutability of the configuration model.
    /// </summary>
    /// <param name="serviceProvider">
    /// An optional dependency injection provider used to instantiate converters during the build phase.
    /// </param>
    /// <returns>The configured settings object.</returns>
    /// <exception cref="FluentJsonConfigurationException">Thrown if the builder has already been built.</exception>
    /// <exception cref="FluentJsonValidationException">Thrown if the configuration contains logical errors.</exception>
    public TSettings Build(IServiceProvider? serviceProvider = null)
    {
        EnsureNotBuilt();

        // 1. Integrity Check & Locking Phase
        // We iterate over all definitions to validate them against business rules 
        // and freeze them to ensure thread safety before generation.
        foreach (KeyValuePair<Type, JsonEntityDefinition> kvp in _scannedDefinitions)
        {
            Type entityType = kvp.Key;
            JsonEntityDefinition definition = kvp.Value;

            // Fail-Fast: Detect invalid configurations immediately
            ModelValidator.ValidateDefinition(entityType, definition);

            // Thread-Safety: Lock the definition permanently
            definition.Freeze();
        }

        // 2. Generation Phase (Adapter specific logic)
        TSettings settings = BuildEngineSettings(serviceProvider);

        // 3. Lifecycle Finalization
        _isBuilt = true;

        return settings;
    }

    /// <summary>
    /// When implemented in a derived class, translates the frozen definitions into the engine-specific settings object.
    /// </summary>
    /// <param name="serviceProvider">The provider for resolving runtime dependencies (e.g., Converters).</param>
    /// <returns>The concrete settings instance.</returns>
    protected abstract TSettings BuildEngineSettings(IServiceProvider? serviceProvider);

    /// <summary>
    /// Scans the specified assemblies for <see cref="IJsonEntityTypeConfiguration{T}"/> implementations 
    /// and applies them to the builder.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="serviceProvider">Optional provider to resolve configuration classes with dependencies.</param>
    public void ApplyConfigurationsFromAssemblies(IServiceProvider? serviceProvider, params Assembly[] assemblies)
    {
        EnsureNotBuilt();
        if (assemblies == null || assemblies.Length == 0)
        {
            throw new ArgumentNullException(nameof(assemblies));
        }

        Func<Type, object>? providerDelegate = serviceProvider is null
            ? null
            : serviceProvider.GetService;

        // Delegate discovery to the internal service
        IEnumerable<DiscoveredConfiguration> configs = ConfigurationDiscovery.FindAndInstantiateConfigurations(assemblies, providerDelegate);

        foreach (DiscoveredConfiguration config in configs)
        {
            ApplyConfigurationInstance(config.Instance, config.ConfigType, config.EntityType);
        }
    }

    /// <summary>
    /// Overload for convenience without a service provider.
    /// </summary>
    public void ApplyConfigurationsFromAssemblies(params Assembly[] assemblies)
        => ApplyConfigurationsFromAssemblies(null, assemblies);

    /// <summary>
    /// Manually applies a specific configuration instance.
    /// </summary>
    public void ApplyConfiguration<T>(IJsonEntityTypeConfiguration<T> configuration) where T : class
    {
        EnsureNotBuilt();
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        ApplyConfigurationInstance(configuration, configuration.GetType(), typeof(T));
    }

    /// <summary>
    /// Internal helper to execute the 'Configure' method on a configuration instance.
    /// </summary>
    private void ApplyConfigurationInstance(object configInstance, Type configType, Type entityType)
    {
        try
        {
            // 1. Create the specific builder: JsonEntityTypeBuilder<T>
            Type builderType = typeof(JsonEntityTypeBuilder<>).MakeGenericType(entityType);
            object builderInstance = Activator.CreateInstance(builderType)
                ?? throw new FluentJsonConfigurationException($"Failed to create builder context for '{entityType.Name}'.");

            // 2. Invoke the 'Configure' method
            // We know the method exists because configInstance implements IJsonEntityTypeConfiguration<T>
            MethodInfo configureMethod = configType.GetMethod("Configure")!;
            configureMethod.Invoke(configInstance, [builderInstance]);

            // 3. Extract and store the definition via the internal accessor
            if (builderInstance is IJsonEntityTypeBuilderAccessor accessor)
            {
                // Last-write wins strategy for duplicated configurations of the same type
                _scannedDefinitions[entityType] = accessor.Definition;
            }
        }
        catch (TargetInvocationException ex)
        {
            throw new FluentJsonConfigurationException(
                $"Error applying configuration '{configType.Name}': {ex.InnerException?.Message}",
                ex.InnerException ?? ex);
        }
    }

    /// <summary>
    /// Guarantees that the builder is in a mutable state.
    /// </summary>
    protected void EnsureNotBuilt()
    {
        if (_isBuilt)
        {
            throw new FluentJsonConfigurationException(
                "The builder has already been built. Modifications are not allowed after Build() has been called.");
        }
    }
}
