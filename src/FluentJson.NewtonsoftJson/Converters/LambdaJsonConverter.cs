using Newtonsoft.Json;
using System;

namespace FluentJson.NewtonsoftJson.Converters;

/// <summary>
/// A generic Newtonsoft.Json converter that delegates transformation logic to external functions.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Adapter / Strategy.
/// </para>
/// <para>
/// This converter bridges the gap between the domain model <typeparamref name="TModel"/> and a JSON-friendly surrogate 
/// <typeparamref name="TJson"/> (Data Transfer Object). It executes the user-defined lambda expressions 
/// configured via the fluent API during the serialization/deserialization pipeline.
/// </para>
/// </remarks>
/// <typeparam name="TModel">The type of the domain entity.</typeparam>
/// <typeparam name="TJson">The intermediate type used for the JSON representation (e.g., string, int, DTO).</typeparam>
/// <param name="convertToProvider">Function to convert the domain model to the JSON surrogate.</param>
/// <param name="convertFromProvider">Function to convert the JSON surrogate back to the domain model.</param>
internal class LambdaJsonConverter<TModel, TJson>(Func<TModel, TJson> convertToProvider, Func<TJson, TModel> convertFromProvider) : JsonConverter<TModel>
{
    private readonly Func<TJson, TModel> _convertFromProvider = convertFromProvider ?? throw new ArgumentNullException(nameof(convertFromProvider));
    private readonly Func<TModel, TJson> _convertToProvider = convertToProvider ?? throw new ArgumentNullException(nameof(convertToProvider));

    /// <summary>
    /// Reads the JSON representation of the object.
    /// </summary>
    /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
    /// <param name="objectType">Type of the object.</param>
    /// <param name="existingValue">The existing value of object being read.</param>
    /// <param name="hasExistingValue">The existing value has a value.</param>
    /// <param name="serializer">The calling serializer.</param>
    /// <returns>The deserialized object value.</returns>
    /// <exception cref="JsonSerializationException">Thrown if the JSON is invalid or if the user-defined lambda fails.</exception>
    public override TModel? ReadJson(JsonReader reader, Type objectType, TModel? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        // Null Handling:
        // If the JSON token is null, we return the default value for TModel immediately.
        if (reader.TokenType == JsonToken.Null)
        {
            return default;
        }

        // Phase 1: Deserialize to Intermediate Type (TJson)
        // We rely on Newtonsoft to handle the parsing complexity (dates, numbers, strings).
        TJson? jsonValue;
        try
        {
            jsonValue = serializer.Deserialize<TJson>(reader);
        }
        catch (Exception ex)
        {
            throw new JsonSerializationException($"Error reading intermediate JSON value of type '{typeof(TJson).Name}'.", ex);
        }

        // Safety Check for Non-Nullable Value Types
        if (jsonValue is null)
        {
            if (typeof(TModel).IsValueType && Nullable.GetUnderlyingType(typeof(TModel)) == null)
            {
                throw new JsonSerializationException($"Cannot convert null intermediate value to non-nullable model type '{typeof(TModel).Name}'.");
            }
            return default;
        }

        // Phase 2: Execute User Logic (TJson -> TModel)
        try
        {
            TModel result = _convertFromProvider(jsonValue);

            // Validation: Ensure the user function didn't return null for a non-nullable value type.
            if (result == null && typeof(TModel).IsValueType && Nullable.GetUnderlyingType(typeof(TModel)) == null)
            {
                throw new JsonSerializationException(
                    $"The configured lambda converter returned null for non-nullable type '{typeof(TModel).Name}'. " +
                    "Ensure your conversion logic handles all inputs correctly.");
            }

            return result;
        }
        catch (Exception ex) when (ex is not JsonSerializationException)
        {
            // Exception Wrapping:
            // User-defined lambdas can fail. We wrap the original exception to provide clear context.
            throw new JsonSerializationException(
                $"Error converting from JSON surrogate ('{typeof(TJson).Name}') to Model ('{typeof(TModel).Name}'). " +
                $"User-defined lambda threw an exception: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Writes the JSON representation of the object.
    /// </summary>
    /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="serializer">The calling serializer.</param>
    /// <exception cref="JsonSerializationException">Thrown if the user-defined lambda fails.</exception>
    public override void WriteJson(JsonWriter writer, TModel? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        // Phase 1: Execute User Logic (TModel -> TJson)
        TJson? jsonValue;
        try
        {
            jsonValue = _convertToProvider(value);
        }
        catch (Exception ex)
        {
            throw new JsonSerializationException(
                $"Error converting from Model ('{typeof(TModel).Name}') to JSON surrogate ('{typeof(TJson).Name}'). " +
                $"User-defined lambda threw an exception: {ex.Message}", ex);
        }

        // Phase 2: Serialize Intermediate Type
        if (jsonValue is null)
        {
            writer.WriteNull();
        }
        else
        {
            serializer.Serialize(writer, jsonValue);
        }
    }
}
