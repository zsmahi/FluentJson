using System;
using System.Collections.Generic;
using System.Reflection;
using FluentJson.Abstractions;
using FluentJson.Definitions;
using FluentJson.Internal;

namespace FluentJson.Builders;

/// <summary>
/// Serves as the abstract foundation for implementing JSON configuration builders.
/// It centralizes the logic for configuration discovery, scanning, and definition aggregation.
/// </summary>
/// <typeparam name="TSettings">
/// The type of the final configuration object produced by the builder 
/// (e.g., <c>JsonSerializerSettings</c> for Newtonsoft or <c>JsonSerializerOptions</c> for System.Text.Json).
/// </typeparam>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Template Method / Layer Supertype.
/// </para>
/// <para>
/// This class handles the heavy lifting of reflection-based discovery and definition management.
/// Concrete implementations (adapters) only need to implement the <see cref="Build"/> method 
/// to translate the agnostic <see cref="JsonEntityDefinition"/> model into the specific 
/// settings object required by the underlying JSON engine.
/// </para>
/// </remarks>
public abstract class JsonModelBuilderBase<TSettings>
{
    // Centralized definition storage accessible by child implementations
    protected readonly Dictionary<Type, JsonEntityDefinition> _scannedDefinitions = [];

    // Shared state
    protected NamingConvention _namingConvention = NamingConvention.Default;
    protected bool _isBuilt;

    /// <summary>
    /// When implemented in a derived class, compiles the aggregated definitions into the engine-specific settings object.
    /// </summary>
    /// <returns>The fully configured settings object ready for use by the serializer.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the builder has already been built.</exception>
    public abstract TSettings Build();

    /// <summary>
    /// Verifies that the builder is in a mutable state.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="Build"/> has already been called.</exception>
    protected void EnsureNotBuilt()
    {
        if (_isBuilt) throw new InvalidOperationException("JsonModelBuilder cannot be modified after Build().");
    }

    #region Configuration Scanning (Shared Logic)

    /// <summary>
    /// Scans the specified assemblies for implementations of <see cref="IJsonEntityTypeConfiguration{T}"/> 
    /// and applies them to the builder using default constructors.
    /// </summary>
    /// <param name="assemblies">The list of assemblies to scan.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="assemblies"/> is null.</exception>
    public void ApplyConfigurationsFromAssemblies(params Assembly[] assemblies)
        => ApplyConfigurationsFromAssemblies(null, assemblies);

    /// <summary>
    /// Scans the specified assemblies for configuration classes and instantiates them using the provided service provider.
    /// </summary>
    /// <param name="serviceProvider">
    /// An optional delegate to resolve dependencies for configuration classes (e.g., <c>serviceProvider.GetService</c>). 
    /// If null, <see cref="Activator.CreateInstance(Type)"/> is used.
    /// </param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <exception cref="InvalidOperationException">Thrown if a configuration class cannot be instantiated.</exception>
    public void ApplyConfigurationsFromAssemblies(Func<Type, object>? serviceProvider, params Assembly[] assemblies)
    {
        EnsureNotBuilt();
        if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));

        foreach (Assembly assembly in assemblies)
        {
            ApplyConfigurationsFromAssembly(assembly, serviceProvider);
        }
    }

    /// <summary>
    /// Applies a specific configuration instance manually.
    /// </summary>
    /// <typeparam name="T">The entity type being configured.</typeparam>
    /// <param name="configuration">The configuration instance to apply.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="configuration"/> is null.</exception>
    public void ApplyConfiguration<T>(IJsonEntityTypeConfiguration<T> configuration) where T : class
    {
        EnsureNotBuilt();
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        Type entityType = typeof(T);
        var builder = new JsonEntityTypeBuilder<T>();

        // User logic execution
        configuration.Configure(builder);

        // Extract definition via internal interface
        if (builder is IJsonEntityTypeBuilderAccessor accessor)
        {
            _scannedDefinitions[entityType] = accessor.Definition;
        }
    }

    /// <summary>
    /// Scans a single assembly for configurations.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="serviceProvider">Optional service provider for DI resolution.</param>
    public void ApplyConfigurationsFromAssembly(Assembly assembly, Func<Type, object>? serviceProvider = null)
    {
        EnsureNotBuilt();
        // Uses the internal scanner from the Core project
        IEnumerable<(Type ConfigType, Type EntityType)> configs = ConfigurationScanner.FindConfigurations(assembly);

        foreach ((Type configType, Type entityType) in configs)
        {
            ProcessConfiguration(configType, entityType, serviceProvider);
        }
    }

    private void ProcessConfiguration(Type configType, Type entityType, Func<Type, object>? serviceProvider)
    {
        try
        {
            // 1. Create configuration instance (DI or Activator)
            object configInstance = CreateConfigurationInstance(configType, serviceProvider);

            // 2. Create generic builder JsonEntityTypeBuilder<T>
            Type builderType = typeof(JsonEntityTypeBuilder<>).MakeGenericType(entityType);
            object builderInstance = Activator.CreateInstance(builderType)
                ?? throw new InvalidOperationException($"Failed to create builder for '{entityType.Name}'.");

            // 3. Invoke Configure method via reflection
            MethodInfo? configureMethod = configType.GetMethod("Configure");
            if (configureMethod is null)
                throw new InvalidOperationException($"Method 'Configure' is missing on configuration '{configType.Name}'.");

            configureMethod.Invoke(configInstance, [builderInstance]);

            // 4. Store result
            if (builderInstance is IJsonEntityTypeBuilderAccessor accessor)
            {
                _scannedDefinitions[entityType] = accessor.Definition;
            }
        }
        catch (TargetInvocationException ex)
        {
            throw new InvalidOperationException(
                $"Error applying configuration for entity '{entityType.Name}': {ex.InnerException?.Message}",
                ex.InnerException ?? ex);
        }
    }

    private static object CreateConfigurationInstance(Type configType, Func<Type, object>? serviceProvider)
    {
        // Try via DI
        if (serviceProvider != null)
        {
            object? instance = serviceProvider(configType);
            if (instance != null) return instance;
            throw new InvalidOperationException($"The provided serviceFactory returned null for type '{configType.Name}'.");
        }

        // Fallback via Activator
        try
        {
            return Activator.CreateInstance(configType)
                ?? throw new InvalidOperationException($"Could not instantiate '{configType.Name}'.");
        }
        catch (MissingMethodException)
        {
            throw new InvalidOperationException(
                $"The configuration class '{configType.Name}' does not have a parameterless constructor. " +
                $"If this class relies on dependencies, you must pass a 'serviceProvider' delegate to 'ApplyConfigurationsFromAssembly'.");
        }
    }

    #endregion
}
