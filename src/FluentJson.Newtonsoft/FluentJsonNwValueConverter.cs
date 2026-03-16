using System;

using Newtonsoft.Json;

namespace FluentJson.Newtonsoft;

/// <summary>
/// A generic Newtonsoft.Json converter that flattens a complex Property into a scalar Value using user-defined delegates.
/// </summary>
/// <typeparam name="TProperty">The complex type (e.g., UserId).</typeparam>
/// <typeparam name="TUnwrapped">The flat type (e.g., Guid).</typeparam>
internal class FluentJsonNwValueConverter<TProperty, TUnwrapped> : JsonConverter<TProperty>
{
    private readonly Func<TProperty, TUnwrapped> _serializeFunc;
    private readonly Func<TUnwrapped, TProperty> _deserializeFunc;

    public FluentJsonNwValueConverter(Func<TProperty, TUnwrapped> serializeFunc, Func<TUnwrapped, TProperty> deserializeFunc)
    {
        _serializeFunc = serializeFunc ?? throw new ArgumentNullException(nameof(serializeFunc));
        _deserializeFunc = deserializeFunc ?? throw new ArgumentNullException(nameof(deserializeFunc));
    }

    public override TProperty? ReadJson(JsonReader reader, Type objectType, TProperty? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return default;

        // Deserialize the flat value
        var flatValue = serializer.Deserialize<TUnwrapped>(reader);

        if (flatValue == null) return default;

        // Wrap it back into the complex type
        return _deserializeFunc(flatValue);
    }

    public override void WriteJson(JsonWriter writer, TProperty? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        // Unwrap the value
        var flatValue = _serializeFunc(value);

        // Serialize the flat value
        serializer.Serialize(writer, flatValue);
    }
}
