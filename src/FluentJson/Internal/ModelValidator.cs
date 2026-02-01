using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentJson.Abstractions;
using FluentJson.Definitions;

namespace FluentJson.Internal;

/// <summary>
/// A static validator responsible for enforcing consistency and correctness rules on the configuration model.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Fail-Fast.
/// </para>
/// <para>
/// This class executes a series of integrity checks during the <c>Build</c> phase. By validating the model 
/// exhaustively at startup, it prevents subtle logic errors (like duplicate discriminator values or 
/// missing properties) from causing runtime exceptions later during actual JSON processing.
/// </para>
/// </remarks>
internal static class ModelValidator
{
    /// <summary>
    /// Validates the consistency of the entity configuration.
    /// Implements the "Fail-Fast" principle: we detect configuration errors during the Build phase
    /// rather than letting them cause obscure exceptions during actual JSON processing.
    /// </summary>
    /// <param name="entityType">The root type of the entity being configured.</param>
    /// <param name="definition">The raw configuration definition to validate.</param>
    public static void ValidateDefinition(Type entityType, JsonEntityDefinition definition)
    {
        // 1. Polymorphism Integrity
        // Ensures that the discriminator logic (if present) is structurally sound 
        // (e.g., unique values, valid subtypes).
        if (definition.Polymorphism != null)
        {
            ValidatePolymorphism(entityType, definition.Polymorphism);
        }

        // 2. Property & Field Mapping Validation
        // Iterates over all configured members to ensure mapped fields exist 
        // and converters are type-compatible.
        foreach (KeyValuePair<MemberInfo, JsonPropertyDefinition> propKvp in definition.Properties)
        {
            ValidateProperty(entityType, propKvp.Key, propKvp.Value);
        }
    }

    /// <summary>
    /// Validates a specific property configuration, focusing on backing field accessibility 
    /// and converter type compatibility.
    /// </summary>
    private static void ValidateProperty(Type entityType, MemberInfo member, JsonPropertyDefinition def)
    {
        // A. Backing Field Structural Validation
        // This is critical for DDD scenarios. If the user maps a property to a private field 
        // via 'HasField', we must guarantee that the field strictly belongs to the entity 
        // (or its hierarchy). Failure to check this now would result in a compiled Expression Tree 
        // throwing a runtime exception when accessed.
        if (def.BackingField != null)
        {
            Type? fieldDeclaringType = def.BackingField.DeclaringType;

            // Constraint: The field must be declared on the entity type or one of its base classes.
            bool isFieldAccessible = fieldDeclaringType != null &&
                                     fieldDeclaringType.IsAssignableFrom(entityType);

            if (!isFieldAccessible)
            {
                throw new InvalidOperationException(
                    $"Invalid configuration on '{entityType.Name}.{member.Name}': " +
                    $"The mapped backing field '{def.BackingField.Name}' belongs to '{fieldDeclaringType?.Name}', " +
                    $"which is not compatible with the configured entity '{entityType.Name}'. " +
                    "Ensure the field is defined within the class hierarchy.");
            }
        }

        // B. Converter Compatibility
        // Since converters are often instantiated via Reflection or Activator, we lose compile-time 
        // type safety. We must enforce these invariants manually here.
        if (def.ConverterDefinition != null)
        {
            // Case 1: Class-based Converters (TypeConverterDefinition)
            // Requirement: Must have a public parameterless constructor for instantiation.
            if (def.ConverterDefinition is TypeConverterDefinition typeDef)
            {
                if (typeDef.ConverterType.GetConstructor(Type.EmptyTypes) == null)
                {
                    throw new InvalidOperationException(
                        $"Configuration error on '{entityType.Name}.{member.Name}': " +
                        $"The converter '{typeDef.ConverterType.Name}' must have a public parameterless constructor " +
                        "to be used with the 'HasConversion<T>' method.");
                }
            }

            // Case 2: Lambda-based Converters (LambdaConverterDefinition)
            // Requirement: The input/output types of the lambda must match the property type.
            if (def.ConverterDefinition is LambdaConverterDefinition lambdaDef)
            {
                Type actualPropertyType = (member is PropertyInfo p)
                    ? p.PropertyType
                    : ((FieldInfo)member).FieldType;

                // Invariant Check: TModel in Converter<TModel, TJson> must match property type.
                if (lambdaDef.ModelType != actualPropertyType)
                {
                    throw new InvalidOperationException(
                        $"Type mismatch on '{entityType.Name}.{member.Name}': " +
                        $"The property is of type '{actualPropertyType.Name}', but the configured converter " +
                        $"expects '{lambdaDef.ModelType.Name}'. Check your 'HasConversion' lambda signatures.");
                }
            }
        }
    }

    private static void ValidatePolymorphism(Type entityType, PolymorphismDefinition poly)
    {
        // 1. Completeness Check: Polymorphism enabled but no subtypes mapped.
        if (poly.SubTypes.Count == 0)
        {
            throw new InvalidOperationException(
                $"Polymorphism is enabled for '{entityType.Name}' via property '{poly.DiscriminatorProperty}', but no subtypes have been registered.");
        }

        // 2. Structural Integrity: The discriminator property must actually exist on the base class.
        // We only check for the property existence if it is NOT a shadow property.
        if (!poly.IsShadowProperty)
        {
            // We check NonPublic flags to support encapsulated discriminator properties.
            PropertyInfo? discriminatorProp = entityType.GetProperty(poly.DiscriminatorProperty,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (discriminatorProp is null)
            {
                throw new InvalidOperationException(
                    $"The discriminator property '{poly.DiscriminatorProperty}' defined for '{entityType.Name}' does not exist on the class. " +
                    $"Please check the spelling or use the strongly-typed 'HasDiscriminator(x => x.Prop)' method.");
            }
        }

        // 3. Type Consistency: All discriminator values must be of the same primitive type (e.g., all int or all string).
        Type firstValueType = poly.SubTypes.Values.First().GetType();
        if (poly.SubTypes.Values.Any(v => v.GetType() != firstValueType))
        {
            throw new InvalidOperationException(
                $"Polymorphism configuration error on '{entityType.Name}': Discriminator values are heterogeneous. " +
                $"All subtypes must use the same value type (e.g., all strings or all integers). " +
                $"Expected type: {firstValueType.Name}.");
        }

        // 4. Uniqueness Constraint: No two types can share the same discriminator value.
        var duplicateValues = poly.SubTypes
            .GroupBy(x => x.Value)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateValues.Any())
        {
            string valuesStr = string.Join(", ", duplicateValues);
            throw new InvalidOperationException(
                $"Configuration error on '{entityType.Name}': Duplicate discriminator values detected: {valuesStr}.");
        }

        // 5. Hierarchy Validation: Registered subtypes must legitimately inherit from the base type.
        foreach (Type subType in poly.SubTypes.Keys)
        {
            if (!entityType.IsAssignableFrom(subType))
            {
                throw new InvalidOperationException(
                    $"Type '{subType.Name}' is registered as a subtype of '{entityType.Name}' but does not inherit from it.");
            }
        }
    }
}
