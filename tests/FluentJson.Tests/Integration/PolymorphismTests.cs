using FluentAssertions;
using FluentJson.Abstractions;
using FluentJson.Builders;
using System.Text.Json;
using NewtonsoftSerial = Newtonsoft.Json;

namespace FluentJson.Tests.Integration;

public class PolymorphismTests
{
    // --- Domain Hierarchy ---
    public abstract class Shape
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
    }

    public class Circle : Shape
    {
        public double Radius { get; set; }
    }

    public class Rectangle : Shape
    {
        public double Width { get; set; }
        public double Height { get; set; }
    }

    // --- Configuration Class ---
    public class ShapeConfiguration : IJsonEntityTypeConfiguration<Shape>
    {
        public void Configure(JsonEntityTypeBuilder<Shape> builder)
        {
            builder.HasShadowDiscriminator("kind")
                   .HasSubType<Circle>("circle")
                   .HasSubType<Rectangle>("rect");
        }
    }

    [Fact]
    public void Newtonsoft_Should_Handle_Polymorphism_RoundTrip()
    {
        // Arrange
        var builder = new NewtonsoftJson.JsonModelBuilder();
        builder.ApplyConfiguration(new ShapeConfiguration());
        NewtonsoftSerial.JsonSerializerSettings settings = builder.Build();

        var shapes = new List<Shape>
        {
            new Circle { Radius = 5 },
            new Rectangle { Width = 10, Height = 20 }
        };

        // Act - Serialize
        // Expected: JSON array with "kind" property injected
        string json = NewtonsoftSerial.JsonConvert.SerializeObject(shapes, settings);

        // Assert - Check Discriminators
        json.Should().Contain("\"kind\":\"circle\"");
        json.Should().Contain("\"kind\":\"rect\"");

        // Act - Deserialize
        // Expected: Correct concrete types instantiated based on "kind"
        List<Shape>? restored = NewtonsoftSerial.JsonConvert.DeserializeObject<List<Shape>>(json, settings);

        // Assert - Check Types
        restored.Should().HaveCount(2);
        restored![0].Should().BeOfType<Circle>().Which.Radius.Should().Be(5);
        restored[1].Should().BeOfType<Rectangle>().Which.Width.Should().Be(10);
    }

    [Fact]
    public void SystemTextJson_Should_Handle_Polymorphism_RoundTrip()
    {
        // Arrange
        var builder = new SystemTextJson.JsonModelBuilder();
        builder.ApplyConfiguration(new ShapeConfiguration());
        JsonSerializerOptions options = builder.Build();

        var shapes = new List<Shape>
        {
            new Circle { Radius = 5 },
            new Rectangle { Width = 10, Height = 20 }
        };

        // Act - Serialize
        string json = JsonSerializer.Serialize(shapes, options);

        // Assert - Check Discriminators
        json.Should().Contain("\"kind\":\"circle\"");
        json.Should().Contain("\"kind\":\"rect\"");

        // Act - Deserialize
        List<Shape>? restored = JsonSerializer.Deserialize<List<Shape>>(json, options);

        // Assert - Check Types
        restored.Should().HaveCount(2);
        restored![0].Should().BeOfType<Circle>().Which.Radius.Should().Be(5);
        restored[1].Should().BeOfType<Rectangle>().Which.Width.Should().Be(10);
    }
}
