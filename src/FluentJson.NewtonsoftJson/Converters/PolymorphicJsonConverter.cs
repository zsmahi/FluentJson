using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace FluentJson.NewtonsoftJson.Converters;

/// <summary>
/// A specialized Newtonsoft.Json converter capable of deserializing abstract types into concrete implementations 
/// based on a discriminator property in the JSON payload.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Factory / Type Discriminator.
/// </para>
/// <para>
/// This converter intercepts the deserialization process for a base type. It reads a specific property 
/// (the "discriminator") to decide which derived class to instantiate, effectively solving the 
/// "Polymorphic Deserialization" problem that standard JSON parsers cannot handle automatically.
/// </para>
/// </remarks>
internal class PolymorphicJsonConverter : JsonConverter
{
    private readonly Type _baseType;
    private readonly string _discriminatorProp;
    private readonly Dictionary<string, Type> _valueToTypeMap;

    /// <summary>
    /// Initializes a new instance of the polymorphic converter.
    /// </summary>
    /// <param name="baseType">The abstract base class or interface that this converter is responsible for.</param>
    /// <param name="discriminatorProp">The name of the JSON property that acts as the type identifier (e.g., "$type").</param>
    /// <param name="subTypes">
    /// A dictionary mapping the expected discriminator values (e.g., "circle") to their corresponding CLR types (e.g., typeof(Circle)).
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="baseType"/> is null.</exception>
    public PolymorphicJsonConverter(Type baseType, string discriminatorProp, IReadOnlyDictionary<Type, object> subTypes)
    {
        _baseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
        _discriminatorProp = discriminatorProp;

        // Optimization: Invert the dictionary for O(1) lookups during deserialization.
        // Input:  [Type -> "value"]
        // Stored: ["value" -> Type]
        _valueToTypeMap = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<Type, object> kvp in subTypes)
        {
            if (kvp.Value != null)
            {
                string key = Convert.ToString(kvp.Value, System.Globalization.CultureInfo.InvariantCulture)!;

                if (!_valueToTypeMap.ContainsKey(key))
                {
                    _valueToTypeMap[key] = kvp.Key;
                }
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether this converter can write JSON.
    /// </summary>
    /// <value>
    /// <c>false</c>, because serialization of concrete types is handled naturally by the default serializer 
    /// (the object instance already knows its own type).
    /// </value>
    public override bool CanWrite => false;

    /// <summary>
    /// Determines whether this instance can convert the specified object type.
    /// </summary>
    /// <param name="objectType">The type of the object.</param>
    /// <returns><c>true</c> if the type equals base type; otherwise, <c>false</c>.</returns>
    public override bool CanConvert(Type objectType)
        => objectType == _baseType;

    /// <summary>
    /// Reads the JSON representation of the object.
    /// </summary>
    /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
    /// <param name="objectType">Type of the object.</param>
    /// <param name="existingValue">The existing value of object being read.</param>
    /// <param name="serializer">The calling serializer.</param>
    /// <returns>The deserialized object of the concrete derived type.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Performance Note:</strong>
    /// This method loads the entire JSON object into a <see cref="JObject"/> in memory. This buffering is necessary 
    /// because the discriminator property might appear anywhere in the JSON object (not necessarily at the start).
    /// </para>
    /// </remarks>
    /// <exception cref="JsonSerializationException">
    /// Thrown if the discriminator property is missing, null, or corresponds to an unknown type.
    /// </exception>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;

        if (reader.TokenType != JsonToken.StartObject)
        {
            throw new JsonSerializationException(
                $"Polymorphic deserialization failed. Expected token 'StartObject' but got '{reader.TokenType}'. " +
                $"Ensure the JSON structure matches the entity definition.");
        }

        // Buffer the JSON to allow random access to properties
        JObject item;
        try
        {
            item = JObject.Load(reader);
        }
        catch (Exception ex)
        {
            throw new JsonSerializationException("Error buffering JSON for polymorphic deserialization.", ex);
        }

        // Locate the discriminator property
        JToken? discriminatorToken = item.GetValue(_discriminatorProp, StringComparison.OrdinalIgnoreCase);

        if (discriminatorToken == null)
        {
            throw new JsonSerializationException(
                $"Unable to deserialize into type '{objectType.Name}': The discriminator property '{_discriminatorProp}' is missing in the JSON payload.");
        }

        string? discriminatorString = discriminatorToken.ToString();

        if (discriminatorString != null && _valueToTypeMap.TryGetValue(discriminatorString, out Type? targetType))
        {
            using JsonReader subReader = item.CreateReader();
            return serializer.Deserialize(subReader, targetType);
        }

        throw new JsonSerializationException(
            $"Unknown discriminator value '{discriminatorString}' for base type '{_baseType.Name}'. " +
            $"Expected one of: {string.Join(", ", _valueToTypeMap.Keys)}"
            );

    }

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        => throw new NotSupportedException("This converter only supports deserialization.");
}
