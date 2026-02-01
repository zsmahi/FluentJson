using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentJson.Abstractions;
using FluentJson.Definitions;
using FluentJson.Exceptions;

namespace FluentJson.Internal;

/// <summary>
/// Static validator responsible for enforcing consistency and correctness rules on the configuration model.
/// Implements the "Fail-Fast" principle to prevent runtime errors.
/// </summary>
internal static class ModelValidator
{
    /// <summary>
    /// Validates the consistency of the entity configuration.
    /// </summary>
    /// <param name="entityType">The root type of the entity being configured.</param>
    /// <param name="definition">The raw configuration definition to validate.</param>
    /// <exception cref="FluentJsonValidationException">Thrown if any rule is violated.</exception>
    public static void ValidateDefinition(Type entityType, JsonEntityDefinition definition)
    {
        // 1. Polymorphism Integrity
        if (definition.Polymorphism != null)
        {
            ValidatePolymorphism(entityType, definition.Polymorphism);
        }

        // 2. Global Property Consistency (Cross-property validation)
        ValidateJsonNameUniqueness(entityType, definition.Properties.Values);

        // 3. Individual Property Validation
        foreach (var kvp in definition.Properties)
        {
            ValidateProperty(entityType, kvp.Key, kvp.Value);
        }
    }

    private static void ValidateJsonNameUniqueness(Type entityType, IEnumerable<JsonPropertyDefinition> properties)
    {
        // Check for duplicate JSON keys (e.g. two properties mapped to "id")
        var duplicates = properties
            .Where(p => !p.Ignored && p.JsonName != null)
            .GroupBy(p => p.JsonName)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            string names = string.Join(", ", duplicates);
            throw new FluentJsonValidationException(
                $"Configuration error on '{entityType.Name}': Duplicate JSON property names detected: [{names}]. " +
                "Ensure that 'HasJsonPropertyName' is unique for each property.");
        }
    }

    private static void ValidateProperty(Type entityType, MemberInfo member, JsonPropertyDefinition def)
    {
        // Rule A: Logical conflict (Ignored + Required)
        if (def.Ignored && def.IsRequired == true)
        {
            throw new FluentJsonValidationException(
                $"Configuration conflict on '{entityType.Name}.{member.Name}': " +
                "A property cannot be both 'Ignored' and 'Required'.");
        }

        // Rule B: Useless configuration (Ignored + Converter)
        if (def.Ignored && def.ConverterDefinition != null)
        {
            throw new FluentJsonValidationException(
                $"Configuration warning on '{entityType.Name}.{member.Name}': " +
                "A Converter has been defined for an 'Ignored' property. Remove 'Ignore()' or the converter.");
        }

        // Rule C: Backing Field Structural Validation
        if (def.BackingField != null)
        {
            Type? fieldDeclaringType = def.BackingField.DeclaringType;
            bool isFieldAccessible = fieldDeclaringType != null && fieldDeclaringType.IsAssignableFrom(entityType);

            if (!isFieldAccessible)
            {
                throw new FluentJsonValidationException(
                    $"Invalid configuration on '{entityType.Name}.{member.Name}': " +
                    $"The mapped backing field '{def.BackingField.Name}' belongs to '{fieldDeclaringType?.Name}', " +
                    $"which is not compatible with the configured entity '{entityType.Name}'.");
            }
        }

        // Rule D: Converter Compatibility
        if (def.ConverterDefinition != null)
        {
            ValidateConverter(entityType, member, def.ConverterDefinition);
        }
    }

    private static void ValidateConverter(Type entityType, MemberInfo member, IConverterDefinition converterDef)
    {
        // 1. Structural Integrity Check (for Type-based converters)
        // We ensure that the type provided can be instantiated by the Activator (requires a public parameterless constructor).
        // Note: In a pure DI scenario, this might be too strict, but for a library ensuring stability, we enforce it.
        if (converterDef is TypeConverterDefinition typeDef)
        {
            if (typeDef.ConverterType.GetConstructor(Type.EmptyTypes) == null)
            {
                throw new FluentJsonValidationException(
                   $"Configuration error on '{entityType.Name}.{member.Name}': " +
                   $"The converter '{typeDef.ConverterType.Name}' must have a public parameterless constructor.");
            }
        }

        // 2. Type Safety Check (Polymorphic)
        // Instead of casting to specific implementations, we rely on the interface contract.
        // If the converter definition exposes a specific ModelType, we enforce type compatibility.
        if (converterDef.ModelType != null)
        {
            Type actualPropertyType = (member is PropertyInfo p) ? p.PropertyType : ((FieldInfo)member).FieldType;

            if (converterDef.ModelType != actualPropertyType)
            {
                throw new FluentJsonValidationException(
                    $"Type mismatch on '{entityType.Name}.{member.Name}': " +
                    $"The property is of type '{actualPropertyType.Name}', but the configured converter " +
                    $"expects '{converterDef.ModelType.Name}'.");
            }
        }
    }
    private static void ValidatePolymorphism(Type entityType, PolymorphismDefinition poly)
    {
        if (poly.SubTypes.Count == 0)
        {
            throw new FluentJsonValidationException(
                $"Polymorphism enabled for '{entityType.Name}' but no subtypes have been registered.");
        }

        // Structural Integrity: Discriminator property existence
        if (!poly.IsShadowProperty)
        {
            PropertyInfo? discriminatorProp = entityType.GetProperty(poly.DiscriminatorProperty,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (discriminatorProp is null)
            {
                throw new FluentJsonValidationException(
                    $"The discriminator property '{poly.DiscriminatorProperty}' defined for '{entityType.Name}' does not exist on the class. " +
                    "Use 'HasShadowDiscriminator' if this property only exists in JSON.");
            }
        }

        // Type Consistency check
        Type firstValueType = poly.SubTypes.Values.First().GetType();
        if (poly.SubTypes.Values.Any(v => v.GetType() != firstValueType))
        {
            throw new FluentJsonValidationException(
                $"Polymorphism error on '{entityType.Name}': Discriminator values must be of the same type (mixed string/int detected).");
        }

        // Hierarchy Validation
        foreach (Type subType in poly.SubTypes.Keys)
        {
            if (!entityType.IsAssignableFrom(subType))
            {
                throw new FluentJsonValidationException(
                    $"Type '{subType.Name}' is registered as a subtype of '{entityType.Name}' but does not inherit from it.");
            }
        }
    }
}
