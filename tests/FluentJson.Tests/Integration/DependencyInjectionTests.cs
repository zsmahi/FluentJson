using FluentAssertions;
using FluentJson.Abstractions;
using FluentJson.Builders;
using NSubstitute;
using System.Text.Json;
using NewtonsoftSerial = Newtonsoft.Json;
using StjSerial = System.Text.Json.Serialization;

namespace FluentJson.Tests.Integration;

public class DependencyInjectionTests
{
    // --- Shared Domain ---
    public interface ITestService
    {
        string Transform(string input);
    }

    public class TestEntity
    {
        public string Value { get; set; } = "";
    }

    // --- Newtonsoft Implementation ---

    public class NewtonsoftDiConverter : NewtonsoftSerial.JsonConverter
    {
        private readonly ITestService _service;

        public NewtonsoftDiConverter(ITestService service) => _service = service;

        public override void WriteJson(NewtonsoftSerial.JsonWriter writer, object? value, NewtonsoftSerial.JsonSerializer serializer)
        {
            writer.WriteValue(_service.Transform((string)value!));
        }

        public override object? ReadJson(NewtonsoftSerial.JsonReader reader, Type objectType, object? existingValue, NewtonsoftSerial.JsonSerializer serializer)
        {
            return _service.Transform((string)reader.Value!);
        }

        public override bool CanConvert(Type objectType) => objectType == typeof(string);
    }

    // Configuration class for Newtonsoft Scenario
    public class NewtonsoftTestConfiguration : IJsonEntityTypeConfiguration<TestEntity>
    {
        public void Configure(JsonEntityTypeBuilder<TestEntity> builder)
        {
            builder.Property(x => x.Value)
                   .HasConversion<NewtonsoftDiConverter>();
        }
    }

    [Fact]
    public void Newtonsoft_Should_Inject_Service_Into_Converter()
    {
        // 1. Arrange - The Mock
        ITestService serviceMock = Substitute.For<ITestService>();
        serviceMock.Transform(Arg.Any<string>()).Returns("INJECTED_NEWTONSOFT");

        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(NewtonsoftDiConverter)).Returns(new NewtonsoftDiConverter(serviceMock));

        // 2. Arrange - The Builder
        var builder = new NewtonsoftJson.JsonModelBuilder();

        // Correct Usage: Apply the external configuration class
        builder.ApplyConfiguration(new NewtonsoftTestConfiguration());

        // 3. Act - Build with Provider
        NewtonsoftSerial.JsonSerializerSettings settings = builder.Build(serviceProvider);

        // Serialize
        var entity = new TestEntity { Value = "original" };
        string json = NewtonsoftSerial.JsonConvert.SerializeObject(entity, settings);

        // 4. Assert
        json.Should().Contain("INJECTED_NEWTONSOFT");
        serviceMock.Received().Transform("original");
    }

    // --- System.Text.Json Implementation ---

    public class StjDiConverter : StjSerial.JsonConverter<string>
    {
        private readonly ITestService _service;

        public StjDiConverter(ITestService service) => _service = service;

        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return _service.Transform(reader.GetString()!);
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(_service.Transform(value));
        }
    }

    // Configuration class for STJ Scenario
    public class StjTestConfiguration : IJsonEntityTypeConfiguration<TestEntity>
    {
        public void Configure(JsonEntityTypeBuilder<TestEntity> builder)
        {
            builder.Property(x => x.Value)
                   .HasConversion<StjDiConverter>();
        }
    }

    [Fact]
    public void SystemTextJson_Should_Inject_Service_Into_Converter()
    {
        // 1. Arrange - The Mock
        ITestService serviceMock = Substitute.For<ITestService>();
        serviceMock.Transform(Arg.Any<string>()).Returns("INJECTED_STJ");

        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(StjDiConverter)).Returns(new StjDiConverter(serviceMock));

        // 2. Arrange - The Builder
        var builder = new SystemTextJson.JsonModelBuilder();

        // Correct Usage: Apply the external configuration class
        builder.ApplyConfiguration(new StjTestConfiguration());

        // 3. Act - Build with Provider
        JsonSerializerOptions options = builder.Build(serviceProvider);

        // Serialize
        var entity = new TestEntity { Value = "original" };
        string json = JsonSerializer.Serialize(entity, options);

        // 4. Assert
        json.Should().Contain("INJECTED_STJ");
        serviceMock.Received().Transform("original");
    }
}
