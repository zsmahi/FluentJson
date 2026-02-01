using FluentJson.Definitions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace FluentJson.NewtonsoftJson.Converters;

internal class PolymorphicJsonConverter : JsonConverter
{
    private readonly Type _baseType;
    private readonly PolymorphismDefinition _definition;

    public PolymorphicJsonConverter(Type baseType, PolymorphismDefinition definition)
    {
        _baseType = baseType;
        _definition = definition;
    }

    public override bool CanConvert(Type objectType)
    {
        return _baseType.IsAssignableFrom(objectType);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        var item = JObject.Load(reader);
        JToken? discriminatorToken = item[_definition.DiscriminatorProperty];

        if (discriminatorToken == null)
        {
            throw new JsonSerializationException(
                $"Polymorphism failure: Missing required discriminator property '{_definition.DiscriminatorProperty}' in JSON payload.");
        }

        string? discriminatorValue = discriminatorToken.ToString();

        Type? targetType = _definition.SubTypes
            .FirstOrDefault(x => Convert.ToString(x.Value, System.Globalization.CultureInfo.InvariantCulture) == discriminatorValue)
            .Key;

        if (targetType == null)
        {
            throw new JsonSerializationException(
                $"Polymorphism failure: Unknown discriminator value '{discriminatorValue}'.");
        }

        object target = Activator.CreateInstance(targetType)!;
        serializer.Populate(item.CreateReader(), target);

        return target;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        Type type = value.GetType();

        if (!_definition.SubTypes.TryGetValue(type, out object? discriminatorValue))
        {
            throw new JsonSerializationException(
                $"Polymorphism failure: Type '{type.Name}' is not registered as a subtype of the configured base class '{_baseType.Name}'.");
        }

        var internalSerializer = new JsonSerializer
        {
            ContractResolver = serializer.ContractResolver,
            NullValueHandling = serializer.NullValueHandling,
            ReferenceLoopHandling = serializer.ReferenceLoopHandling,
            DateFormatHandling = serializer.DateFormatHandling
        };

        foreach (JsonConverter? converter in serializer.Converters.Where(c => c != this))
        {
            internalSerializer.Converters.Add(converter);
        }

        var jo = JObject.FromObject(value, internalSerializer);

        // This prevents the "Property with the same name already exists" error.
        if (jo.Property(_definition.DiscriminatorProperty) != null)
        {
            jo.Remove(_definition.DiscriminatorProperty);
        }

        // Inject as Metadata (First Property)
        jo.AddFirst(new JProperty(_definition.DiscriminatorProperty, discriminatorValue));

        jo.WriteTo(writer);
    }
}
