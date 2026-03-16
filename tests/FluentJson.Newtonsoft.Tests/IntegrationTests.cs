using System;

using FluentAssertions;

using FluentJson.Core.Builder;
using FluentJson.Core.Metadata;

using Newtonsoft.Json;

using Xunit;

namespace FluentJson.Newtonsoft.Tests;

public class IntegrationTests
{
    public class Order
    {
        public Guid Id { get; private set; }
        public string CustomerName { get; private set; }
        public int UnmappedProperty { get; set; }
        public decimal Total { get; }

        private Order()
        {
            CustomerName = string.Empty;
        }

        public Order(Guid id, string customerName, decimal total)
        {
            Id = id;
            CustomerName = customerName;
            Total = total;
        }
    }

    private class UnmappedClass
    {
        public string Name { get; set; } = "Test";
    }

    private class ThrowingModel : IJsonModel
    {
        public System.Collections.Generic.IReadOnlyList<IJsonEntity> Entities => throw new InvalidOperationException("Should not be called!");
    }

    [Fact]
    public void AddFluentJson_Should_ConfigureNewtonsoft_ToUsePrivateConstructorAndFluentNames()
    {
        // Arrange
        var builder = new JsonModelBuilder();
        var entityBuilder = builder.Entity<Order>();
        entityBuilder.Property(x => x.Id).HasName("order_id");
        entityBuilder.Property(x => x.CustomerName).HasName("customer_name").IsRequired(true);
        entityBuilder.Property(x => x.Total).HasName("total");

        var model = builder.Build();

        var settings = new JsonSerializerSettings();
        settings.AddFluentJson(model);

        var orderId = Guid.NewGuid();
        var json = $$"""
            {
                "order_id": "{{orderId}}",
                "customer_name": "Acme Corp",
                "UnmappedProperty": 42,
                "total": 99.99
            }
            """;

        // Act
        var result = JsonConvert.DeserializeObject<Order>(json, settings);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(orderId);
        result.CustomerName.Should().Be("Acme Corp");
        result.UnmappedProperty.Should().Be(0); // Ignored implicitly because not mapped
        result.Total.Should().Be(99.99m); // Injected successfully via backing field!
    }

    [Fact]
    public void Serializer_Should_NotThrow_WhenDeserializingUnmappedClass()
    {
        var builder = new JsonModelBuilder();
        var settings = new JsonSerializerSettings().AddFluentJson(builder.Build());

        var json = """{"Name":"Unmapped"}""";
        var result = JsonConvert.DeserializeObject<UnmappedClass>(json, settings);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Unmapped");
    }

    [Fact]
    public void AddFluentJson_Should_ThrowArgumentNullException_WhenArgumentsAreNull()
    {
        var settings = new JsonSerializerSettings();
        var model = new JsonModelBuilder().Build();

        Action act1 = () => FluentJsonSerializerSettingsExtensions.AddFluentJson(null!, model);
        act1.Should().Throw<ArgumentNullException>().WithParameterName("settings");

        Action act2 = () => settings.AddFluentJson(null!);
        act2.Should().Throw<ArgumentNullException>().WithParameterName("model");
    }

    public class ClassA
    {
        public string Name { get; private set; }

        private ClassA() { Name = string.Empty; }
        public ClassA(string name) { Name = name; }
    }

    public class ClassB
    {
        public Guid Id { get; private set; }
        public ClassA A { get; private set; }
        public string Secret { get; private set; }

        private ClassB() { A = new ClassA(""); Secret = string.Empty; }
        public ClassB(Guid id, ClassA a, string secret) { Id = id; A = a; Secret = secret; }
    }

    [Fact]
    public void ComplexGraph_Should_SerializeAndDeserialize_WithIgnoredProperties()
    {
        // Arrange
        var builder = new JsonModelBuilder();

        builder.Entity<ClassA>()
            .Property(x => x.Name).HasName("a_name");

        var entityB = builder.Entity<ClassB>();
        entityB.Property(x => x.Id).HasName("b_id");
        entityB.Property(x => x.A).HasName("nested_a");
        entityB.Ignore(x => x.Secret);

        var settings = new JsonSerializerSettings().AddFluentJson(builder.Build());

        var bId = Guid.NewGuid();
        var originalB = new ClassB(bId, new ClassA("TestName"), "MySecretValue");

        // Act
        var json = JsonConvert.SerializeObject(originalB, settings);
        var deserializedB = JsonConvert.DeserializeObject<ClassB>(json, settings);

        // Assert
        json.Should().NotContain("MySecretValue");
        json.Should().NotContain("Secret");
        json.Should().Contain("a_name");
        json.Should().Contain("TestName");
        json.Should().Contain("b_id");
        json.Should().Contain("nested_a");

        deserializedB.Should().NotBeNull();
        deserializedB!.Id.Should().Be(bId);
        deserializedB.A.Should().NotBeNull();
        deserializedB.A.Name.Should().Be("TestName");

        // Secret is ignored, so it shouldn't be serialized or deserialized
        deserializedB.Secret.Should().Be(string.Empty);
    }

    [Fact]
    // Tests that property.Required = Required.Always is correctly applied
    public void AddFluentJson_Should_EnforceRequiredProperties()
    {
        var builder = new JsonModelBuilder();
        builder.Entity<Order>().Property(x => x.CustomerName).IsRequired();

        var settings = new JsonSerializerSettings().AddFluentJson(builder.Build());

        // This JSON is missing CustomerName
        var json = """{"Id": "00000000-0000-0000-0000-000000000000", "Total": 0}""";

        Action act = () => JsonConvert.DeserializeObject<Order>(json, settings);

        act.Should().Throw<JsonSerializationException>()
           .WithMessage("*Required property 'CustomerName'*");
    }

    [Fact]
    // Tests that property.Required = Required.Default allows missing fields
    public void AddFluentJson_Should_AllowMissingProperties_WhenNotRequired()
    {
        var builder = new JsonModelBuilder();
        builder.Entity<Order>().Property(x => x.CustomerName); // Not required

        var settings = new JsonSerializerSettings().AddFluentJson(builder.Build());

        // This JSON is missing CustomerName, but it's not required
        var json = """{"Id": "00000000-0000-0000-0000-000000000000", "Total": 0}""";

        var result = JsonConvert.DeserializeObject<Order>(json, settings);

        result.Should().NotBeNull();
        result!.CustomerName.Should().BeEmpty();
    }
}
