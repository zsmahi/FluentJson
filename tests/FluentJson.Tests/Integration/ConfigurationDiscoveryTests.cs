using FluentAssertions;
using FluentJson.Abstractions;
using FluentJson.Builders;
using NSubstitute;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using NewtonsoftSerial = Newtonsoft.Json;

namespace FluentJson.Tests.Integration;

public class ConfigurationDiscoveryTests
{
    // --- Domain & Services ---
    public class ScannedEntity
    {
        public string Original { get; set; } = "";
        public string BoundData { get; set; } = "";
    }

    public interface IPrefixService
    {
        string GetPrefix();
    }

    // This converter requires a service, so it MUST be in the DI container
    public class StjDiConverter : JsonConverter<string>
    {
        private readonly IPrefixService _prefixService;
        public StjDiConverter(IPrefixService prefixService) => _prefixService = prefixService;

        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetString() ?? string.Empty;

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            => writer.WriteStringValue($"{_prefixService.GetPrefix()}_{value}");
    }

    // --- Configuration Class ---
    public class ScannedEntityConfiguration : IJsonEntityTypeConfiguration<ScannedEntity>
    {
        private readonly IPrefixService _prefixService;
        public ScannedEntityConfiguration(IPrefixService prefixService) => _prefixService = prefixService;

        public void Configure(JsonEntityTypeBuilder<ScannedEntity> builder)
        {
            builder.Property(x => x.Original)
                   .HasPropertyName(_prefixService.GetPrefix() + "_original");

            // This line forces the builder to resolve StjDiConverter during Build()
            builder.Property(x => x.BoundData)
                   .HasConversion<StjDiConverter>();
        }
    }

    [Fact]
    public void SystemTextJson_Should_Discover_Configurations_And_Inject_Dependencies()
    {
        // 1. Arrange
        var prefixService = Substitute.For<IPrefixService>();
        prefixService.GetPrefix().Returns("stj");

        var serviceProvider = Substitute.For<IServiceProvider>();

        serviceProvider.GetService(typeof(IPrefixService)).Returns(prefixService);

        serviceProvider.GetService(typeof(ScannedEntityConfiguration))
            .Returns(new ScannedEntityConfiguration(prefixService));

        serviceProvider.GetService(typeof(StjDiConverter))
            .Returns(new StjDiConverter(prefixService));

        // 2. Act
        var builder = new FluentJson.SystemTextJson.JsonModelBuilder();

        builder.ApplyConfiguration(new ScannedEntityConfiguration(prefixService));

        var options = builder.Build(serviceProvider);

        // 3. Verify
        var entity = new ScannedEntity { Original = "Value", BoundData = "Data" };
        string json = JsonSerializer.Serialize(entity, options);

        // 4. Assert
        json.Should().Contain("\"stj_original\":\"Value\"");
        json.Should().Contain("\"BoundData\":\"stj_Data\"");
    }
    [Fact]
    public void Newtonsoft_Should_Discover_Configurations_And_Inject_Dependencies()
    {
        // 1. Arrange
        var prefixService = Substitute.For<IPrefixService>();
        prefixService.GetPrefix().Returns("nw");

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IPrefixService)).Returns(prefixService);

        serviceProvider.GetService(typeof(ScannedEntityConfiguration))
            .Returns(new ScannedEntityConfiguration(prefixService));

        // 2. Act
        var builder = new NewtonsoftJson.JsonModelBuilder();
        builder.ApplyConfigurationsFromAssemblies(serviceProvider, Assembly.GetExecutingAssembly());
        var settings = builder.Build(serviceProvider);

        // 3. Verify
        var entity = new ScannedEntity { Original = "Value" };
        string json = NewtonsoftSerial.JsonConvert.SerializeObject(entity, settings);

        // 4. Assert
        json.Should().Contain("\"nw_original\":\"Value\"");
    }
}
