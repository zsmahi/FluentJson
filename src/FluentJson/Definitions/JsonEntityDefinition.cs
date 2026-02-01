// ... (imports standards)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using FluentJson.Abstractions;
using FluentJson.Exceptions;

namespace FluentJson.Definitions;

/// <summary>
/// Represents the root container for all configuration rules associated with a specific CLR entity type.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class JsonEntityDefinition : FreezableBase
{
    private readonly Dictionary<MemberInfo, JsonPropertyDefinition> _properties = [];

    public JsonEntityDefinition(Type entityType)
    {
        if (entityType is null) throw new ArgumentNullException(nameof(entityType));
        EntityType = entityType;
    }

    public Type EntityType { get; }

    public PolymorphismDefinition? Polymorphism { get; private set; }

    public IReadOnlyDictionary<MemberInfo, JsonPropertyDefinition> Properties => _properties;

    public void EnablePolymorphism(string discriminator, bool isShadowProperty = false)
    {
        ThrowIfFrozen();
        if (Polymorphism == null)
        {
            Polymorphism = new PolymorphismDefinition(discriminator)
            {
                IsShadowProperty = isShadowProperty
            };
        }
    }

    public override void Freeze()
    {
        if (EntityType == null) throw new FluentJsonConfigurationException("EntityType metadata must be present to freeze.");
        if (IsFrozen) return;

        base.Freeze();

        foreach (JsonPropertyDefinition propDef in _properties.Values)
        {
            propDef.Freeze();
        }

        if (Polymorphism != null)
        {
            Polymorphism.Freeze();
        }
    }

    public JsonPropertyDefinition GetOrCreateMember(MemberInfo member)
    {
        ThrowIfFrozen();
        if (!_properties.TryGetValue(member, out JsonPropertyDefinition def))
        {
            def = new JsonPropertyDefinition(member);
            _properties[member] = def;
        }
        return def;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay =>
        $"Entity: {EntityType.Name} | Props: {_properties.Count} | Frozen: {IsFrozen}";
}
