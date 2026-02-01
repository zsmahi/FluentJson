using FluentAssertions;
using FluentJson.Abstractions;
using FluentJson.Builders;
using System.Text.Json;

namespace FluentJson.Tests.Integration;

public class ShadowPropertyTests
{
    public class BaseWithField
    {
        private readonly string _internalData = "initial";
        public string PublicData => _internalData;
    }

    public class DerivedWithField : BaseWithField { }

    public class ShadowConfiguration : IJsonEntityTypeConfiguration<BaseWithField>
    {
        public void Configure(JsonEntityTypeBuilder<BaseWithField> builder)
        {
            builder.Property(x => x.PublicData)
                   .HasField("_internalData")
                   .HasPropertyName("raw_data");
        }
    }

    [Fact]
    public void Should_Respect_Field_Redirection_In_Derived_Classes()
    {
        var builder = new SystemTextJson.JsonModelBuilder();
        builder.ApplyConfiguration(new ShadowConfiguration());
        JsonSerializerOptions options = builder.Build();

        var instance = new DerivedWithField();
        string json = JsonSerializer.Serialize(instance, options);

        json.Should().Contain("\"raw_data\":\"initial\"");
        json.Should().NotContain("PublicData");
    }
}
