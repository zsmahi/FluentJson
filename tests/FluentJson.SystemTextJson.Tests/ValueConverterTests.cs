using System;
using System.Text.Json;

using FluentAssertions;

using FluentJson.Core.Builder;

using Xunit;

namespace FluentJson.SystemTextJson.Tests;

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
        var converter = new FluentJsonStjValueConverter<string, string>(s => s, s => s);

        var options = new JsonSerializerOptions();
        var json = JsonSerializer.Serialize<string>(null!, options);
        json.Should().Be("null");
    }

    [Fact]
    public void Converter_Should_ThrowArgumentNullException_WhenDelegatesAreNull()
    {
        Action act1 = () => new FluentJsonStjValueConverter<UserId, string>(null!, s => new UserId(Guid.Parse(s)));
        act1.Should().Throw<ArgumentNullException>().WithParameterName("serializeFunc");

        Action act2 = () => new FluentJsonStjValueConverter<UserId, string>(u => u.Value.ToString(), null!);
        act2.Should().Throw<ArgumentNullException>().WithParameterName("deserializeFunc");
    }
}
