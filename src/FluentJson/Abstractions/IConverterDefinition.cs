using System;

namespace FluentJson.Abstractions;

/// <summary>
/// Represents the abstract configuration for a custom value conversion strategy.
/// </summary>
public interface IConverterDefinition
{
    /// <summary>
    /// Gets the CLR type of the model property that this converter handles.
    /// Returns <c>null</c> if the type cannot be determined statically (e.g. for generic type converters).
    /// </summary>
    Type? ModelType { get; }
}
