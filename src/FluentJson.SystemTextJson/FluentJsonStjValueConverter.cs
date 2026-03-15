using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FluentJson.SystemTextJson;

/// <summary>
/// A generic System.Text.Json converter that flattens a complex Property into a scalar Value using user-defined delegates.
/// </summary>
/// <typeparam name="TProperty">The complex type (e.g., UserId).</typeparam>
/// <typeparam name="TUnwrapped">The flat type (e.g., Guid).</typeparam>
internal class FluentJsonStjValueConverter<TProperty, TUnwrapped> : JsonConverter<TProperty>
{
    private readonly Func<TProperty, TUnwrapped> _serializeFunc;
    private readonly Func<TUnwrapped, TProperty> _deserializeFunc;

    public FluentJsonStjValueConverter(Func<TProperty, TUnwrapped> serializeFunc, Func<TUnwrapped, TProperty> deserializeFunc)
    {
        _serializeFunc = serializeFunc ?? throw new ArgumentNullException(nameof(serializeFunc));
        _deserializeFunc = deserializeFunc ?? throw new ArgumentNullException(nameof(deserializeFunc));
    }

    public override TProperty? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Deserialize the flat value
        var flatValue = JsonSerializer.Deserialize<TUnwrapped>(ref reader, options);
        
        if (flatValue == null) return default;
        
        // Wrap it back into the complex type
        return _deserializeFunc(flatValue);
    }

    public override void Write(Utf8JsonWriter writer, TProperty value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        // Unwrap the value
        var flatValue = _serializeFunc(value);

        // Serialize the flat value
        JsonSerializer.Serialize(writer, flatValue, options);
    }
}
