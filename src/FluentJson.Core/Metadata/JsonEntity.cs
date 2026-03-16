using System;
using System.Collections.Generic;

namespace FluentJson.Core.Metadata;

/// <summary>
/// Internal immutable implementation of <see cref="IJsonEntity"/>.
/// </summary>
internal class JsonEntity : IJsonEntity
{
    public Type EntityType { get; }
    public IReadOnlyList<IJsonProperty> Properties { get; }
    public Func<object> ConstructorFactory { get; }
    public string? DiscriminatorPropertyName { get; }
    public IReadOnlyDictionary<object, Type> DerivedTypes { get; }
    public bool ShouldPreserveReferences { get; }

    public JsonEntity(
        Type entityType,
        IReadOnlyList<IJsonProperty> properties,
        Func<object> constructorFactory,
        string? discriminatorPropertyName,
        IReadOnlyDictionary<object, Type> derivedTypes,
        bool shouldPreserveReferences)
    {
        EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
        Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        ConstructorFactory = constructorFactory ?? throw new ArgumentNullException(nameof(constructorFactory));
        DiscriminatorPropertyName = discriminatorPropertyName;
        DerivedTypes = derivedTypes ?? throw new ArgumentNullException(nameof(derivedTypes));
        ShouldPreserveReferences = shouldPreserveReferences;
    }
}
