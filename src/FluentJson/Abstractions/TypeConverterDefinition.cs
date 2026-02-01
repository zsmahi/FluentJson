using System;

namespace FluentJson.Abstractions;

/// <summary>
/// Represents a conversion strategy defined by a concrete converter class type.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Metadata / Deferred Instantiation.
/// </para>
/// <para>
/// This definition maps to the traditional <c>[JsonConverter(typeof(T))]</c> approach found in both Newtonsoft and System.Text.Json.
/// </para>
/// <para>
/// <strong>Architectural Note:</strong>
/// We store the <see cref="Type"/> metadata here rather than an instantiated object. This design defers 
/// object creation until the "Build" phase. It allows the concrete serializer adapter to choose the appropriate 
/// resolution strategy—using <see cref="Activator.CreateInstance(Type)"/> for simple scenarios, or a 
/// <see cref="IServiceProvider"/> for scenarios requiring Dependency Injection (DI) inside converters.
/// </para>
/// </remarks>
/// <param name="converterType">The CLR type of the converter (e.g., a class inheriting from <c>JsonConverter</c>).</param>
public class TypeConverterDefinition(Type converterType) : IConverterDefinition
{
    /// <summary>
    /// Gets the concrete type of the converter to be resolved and instantiated by the serializer adapter.
    /// </summary>
    public Type ConverterType { get; } = converterType;

    /// <inheritdoc />
    // We cannot determine the target model type statically from just the converter type 
    // without complex reflection, so we return null to skip type validation.
    public Type? ModelType => null;
}
