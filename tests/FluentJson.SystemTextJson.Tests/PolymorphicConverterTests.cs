using System;
using System.Text.Json;
using FluentAssertions;
using FluentJson.Core.Builder;
using Xunit;

namespace FluentJson.SystemTextJson.Tests;

public class PolymorphicConverterTests
{
    private abstract class Shape { }
    private class Circle : Shape { public int Radius { get; set; } }
    private class Square : Shape { public int Side { get; set; } }
    
    [Fact]
    public void Factory_Should_ThrowArgumentNullException_WhenModelIsNull()
    {
        Action act = () => new FluentJsonStjPolymorphicConverterFactory(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("model");
    }

    [Fact]
    public void Factory_CanConvert_Should_ReturnFalse_ForUnregisteredType()
    {
        var model = new JsonModelBuilder().Build();
        var factory = new FluentJsonStjPolymorphicConverterFactory(model);
        factory.CanConvert(typeof(string)).Should().BeFalse();
    }

    [Fact]
    public void Converter_Should_WriteNullValue_WhenInstanceIsNull()
    {
        var builder = new JsonModelBuilder();
        builder.Entity<Shape>().HasDiscriminator("type").HasDerivedType<Circle>("circle");
        var model = builder.Build();

        var options = new JsonSerializerOptions();
        options.AddFluentJson(model);

        var json = JsonSerializer.Serialize<Shape>(null!, options);
        json.Should().Be("null");
    }

    [Fact]
    public void Converter_Should_ReadNullValue_WhenJsonIsNull()
    {
        var builder = new JsonModelBuilder();
        builder.Entity<Shape>().HasDiscriminator("type").HasDerivedType<Circle>("circle");
        var model = builder.Build();

        var options = new JsonSerializerOptions();
        options.AddFluentJson(model);

        var obj = JsonSerializer.Deserialize<Shape>("null", options);
        obj.Should().BeNull();
    }

    [Fact]
    public void Converter_Should_Serialize_DerivedTypeCorrectly()
    {
        var builder = new JsonModelBuilder();
        builder.Entity<Shape>().HasDiscriminator("type").HasDerivedType<Circle>("circle");
        var model = builder.Build();

        var options = new JsonSerializerOptions();
        options.AddFluentJson(model);

        Shape c = new Circle { Radius = 5 };
        var json = JsonSerializer.Serialize(c, options);
        json.Should().Contain("\"Radius\":5");
        // STJ serialization handles polymorphism natively based on runtime type.
    }
}
