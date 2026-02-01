using System;

namespace FluentJson.Internal;

/// <summary>
/// Represents the result of a configuration discovery operation.
/// </summary>
internal readonly struct DiscoveredConfiguration(object instance, Type configType, Type entityType)
{
    public object Instance { get; } = instance;
    public Type ConfigType { get; } = configType;
    public Type EntityType { get; } = entityType;
}
