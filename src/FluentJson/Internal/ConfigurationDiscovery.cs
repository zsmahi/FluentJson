using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using FluentJson.Abstractions;
using FluentJson.Exceptions;

namespace FluentJson.Internal;

/// <summary>
/// Internal service responsible for discovering and instantiating configuration classes.
/// Decouples the scanning logic from the builder lifecycle.
/// </summary>
internal static class ConfigurationDiscovery
{
    /// <summary>
    /// Scans the provided assemblies for implementations of <see cref="IJsonEntityTypeConfiguration{T}"/> 
    /// and returns fully instantiated configuration objects.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="serviceProvider">Optional DI provider for resolving configuration dependencies.</param>
    /// <returns>A collection of discovered configurations ready for application.</returns>
    /// <exception cref="FluentJsonConfigurationException">Thrown if instantiation fails.</exception>
    [RequiresUnreferencedCode("This method scans assemblies via Reflection and instantiates types dynamically. " +
                              "If using NativeAOT, ensure that configuration classes are preserved from trimming.")]
    public static IEnumerable<DiscoveredConfiguration> FindAndInstantiateConfigurations(
        IEnumerable<Assembly> assemblies,
        Func<Type, object>? serviceProvider)
    {
        foreach (Assembly assembly in assemblies)
        {
            var configTypes = FindConfigurationTypes(assembly);

            foreach (var (configType, entityType) in configTypes)
            {
                object configInstance = CreateConfigurationInstance(configType, serviceProvider);
                yield return new DiscoveredConfiguration(configInstance, configType, entityType);
            }
        }
    }

    /// <summary>
    /// Identifies all concrete types implementing the generic configuration interface.
    /// handles <see cref="ReflectionTypeLoadException"/> gracefully to support complex assembly graphs.
    /// </summary>
    [RequiresUnreferencedCode("Reflection Scanning requires access to types that might be trimmed.")]
    private static IEnumerable<(Type ConfigType, Type EntityType)> FindConfigurationTypes(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Filter out types that failed to load
            types = ex.Types.Where(t => t != null).Select(t => t!).ToArray();
        }

        return types
            .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IJsonEntityTypeConfiguration<>))
                .Select(i => (ConfigType: t, EntityType: i.GetGenericArguments()[0]))
            );
    }

    [RequiresUnreferencedCode("Calls Activator.CreateInstance which requires dynamic type access.")]
    private static object CreateConfigurationInstance(Type configType, Func<Type, object>? serviceProvider)
    {
        // 1. Attempt resolution via Dependency Injection
        if (serviceProvider != null)
        {
            try
            {
                var instance = serviceProvider(configType);
                if (instance != null) return instance;
            }
            catch (Exception ex)
            {
                throw new FluentJsonConfigurationException(
                    $"The provided ServiceProvider failed to resolve configuration type '{configType.Name}'.", ex);
            }
        }

        // 2. Fallback to Activator (parameterless constructor)
        try
        {
            // Explicit warning suppression or acknowledgment via attribute on method
            return Activator.CreateInstance(configType)
                ?? throw new FluentJsonConfigurationException($"Activator failed to instantiate '{configType.Name}'.");
        }
        catch (MissingMethodException)
        {
            throw new FluentJsonConfigurationException(
                $"The configuration class '{configType.Name}' does not have a public parameterless constructor. " +
                "If this class requires dependencies, ensure a valid 'serviceProvider' delegate is passed to the builder.");
        }
        catch (TargetInvocationException ex)
        {
            throw new FluentJsonConfigurationException(
                $"The constructor of configuration '{configType.Name}' threw an exception.", ex.InnerException ?? ex);
        }
    }
}
