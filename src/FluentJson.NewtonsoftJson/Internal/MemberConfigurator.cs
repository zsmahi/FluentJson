using FluentJson.Definitions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace FluentJson.NewtonsoftJson.Internal;

/// <summary>
/// A static helper that applies abstract configuration rules to concrete Newtonsoft <see cref="JsonProperty"/> objects.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Configurator / Populator.
/// </para>
/// <para>
/// This class encapsulates the mapping logic between the library-agnostic <see cref="JsonPropertyDefinition"/> 
/// and the library-specific <see cref="JsonProperty"/>. It ensures that all fluent settings (renaming, 
/// ordering, ignoring, converters) are correctly translated into the Newtonsoft contract.
/// </para>
/// </remarks>
internal static class MemberConfigurator
{
    /// <summary>
    /// Applies the configuration definition to the specified JSON property.
    /// </summary>
    /// <param name="property">The target Newtonsoft property object to configure.</param>
    /// <param name="def">The source configuration definition containing user rules.</param>
    /// <param name="currentMember">The original member (property) being processed.</param>
    public static void Apply(JsonProperty property, JsonPropertyDefinition def, MemberInfo currentMember)
    {
        // 1. Check for exclusion first
        if (def.Ignored)
        {
            property.Ignored = true;
            return;
        }

        // 2. Map basic metadata
        if (def.JsonName != null)
        {
            property.PropertyName = def.JsonName;
        }

        if (def.Order.HasValue)
        {
            property.Order = def.Order;
        }

        if (def.IsRequired.HasValue)
        {
            property.Required = def.IsRequired.Value ? Required.Always : Required.Default;
        }

        // 3. Instantiate and assign converters
        if (def.ConverterDefinition != null)
        {
            property.Converter = ConverterFactory.Create(def.ConverterDefinition);
        }

        // 4. Configure Data Access (Value Provider)
        // Determine if we should read/write to the Property or a Backing Field
        MemberInfo targetMember = (MemberInfo?)def.BackingField ?? currentMember;

        // Optimization:
        // We replace the default reflection-based provider with our compiled expression provider.
        // This boosts performance for both standard properties and private fields.
        property.ValueProvider = new FluentExpressionValueProvider(targetMember);

        // 5. Enforce DDD Encapsulation
        // If a backing field is targeted, we must ensure the serializer attempts to write to it,
        // even if the public property was read-only or the field is private.
        if (def.BackingField != null)
        {
            property.Writable = true;
            property.Readable = true;
        }
    }
}
