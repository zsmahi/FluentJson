using System;
using FluentAssertions;
using FluentJson.Core.Builder;
using FluentJson.Newtonsoft;
using Newtonsoft.Json;
using Xunit;

namespace FluentJson.Newtonsoft.Tests;

public class ValueConverterTests
{
    private struct UserId
    {
        public Guid Value { get; }
        public UserId(Guid value) => Value = value;
    }

    private class User
    {
        public UserId Id { get; set; }
    }

    [Fact]
    public void Converter_Should_WriteNull_WhenValueIsNull()
    {
        var converter = new FluentJsonNwValueConverter<string, string>(s => s, s => s);
        
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(converter);
        
        var json = JsonConvert.SerializeObject(null, typeof(string), settings);
        json.Should().Be("null");
        
        // Invoke ReadJson correctly via the engine
        var obj = JsonConvert.DeserializeObject<string>("null", settings);
        obj.Should().BeNull();
    }

    [Fact]
    public void Converter_CanConvert_Should_ReturnTrue()
    {
        var converter = new FluentJsonNwValueConverter<string, string>(s => s, s => s);
        converter.CanConvert(typeof(string)).Should().BeTrue();
    }

    [Fact]
    public void Converter_Should_ThrowArgumentNullException_WhenDelegatesAreNull()
    {
        Action act1 = () => new FluentJsonNwValueConverter<UserId, string>(null!, s => new UserId(Guid.Parse(s)));
        act1.Should().Throw<ArgumentNullException>().WithParameterName("serializeFunc");

        Action act2 = () => new FluentJsonNwValueConverter<UserId, string>(u => u.Value.ToString(), null!);
        act2.Should().Throw<ArgumentNullException>().WithParameterName("deserializeFunc");
    }
}
