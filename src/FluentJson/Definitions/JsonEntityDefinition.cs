using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using FluentJson.Abstractions;

namespace FluentJson.Definitions;

/// <summary>
/// Represents the root container for all configuration rules associated with a specific CLR entity type.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Metadata Container / Composite.
/// </para>
/// <para>
/// This class acts as the central repository for the configuration graph. It aggregates individual 
/// <see cref="JsonPropertyDefinition"/> objects and optional <see cref="PolymorphismDefinition"/> 
/// settings. It inherits from <see cref="FreezableBase"/> to ensure thread-safety once the configuration 
/// phase is complete and the serializer starts using it.
/// </para>
/// </remarks>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class JsonEntityDefinition : FreezableBase
{
    // Keyed by MemberInfo to support both Properties and Fields uniformly
    private readonly Dictionary<MemberInfo, JsonPropertyDefinition> _properties = [];

    /// <summary>
    /// Initializes a new instance of the entity definition for the specified type.
    /// </summary>
    /// <param name="entityType">The CLR type of the entity being configured.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="entityType"/> is null.</exception>
    public JsonEntityDefinition(Type entityType)
    {
        if (entityType is null)
        {
            throw new ArgumentNullException(nameof(entityType));
        }

        EntityType = entityType;
    }

    /// <summary>
    /// Gets the CLR type of the entity to which this definition applies.
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    /// Gets the configuration settings for polymorphic deserialization, if enabled.
    /// </summary>
    /// <value>
    /// A <see cref="PolymorphismDefinition"/> instance if polymorphism is configured; otherwise, <c>null</c>.
    /// </value>
    public PolymorphismDefinition? Polymorphism { get; private set; }

    /// <summary>
    /// Gets a read-only view of the configured properties and fields, indexed by their reflection metadata.
    /// </summary>
    public IReadOnlyDictionary<MemberInfo, JsonPropertyDefinition> Properties => _properties;

    /// <summary>
    /// Enables and initializes polymorphic behavior for this entity.
    /// </summary>
    /// <param name="discriminator">The name of the JSON property that will hold the type discriminator value.</param>
    /// <exception cref="InvalidOperationException">Thrown if the definition has already been frozen.</exception>
    public void EnablePolymorphism(string discriminator)
    {
        ThrowIfFrozen();

        if (Polymorphism == null)
        {
            Polymorphism = new PolymorphismDefinition(discriminator);
        }
    }

    /// <summary>
    /// Locks the definition and all its child components to prevent further modifications.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Thread Safety:</strong>
    /// This method must be called before the definition is shared across threads (e.g., during the Build() phase). 
    /// It recursively freezes all contained <see cref="JsonPropertyDefinition"/> and <see cref="PolymorphismDefinition"/> objects.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="EntityType"/> is missing or invalid.</exception>
    public override void Freeze()
    {
        // Validation check
        if (EntityType == null)
        {
            throw new InvalidOperationException("EntityType metadata must be present to freeze.");
        }

        if (IsFrozen)
        {
            return;
        }

        base.Freeze();

        // Recursively freeze children
        foreach (JsonPropertyDefinition propDef in _properties.Values)
        {
            propDef.Freeze();
        }

        if (Polymorphism != null)
        {
            Polymorphism.Freeze();
        }
    }

    /// <summary>
    /// Retrieves the configuration for a specific member, creating it if it does not already exist.
    /// </summary>
    /// <param name="member">The reflection metadata (PropertyInfo or FieldInfo) of the member.</param>
    /// <returns>The mutable <see cref="JsonPropertyDefinition"/> for the specified member.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the definition has already been frozen.</exception>
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
    private string DebuggerDisplay
    {
        get
        {
            string polyInfo = Polymorphism != null ? $" | Polymorphic ('{Polymorphism.DiscriminatorProperty}')" : "";
            return $"Entity: {EntityType.Name} | Props: {_properties.Count}{polyInfo} | Frozen: {IsFrozen}";
        }
    }
}
