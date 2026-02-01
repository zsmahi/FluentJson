using FluentJson.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FluentJson.Internal;

/// <summary>
/// A utility class responsible for automatically discovering configuration classes within assemblies.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Service Locator / Scanner.
/// </para>
/// <para>
/// This class encapsulates the reflection logic needed to find all types implementing 
/// <see cref="IJsonEntityTypeConfiguration{T}"/>. It is used by the builder's auto-discovery features 
/// to register all configurations in a single pass.
/// </para>
/// </remarks>
internal static class ConfigurationScanner
{
    /// <summary>
    /// Scans the specified assembly for concrete classes implementing <see cref="IJsonEntityTypeConfiguration{T}"/>.
    /// </summary>
    /// <param name="assembly">The assembly to inspect.</param>
    /// <returns>
    /// An enumerable collection of tuples. 
    /// <c>ConfigType</c> is the concrete configuration class (e.g., <c>UserConfiguration</c>), 
    /// and <c>EntityType</c> is the domain model type it configures (e.g., <c>User</c>).
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="assembly"/> is null.</exception>
    public static IEnumerable<(Type ConfigType, Type EntityType)> FindConfigurations(Assembly assembly)
    {
        if (assembly == null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        Type[] types = GetLoadableTypes(assembly);

        return types
            .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IJsonEntityTypeConfiguration<>))
                .Select(i => (ConfigType: t, EntityType: i.GetGenericArguments()[0]))
            );
    }

    /// <summary>
    /// Retrieves all loadable types from an assembly, gracefully handling incomplete loads.
    /// </summary>
    /// <param name="assembly">The assembly to retrieve types from.</param>
    /// <returns>An array of successfully loaded types.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Architectural Note:</strong>
    /// This method guards against <see cref="ReflectionTypeLoadException"/>. This exception is common 
    /// in complex environments (like plugins or when optional dependencies are missing) where the runtime 
    /// fails to load a subset of types in an assembly. Instead of crashing, this method filters out the 
    /// failing types and returns the valid ones.
    /// </para>
    /// </remarks>
    private static Type[] GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Filter out nulls from ex.Types (which represent types that failed to load)
            return [.. ex.Types.Where(t => t != null).Select(t => t!)];
        }
    }
}
