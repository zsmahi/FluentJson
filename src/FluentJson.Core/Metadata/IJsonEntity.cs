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
    public Type EntityType { get; }
    /// <summary>
    /// Gets the read-only list of configured properties and fields for this entity.
    /// </summary>
    public IReadOnlyList<IJsonProperty> Properties { get; }
    /// <summary>
    /// Gets a pre-compiled expression factory to instantiate the entity without using <see cref="System.Runtime.Serialization.FormatterServices.GetUninitializedObject(Type)"/>.
    /// </summary>
    /// <remarks>
    /// This is the core piece that enables safe DDD instantiation with private parameterless constructors.
    /// </remarks>
    public Func<object> ConstructorFactory { get; }

    /// <summary>
    /// Gets the JSON property name used to discriminate derived types.
    /// </summary>
    public string? DiscriminatorPropertyName { get; }

    /// <summary>
    /// Gets the read-only dictionary mapping discriminator values to their concrete derived types.
    /// </summary>
    public IReadOnlyDictionary<object, Type> DerivedTypes { get; }

    /// <summary>
    /// Indicates whether circular references in this entity should be preserved during serialization.
    /// </summary>
    public bool ShouldPreserveReferences { get; }
}
