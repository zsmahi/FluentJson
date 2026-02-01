using FluentAssertions;
using FluentJson.Abstractions;
using FluentJson.Builders;
using System.Text.Json;

namespace FluentJson.Tests.Integration;

public class NamingPolicyConflictTests
{
    public class ConflictEntity
    {
        public string StandardProperty { get; set; } = "v1";
        public string OverriddenProperty { get; set; } = "v2";
    }

    public class ConflictConfiguration : IJsonEntityTypeConfiguration<ConflictEntity>
    {
        public void Configure(JsonEntityTypeBuilder<ConflictEntity> builder)
        {
            // This explicit name must NOT be transformed by the SnakeCase convention
            builder.Property(x => x.OverriddenProperty).HasPropertyName("URGENT_Override");
        }
    }

    [Fact]
    public void Explicit_Property_Names_Should_Override_Global_Naming_Policy()
    {
        var builder = new SystemTextJson.JsonModelBuilder();
        builder.UseSnakeCaseNamingConvention();
        builder.ApplyConfiguration(new ConflictConfiguration());
        JsonSerializerOptions options = builder.Build();

        var entity = new ConflictEntity();
        string json = JsonSerializer.Serialize(entity, options);

        // StandardProperty -> standard_property (Policy)
        json.Should().Contain("\"standard_property\":\"v1\"");

        // OverriddenProperty -> URGENT_Override (Explicit)
        json.Should().Contain("\"URGENT_Override\":\"v2\"");
        json.Should().NotContain("urgent_override");
    }
}
