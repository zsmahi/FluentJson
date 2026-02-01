using FluentJson.Definitions;

namespace FluentJson.Builders;

/// <summary>
/// Internal interface enabling polymorphic management of generic property builders.
/// Allows the EntityTypeBuilder to orchestrate the "Apply" phase without knowing the specific property type.
/// </summary>
internal interface IJsonPropertyBuilderAccessor
{
    /// <summary>
    /// Flushes the builder's temporary state into the final definition.
    /// </summary>
    void Apply(JsonEntityDefinition entityDef);
}
