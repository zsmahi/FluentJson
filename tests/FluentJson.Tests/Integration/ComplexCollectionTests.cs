using FluentAssertions;
using FluentJson.Abstractions;
using FluentJson.Builders;
using System.Text.Json;

namespace FluentJson.Tests.Integration;

public class ComplexCollectionTests
{
    public class NestedItem
    {
        public string SecretValue { get; set; } = "content";
    }

    public class NestedItemConfiguration : IJsonEntityTypeConfiguration<NestedItem>
    {
        public void Configure(JsonEntityTypeBuilder<NestedItem> builder)
        {
            builder.Property(x => x.SecretValue).HasPropertyName("data");
        }
    }

    [Fact]
    public void Should_Apply_Configuration_To_Items_Inside_Dictionaries()
    {
        var builder = new SystemTextJson.JsonModelBuilder();
        builder.ApplyConfiguration(new NestedItemConfiguration());
        JsonSerializerOptions options = builder.Build();

        var container = new Dictionary<string, NestedItem>
        {
            { "key1", new NestedItem() }
        };

        string json = JsonSerializer.Serialize(container, options);

        // Verification that "SecretValue" became "data" inside the dictionary entry
        json.Should().Contain("\"key1\":{\"data\":\"content\"}");
    }
}
