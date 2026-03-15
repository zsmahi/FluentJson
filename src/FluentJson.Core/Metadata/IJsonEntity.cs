using System;
using System.Collections.Generic;

namespace FluentJson.Core.Metadata;

/// <summary>
/// Represents the compiled metadata for a specific entity type, containing all its properties and mapping rules.
/// </summary>
/// <remarks>
/// Instances of this interface are deeply immutable and highly optimized for runtime reflection.
/// </remarks>
public interface IJsonEntity
{
    /// <summary>
    /// Gets the CLR type of the configured entity.
    /// </summary>
    Type EntityType { get; }
    /// <summary>
    /// Gets the read-only list of configured properties and fields for this entity.
    /// </summary>
    IReadOnlyList<IJsonProperty> Properties { get; }
    /// <summary>
    /// Gets a pre-compiled expression factory to instantiate the entity without using <see cref="System.Runtime.Serialization.FormatterServices.GetUninitializedObject(Type)"/>.
    /// </summary>
    /// <remarks>
    /// This is the core piece that enables safe DDD instantiation with private parameterless constructors.
    /// </remarks>
    Func<object> ConstructorFactory { get; }
}
