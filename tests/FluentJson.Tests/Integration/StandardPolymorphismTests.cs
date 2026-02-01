using FluentAssertions;
using FluentJson.Abstractions;
using FluentJson.Builders;
using System.Text.Json;
using NewtonsoftSerial = Newtonsoft.Json;

namespace FluentJson.Tests.Integration;

public class StandardPolymorphismTests
{
    // --- Domain Model ---
    public abstract class Message
    {
        // The discriminator is a real C# property here
        public string MessageType { get; set; } = "unknown";
    }

    public class TextMessage : Message
    {
        public string Content { get; set; } = "";
    }

    public class ImageMessage : Message
    {
        public string Url { get; set; } = "";
    }

    // --- Configuration ---
    public class MessageConfiguration : IJsonEntityTypeConfiguration<Message>
    {
        public void Configure(JsonEntityTypeBuilder<Message> builder)
        {
            // Map the discriminator to the existing 'MessageType' property
            builder.HasDiscriminator(x => x.MessageType)
                   .HasSubType<TextMessage>("text")
                   .HasSubType<ImageMessage>("image");
        }
    }

    [Fact]
    public void Newtonsoft_Should_Use_Real_Property_As_Discriminator()
    {
        // Arrange
        var builder = new NewtonsoftJson.JsonModelBuilder();
        builder.ApplyConfiguration(new MessageConfiguration());
        NewtonsoftSerial.JsonSerializerSettings settings = builder.Build();

        var messages = new List<Message>
        {
            new TextMessage { Content = "Hello" },
            new ImageMessage { Url = "http://img.com/1.png" }
        };

        // Act - Serialize
        string json = NewtonsoftSerial.JsonConvert.SerializeObject(messages, settings);

        // Assert - Serialization
        json.Should().Contain("\"MessageType\":\"text\"");
        json.Should().Contain("\"MessageType\":\"image\"");

        // Act - Deserialize
        List<Message>? restored = NewtonsoftSerial.JsonConvert.DeserializeObject<List<Message>>(json, settings);

        // Assert - Deserialization
        restored.Should().HaveCount(2);
        restored![0].Should().BeOfType<TextMessage>();
        restored![0].MessageType.Should().Be("text"); // Property should be populated
    }

    [Fact]
    public void SystemTextJson_Should_Use_Real_Property_As_Discriminator()
    {
        // Arrange
        var builder = new SystemTextJson.JsonModelBuilder();
        builder.ApplyConfiguration(new MessageConfiguration());
        JsonSerializerOptions options = builder.Build();

        var messages = new List<Message>
        {
            new TextMessage { Content = "Hello" },
            new ImageMessage { Url = "http://img.com/1.png" }
        };

        // Act - Serialize
        string json = JsonSerializer.Serialize(messages, options);

        // Assert - Serialization
        json.Should().Contain("\"MessageType\":\"text\"");
        json.Should().Contain("\"MessageType\":\"image\"");

        // Act - Deserialize
        List<Message>? restored = JsonSerializer.Deserialize<List<Message>>(json, options);

        // Assert - Deserialization
        restored.Should().HaveCount(2);
        restored![0].Should().BeOfType<TextMessage>();
        restored![0].MessageType.Should().Be("text");
    }
}
