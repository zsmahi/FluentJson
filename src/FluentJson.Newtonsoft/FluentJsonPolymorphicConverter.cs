using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FluentJson.Newtonsoft;

/// <summary>
/// A custom converter for Newtonsoft.Json to handle polymorphic deserialization based on FluentJson metadata rules.
/// </summary>
internal class FluentJsonPolymorphicConverter : JsonConverter
{
    private readonly string _discriminatorProperty;
    private readonly IReadOnlyDictionary<object, Type> _derivedTypes;

    public FluentJsonPolymorphicConverter(string discriminatorProperty, IReadOnlyDictionary<object, Type> derivedTypes)
    {
        _discriminatorProperty = discriminatorProperty ?? throw new ArgumentNullException(nameof(discriminatorProperty));
        _derivedTypes = derivedTypes ?? throw new ArgumentNullException(nameof(derivedTypes));
    }

    public override bool CanConvert(Type objectType)
    {
        // This converter is manually assigned to specific contracts by the resolver.
        return true;
    }

    public override bool CanWrite => false;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotSupportedException("FluentJsonPolymorphicConverter is only used for deserialization.");
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;

        if (reader.TokenType != JsonToken.StartObject)
        {
            throw new JsonSerializationException($"Expected StartObject token, got {reader.TokenType}");
        }

        // Load the JSON into a JObject
        var jObject = JObject.Load(reader);

        // Find the discriminator property (ignoring case for robustness, standard behavior)
        var discriminatorToken = jObject.GetValue(_discriminatorProperty, StringComparison.OrdinalIgnoreCase);

        if (discriminatorToken == null)
        {
            throw new JsonSerializationException($"The polymorphic discriminator property '{_discriminatorProperty}' is missing from the JSON payload for type {objectType.Name}.");
        }

        // We use string representation for key lookup to support both string and numeric discriminators consistently
        string discriminatorValue = discriminatorToken.Value<string>() ?? string.Empty;

        Type? targetType = null;
        
        // Find the matching derived type based on the key
        foreach (var kvp in _derivedTypes)
        {
            if (string.Equals(kvp.Key.ToString(), discriminatorValue, StringComparison.OrdinalIgnoreCase))
            {
                targetType = kvp.Value;
                break;
            }
        }

        if (targetType == null)
        {
            throw new JsonSerializationException($"Type '{discriminatorValue}' is not a registered derived type");
        }

        // Create the concrete instance and populate it
        // We use serializer.Deserialize to ensure our custom ContractResolver is still used for the derived type
        using var jsonReader = jObject.CreateReader();
        return serializer.Deserialize(jsonReader, targetType);
    }
}
