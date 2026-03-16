using System;
using System.Collections.Generic;

using FluentAssertions;

using FluentJson.Newtonsoft;

using Newtonsoft.Json;

using Xunit;

namespace FluentJson.Newtonsoft.Tests;

public class PolymorphicConverterTests
{
    private class DummyConverter : FluentJsonPolymorphicConverter
    {
        public DummyConverter(string discriminatorProperty, IReadOnlyDictionary<object, Type> derivedTypes)
            : base(discriminatorProperty, derivedTypes)
        {
        }
    }

    [Fact]
    public void CanConvert_Should_ReturnTrue()
    {
        var converter = new DummyConverter("type", new Dictionary<object, Type>());
        converter.CanConvert(typeof(object)).Should().BeTrue();
    }

    [Fact]
    public void WriteJson_Should_WriteNull_WhenValueIsNull()
    {
        var converter = new DummyConverter("type", new Dictionary<object, Type>());

        var settings = new JsonSerializerSettings();
        settings.Converters.Add(converter);

        var json = JsonConvert.SerializeObject(null, typeof(object), settings);
        json.Should().Be("null");
    }

    [Fact]
    public void WriteJson_Should_Serialize_DerivedType()
    {
        var converter = new DummyConverter("type", new Dictionary<object, Type>());

        var settings = new JsonSerializerSettings();
        settings.Converters.Add(converter);

        var json = JsonConvert.SerializeObject(new { A = 1 }, typeof(object), settings);
        json.Should().Contain("\"A\":1");
    }
}
