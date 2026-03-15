using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentJson.Core.Metadata;

namespace FluentJson.SystemTextJson;

internal class FluentJsonStjPolymorphicConverterFactory : JsonConverterFactory
{
    private readonly IJsonModel _model;
    private readonly ConcurrentDictionary<Type, bool> _canConvertCache = new();
    private readonly ConcurrentDictionary<Type, JsonConverter?> _converterCache = new();

    public FluentJsonStjPolymorphicConverterFactory(IJsonModel model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return _canConvertCache.GetOrAdd(typeToConvert, t => 
            _model.Entities.Any(e => e.EntityType == t && e.DiscriminatorPropertyName != null));
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (_converterCache.TryGetValue(typeToConvert, out var cachedConverter))
        {
            return cachedConverter;
        }

        var entity = _model.Entities.First(e => e.EntityType == typeToConvert && e.DiscriminatorPropertyName != null);
        
        var converterType = typeof(FluentJsonStjPolymorphicConverter<>).MakeGenericType(typeToConvert);
        var converter = (JsonConverter)Activator.CreateInstance(converterType, entity.DiscriminatorPropertyName, entity.DerivedTypes)!;
        _converterCache.TryAdd(typeToConvert, converter);
        return converter;
    }
}
