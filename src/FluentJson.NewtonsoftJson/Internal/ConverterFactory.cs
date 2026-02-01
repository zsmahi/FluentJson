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
    /// <param name="serviceProvider">The DI provider used to resolve converter dependencies (optional).</param>
    /// <returns>A configured <see cref="JsonConverter"/> or <c>null</c> if the definition type is unknown.</returns>
    public static JsonConverter? Create(IConverterDefinition definition, IServiceProvider? serviceProvider)
    {
        return definition switch
        {
            TypeConverterDefinition typeDef => CreateFromType(typeDef, serviceProvider),
            LambdaConverterDefinition lambdaDef => CreateFromLambda(lambdaDef),
            _ => null
        };
    }

    /// <summary>
    /// Instantiates a standard class-based converter, attempting resolution via DI first.
    /// </summary>
    private static JsonConverter? CreateFromType(TypeConverterDefinition def, IServiceProvider? serviceProvider)
    {
        // Validation: Ensure the target type actually inherits from JsonConverter
        if (typeof(JsonConverter).IsAssignableFrom(def.ConverterType))
        {
            // 1. Dependency Injection Strategy
            // If a service provider is available, we attempt to resolve the converter from the container.
            // This enables constructor injection for complex converters.
            if (serviceProvider != null)
            {
                try
                {
                    object? service = serviceProvider.GetService(def.ConverterType);
                    if (service is JsonConverter converter)
                    {
                        return converter;
                    }
                }
                catch
                {
                    // Architectural Decision:
                    // If DI resolution fails (e.g. service not registered, scoping issues), 
                    // we silently swallow the error and fallback to the Activator strategy.
                    // This ensures robustness for converters that don't strictly require DI.
                }
            }

            // 2. Default Activator Strategy (Fallback)
            // If DI is unavailable or failed, we instantiate the type directly.
            // Requirement: The converter must have a public parameterless constructor in this scenario.
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
