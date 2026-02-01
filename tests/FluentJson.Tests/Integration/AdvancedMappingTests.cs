using FluentAssertions;
using FluentJson.Abstractions;
using FluentJson.Builders;
using System.Text.Json;

namespace FluentJson.Tests.Integration;

public class AdvancedMappingTests
{
    // --- Scenario 1: Generic Envelopes ---
    public class Envelope<T>
    {
        public T Content { get; set; }
        public string Metadata { get; set; } = "v1";
    }

    public class EnvelopeConfiguration<T> : IJsonEntityTypeConfiguration<Envelope<T>>
    {
        public void Configure(JsonEntityTypeBuilder<Envelope<T>> builder)
        {
            builder.Property(x => x.Content).HasPropertyName("payload");
            builder.Property(x => x.Metadata).HasPropertyName("version");
        }
    }

    // --- Scenario 2: Member Hiding (new keyword) ---
    public class HiddenBase { public string Value { get; set; } = "base"; }
    public class HiddenDerived : HiddenBase { public new int Value { get; set; } = 42; }

    public class HiddenConfiguration : IJsonEntityTypeConfiguration<HiddenDerived>
    {
        public void Configure(JsonEntityTypeBuilder<HiddenDerived> builder)
        {
            builder.Property(x => x.Value).HasPropertyName("count");
        }
    }

    [Fact]
    public void Should_Handle_Generic_Types_Correctly()
    {
        var builder = new SystemTextJson.JsonModelBuilder();
        builder.ApplyConfiguration(new EnvelopeConfiguration<string>());
        JsonSerializerOptions options = builder.Build();

        var envelope = new Envelope<string> { Content = "Hello" };
        string json = JsonSerializer.Serialize(envelope, options);

        json.Should().Contain("\"payload\":\"Hello\"");
        json.Should().Contain("\"version\":\"v1\"");
    }

    [Fact]
    public void Should_Handle_Member_Hiding_With_New_Keyword()
    {
        var builder = new SystemTextJson.JsonModelBuilder();
        builder.ApplyConfiguration(new HiddenConfiguration());
        JsonSerializerOptions options = builder.Build();

        var derived = new HiddenDerived();
        string json = JsonSerializer.Serialize(derived, options);

        json.Should().Contain("\"count\":42");
        json.Should().NotContain("base");
    }
}
