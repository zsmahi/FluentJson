using System.Text.Json;
using System.Text.Json.Serialization;

namespace FluentJson.SystemTextJson.Converters;

/// <summary>
/// A generic System.Text.Json converter that delegates transformation logic to external functions.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Adapter / Strategy.
/// </para>
/// <para>
/// This converter enables "Inline Conversion" for System.Text.Json. It acts as a bridge between the domain model 
/// <typeparamref name="TModel"/> and a JSON-friendly surrogate <typeparamref name="TJson"/> (Data Transfer Object), 
/// executing the user-defined lambda expressions during the serialization pipeline.
/// </para>
/// </remarks>
/// <typeparam name="TModel">The type of the domain entity.</typeparam>
/// <typeparam name="TJson">The intermediate type used for the JSON representation (e.g., string, int, DTO).</typeparam>
internal class LambdaJsonConverter<TModel, TJson>(Func<TModel, TJson> convertTo, Func<TJson, TModel> convertFrom) : JsonConverter<TModel>
{
    private readonly Func<TModel, TJson> _convertTo = convertTo ?? throw new ArgumentNullException(nameof(convertTo));
    private readonly Func<TJson, TModel> _convertFrom = convertFrom ?? throw new ArgumentNullException(nameof(convertFrom));

    /// <summary>
    /// Reads and converts the JSON to type <typeparamref name="TModel"/>.
    /// </summary>
    /// <param name="reader">The <see cref="Utf8JsonReader"/> to read from.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="JsonException">Thrown if the JSON is invalid or the user lambda fails.</exception>
    public override TModel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Null Handling: Align with standard behavior
        if (reader.TokenType == JsonTokenType.Null)
        {
            return default;
        }

        // Phase 1: Deserialize to Intermediate Type (TJson)
        // We let STJ handle the low-level parsing of the surrogate type.
        TJson? intermediate;
        try
        {
            intermediate = JsonSerializer.Deserialize<TJson>(ref reader, options);
        }
        catch (Exception ex)
        {
            throw new JsonException($"Error reading intermediate JSON value of type '{typeof(TJson).Name}'.", ex);
        }

        // Safety Check: Prevent assigning null to a non-nullable value type
        if (intermediate is null)
        {
            if (typeof(TModel).IsValueType && Nullable.GetUnderlyingType(typeof(TModel)) == null)
            {
                throw new JsonException($"Cannot convert null intermediate value to non-nullable model type '{typeof(TModel).Name}'.");
            }
            return default;
        }

        // Phase 2: Execute User Logic (TJson -> TModel)
        try
        {
            return _convertFrom(intermediate);
        }
        catch (Exception ex)
        {
            // Exception Wrapping: 
            // We wrap user exceptions in JsonException to preserve the serialization context.
            throw new JsonException(
                $"Error converting from JSON surrogate ('{typeof(TJson).Name}') to Model ('{typeof(TModel).Name}'). " +
                $"User-defined lambda threw an exception: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Writes a specified value as JSON.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The value to convert to JSON.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, TModel value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        // Phase 1: Execute User Logic (TModel -> TJson)
        TJson? intermediate;
        try
        {
            intermediate = _convertTo(value);
        }
        catch (Exception ex)
        {
            throw new JsonException(
                $"Error converting from Model ('{typeof(TModel).Name}') to JSON surrogate ('{typeof(TJson).Name}'). " +
                $"User-defined lambda threw an exception: {ex.Message}", ex);
        }

        // Phase 2: Serialization of the Surrogate
        if (intermediate is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            JsonSerializer.Serialize(writer, intermediate, options);
        }
    }
}
