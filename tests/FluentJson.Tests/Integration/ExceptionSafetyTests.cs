using System.Text.Json;
using FluentAssertions;
using FluentJson.Abstractions;
using FluentJson.Builders;
using FluentJson.Exceptions;
using NSubstitute;

namespace FluentJson.Tests.Integration;

public class ExceptionSafetyTests
{
    public class MissingConverterEntity { public string Data { get; set; } = default!; }
    public class MissingConverter : System.Text.Json.Serialization.JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o) => "";
        public override void Write(Utf8JsonWriter w, string v, JsonSerializerOptions o) { }
    }

    public class MissingConfig : IJsonEntityTypeConfiguration<MissingConverterEntity>
    {
        public void Configure(JsonEntityTypeBuilder<MissingConverterEntity> builder)
        {
            builder.Property(x => x.Data).HasConversion<MissingConverter>();
        }
    }

    [Fact]
    public void Build_Should_Throw_Clear_Exception_When_DI_Resolution_Fails()
    {
        var builder = new FluentJson.SystemTextJson.JsonModelBuilder();
        builder.ApplyConfiguration(new MissingConfig());

        // Mock a provider that returns null for the required converter
        IServiceProvider provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(MissingConverter)).Returns(null);

        Action act = () => builder.Build(provider);

        act.Should().Throw<FluentJsonConfigurationException>()
           .WithMessage("*DI Resolution failed*");
    }
}
