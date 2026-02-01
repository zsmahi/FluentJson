using FluentAssertions;
using FluentJson.Abstractions;
using FluentJson.Builders;
using NSubstitute;
using System.Reflection;
using System.Text.Json;
using NewtonsoftSerial = Newtonsoft.Json;

namespace FluentJson.Tests.Integration;

public class ConfigurationDiscoveryTests
{
    // --- Domain & Services ---
    public class ScannedEntity
    {
        public string Original { get; set; } = "";
    }

    public interface IPrefixService
    {
        string GetPrefix();
    }

    // --- Configuration Class with Constructor Injection ---
    public class ScannedEntityConfiguration(ConfigurationDiscoveryTests.IPrefixService prefixService) : IJsonEntityTypeConfiguration<ScannedEntity>
    {
        private readonly IPrefixService _prefixService = prefixService;

        public void Configure(JsonEntityTypeBuilder<ScannedEntity> builder)
        {
            builder.Property(x => x.Original)
                   .HasPropertyName(_prefixService.GetPrefix() + "_original");
        }
    }

    [Fact]
    public void Newtonsoft_Should_Discover_Configurations_And_Inject_Dependencies()
    {
        // 1. Arrange - Service Provider Mocking
        IPrefixService prefixService = Substitute.For<IPrefixService>();
        prefixService.GetPrefix().Returns("nw");

        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();

        serviceProvider.GetService(typeof(IPrefixService)).Returns(prefixService);

        serviceProvider.GetService(typeof(ScannedEntityConfiguration))
            .Returns(new ScannedEntityConfiguration(prefixService));

        // 2. Act - Configure Builder via Scanning
        var builder = new FluentJson.NewtonsoftJson.JsonModelBuilder();


        builder.ApplyConfigurationsFromAssemblies(serviceProvider, Assembly.GetExecutingAssembly());

        NewtonsoftSerial.JsonSerializerSettings settings = builder.Build(serviceProvider);

        // 3. Verify Serialization
        var entity = new ScannedEntity { Original = "Value" };
        string json = NewtonsoftSerial.JsonConvert.SerializeObject(entity, settings);

        // 4. Assert
        json.Should().Contain("\"nw_original\":\"Value\"");
    }

    [Fact]
    public void SystemTextJson_Should_Discover_Configurations_And_Inject_Dependencies()
    {
        // 1. Arrange - Service Provider Mocking
        IPrefixService prefixService = Substitute.For<IPrefixService>();
        prefixService.GetPrefix().Returns("stj");

        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IPrefixService)).Returns(prefixService);
        serviceProvider.GetService(typeof(ScannedEntityConfiguration))
            .Returns(new ScannedEntityConfiguration(prefixService));

        // 2. Act - Configure Builder via Scanning
        var builder = new FluentJson.SystemTextJson.JsonModelBuilder();

        builder.ApplyConfigurationsFromAssemblies(serviceProvider, Assembly.GetExecutingAssembly());

        JsonSerializerOptions options = builder.Build(serviceProvider);

        // 3. Verify Serialization
        var entity = new ScannedEntity { Original = "Value" };
        string json = JsonSerializer.Serialize(entity, options);

        // 4. Assert
        json.Should().Contain("\"stj_original\":\"Value\"");
    }
}
