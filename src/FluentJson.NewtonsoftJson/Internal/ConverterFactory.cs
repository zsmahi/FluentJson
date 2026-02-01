using FluentJson.Abstractions;
using FluentJson.NewtonsoftJson.Converters;
using Newtonsoft.Json;
using System;

namespace FluentJson.NewtonsoftJson.Internal;

/// <summary>
/// A static factory responsible for translating abstract converter definitions into concrete Newtonsoft.Json converters.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Factory / Adapter.
/// </para>
/// <para>
/// This class acts as the translation layer between the <see cref="IConverterDefinition"/> abstractions 
/// (stored in the Core project) and the specific implementation required by <c>Newtonsoft.Json</c>.
/// </para>
/// </remarks>
internal static class ConverterFactory
{
    /// <summary>
    /// Creates a compatible <see cref="JsonConverter"/> instance based on the provided definition.
    /// </summary>
    /// <param name="definition">The abstract converter definition.</param>
    /// <returns>A configured <see cref="JsonConverter"/> or <c>null</c> if the definition type is unknown.</returns>
    public static JsonConverter? Create(IConverterDefinition definition)
    {
        return definition switch
        {
            TypeConverterDefinition typeDef => CreateFromType(typeDef),
            LambdaConverterDefinition lambdaDef => CreateFromLambda(lambdaDef),
            _ => null
        };
    }

    /// <summary>
    /// Instantiates a standard class-based converter.
    /// </summary>
    private static JsonConverter? CreateFromType(TypeConverterDefinition def)
    {
        // Validation: Ensure the target type actually inherits from JsonConverter
        if (typeof(JsonConverter).IsAssignableFrom(def.ConverterType))
        {
            // Note: This uses the default parameterless constructor. 
            // Dependency Injection inside Converters is not currently supported in this factory method.
            return (JsonConverter)Activator.CreateInstance(def.ConverterType);
        }
        return null;
    }

    /// <summary>
    /// Constructs a generic <see cref="LambdaJsonConverter{TModel, TJson}"/> at runtime using reflection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Architectural Note:</strong>
    /// Since <see cref="LambdaConverterDefinition"/> stores type metadata as raw <see cref="Type"/> objects (Type Erasure), 
    /// we must use <see cref="Type.MakeGenericType"/> to re-construct the strongly-typed generic converter 
    /// required to execute the delegates safely.
    /// </para>
    /// </remarks>
    private static JsonConverter CreateFromLambda(LambdaConverterDefinition def)
    {
        Type modelType = def.ModelType;
        Type jsonType = def.JsonType;

        // Dynamically create LambdaJsonConverter<TModel, TJson>
        Type converterType = typeof(LambdaJsonConverter<,>).MakeGenericType(modelType, jsonType);

        return (JsonConverter)Activator.CreateInstance(
            converterType,
            def.ConvertToDelegate,
            def.ConvertFromDelegate
        );
    }
}
