using FluentAssertions;
using FluentJson.Abstractions;
using FluentJson.Builders;
using System.Text.Json;

namespace FluentJson.Tests.Integration;

public class InheritanceMappingTests
{
    public class BaseClass
    {
        public string BaseProp { get; set; } = "";
    }

    public class DerivedClass : BaseClass
    {
        public string DerivedProp { get; set; } = "";
    }

    public class BaseConfiguration : IJsonEntityTypeConfiguration<BaseClass>
    {
        public void Configure(JsonEntityTypeBuilder<BaseClass> builder)
        {
            builder.Property(x => x.BaseProp).HasPropertyName("base_mapped");
        }
    }

    public class DerivedConfiguration : IJsonEntityTypeConfiguration<DerivedClass>
    {
        public void Configure(JsonEntityTypeBuilder<DerivedClass> builder)
        {
            builder.Property(x => x.DerivedProp).HasPropertyName("derived_mapped");
        }
    }

    [Fact]
    public void Should_Apply_Hierarchical_Configurations_Correctly()
    {
        var builder = new SystemTextJson.JsonModelBuilder();
        builder.ApplyConfiguration(new BaseConfiguration());
        builder.ApplyConfiguration(new DerivedConfiguration());
        JsonSerializerOptions options = builder.Build();

        var instance = new DerivedClass { BaseProp = "B", DerivedProp = "D" };
        string json = JsonSerializer.Serialize(instance, options);

        json.Should().Contain("\"base_mapped\":\"B\"");
        json.Should().Contain("\"derived_mapped\":\"D\"");
    }
}
