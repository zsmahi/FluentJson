using FluentAssertions;
using FluentJson.Abstractions;
using FluentJson.Builders;
using System.Text.Json;
using NewtonsoftSerial = Newtonsoft.Json;

namespace FluentJson.Tests.Integration;

public class ModernFeaturesTests
{
    public record ImmutableDto
    {
        public string Id { get; init; } = "";
        public string Data { get; init; } = "";
    }

    public class ImmutableDtoConfiguration : IJsonEntityTypeConfiguration<ImmutableDto>
    {
        public void Configure(JsonEntityTypeBuilder<ImmutableDto> builder)
        {
            builder.Property(x => x.Id).HasPropertyName("dto_id");
            builder.Property(x => x.Data).HasPropertyName("payload");
        }
    }

    [Fact]
    public void Newtonsoft_Should_Support_Records_And_Init_Setters()
    {
        var builder = new NewtonsoftJson.JsonModelBuilder();
        builder.ApplyConfiguration(new ImmutableDtoConfiguration());
        NewtonsoftSerial.JsonSerializerSettings settings = builder.Build();

        string json = "{\"dto_id\":\"REC-01\", \"payload\":\"Content\"}";

        ImmutableDto? result = NewtonsoftSerial.JsonConvert.DeserializeObject<ImmutableDto>(json, settings);

        result.Should().NotBeNull();
        result!.Id.Should().Be("REC-01");
        result.Data.Should().Be("Content");
    }

    [Fact]
    public void SystemTextJson_Should_Support_Records_And_Init_Setters()
    {
        var builder = new SystemTextJson.JsonModelBuilder();
        builder.ApplyConfiguration(new ImmutableDtoConfiguration());
        JsonSerializerOptions options = builder.Build();

        string json = "{\"dto_id\":\"REC-01\", \"payload\":\"Content\"}";

        ImmutableDto? result = JsonSerializer.Deserialize<ImmutableDto>(json, options);

        result.Should().NotBeNull();
        result!.Id.Should().Be("REC-01");
        result.Data.Should().Be("Content");
    }
}
