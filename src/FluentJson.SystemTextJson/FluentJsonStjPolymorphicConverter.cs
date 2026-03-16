using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FluentJson.SystemTextJson;

internal class FluentJsonStjPolymorphicConverter<T> : JsonConverter<T>
{
    private readonly string _discriminatorProperty;
    private readonly IReadOnlyDictionary<object, Type> _derivedTypes;

    private readonly byte[] _discriminatorPropertyUtf8;
    private readonly KeyValuePair<byte[], Type>[] _derivedTypesUtf8;

    public FluentJsonStjPolymorphicConverter(string discriminatorProperty, IReadOnlyDictionary<object, Type> derivedTypes)
    {
        _discriminatorProperty = discriminatorProperty;
        _discriminatorPropertyUtf8 = System.Text.Encoding.UTF8.GetBytes(discriminatorProperty);
        _derivedTypes = derivedTypes;

        var derivedList = new List<KeyValuePair<byte[], Type>>(derivedTypes.Count);
        foreach (var kvp in derivedTypes)
        {
            derivedList.Add(new KeyValuePair<byte[], Type>(System.Text.Encoding.UTF8.GetBytes(kvp.Key.ToString()!), kvp.Value));
        }
        _derivedTypesUtf8 = derivedList.ToArray();
    }

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return default;

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected StartObject token, got {reader.TokenType}");
        }

        Utf8JsonReader readerClone = reader;
        Type? targetType = null;
        string? discriminatorValueForError = null;

        while (readerClone.Read())
        {
            if (readerClone.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (readerClone.TokenType == JsonTokenType.PropertyName)
            {
                if (readerClone.ValueTextEquals(_discriminatorPropertyUtf8) ||
                    string.Equals(readerClone.GetString(), _discriminatorProperty, StringComparison.OrdinalIgnoreCase))
                {
                    readerClone.Read();

                    // Zero-allocation loop check for derived types
                    for (int i = 0; i < _derivedTypesUtf8.Length; i++)
                    {
                        if (readerClone.ValueTextEquals(_derivedTypesUtf8[i].Key))
                        {
                            targetType = _derivedTypesUtf8[i].Value;
                            break;
                        }
                    }

                    if (targetType == null)
                    {
                        // Fallback constraint logic for errors or case-insensitive string matching
                        discriminatorValueForError = readerClone.GetString() ?? string.Empty;
                        foreach (var kvp in _derivedTypes)
                        {
                            if (string.Equals(kvp.Key.ToString(), discriminatorValueForError, StringComparison.OrdinalIgnoreCase))
                            {
                                targetType = kvp.Value;
                                break;
                            }
                        }
                    }
                    break;
                }
                else
                {
                    readerClone.Skip();
                }
            }
        }

        if (targetType == null)
        {
            if (discriminatorValueForError != null)
                throw new JsonException($"Type '{discriminatorValueForError}' is not a registered derived type");
            else
                throw new JsonException($"The polymorphic discriminator property '{_discriminatorProperty}' is missing from the JSON payload.");
        }

        // Deserialize as the target type using native STJ deserialization (bypassing this converter)
        // By passing 'ref reader', STJ will consume the tokens directly without re-parsing text.
        return (T?)JsonSerializer.Deserialize(ref reader, targetType, options);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        // For serialization, STJ's default polymorphism is usually fine, or we can manually serialize using the concrete type
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
