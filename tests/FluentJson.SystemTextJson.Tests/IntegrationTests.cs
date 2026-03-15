using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using FluentAssertions;
using FluentJson.Core.Builder;
using FluentJson.Core.Metadata;
using Xunit;

namespace FluentJson.SystemTextJson.Tests;

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
    public void AddFluentJson_Should_ConfigureSTJ_ToUsePrivateConstructorAndFluentNames()
    {
        // Arrange
        var builder = new JsonModelBuilder();
        var entityBuilder = builder.Entity<Order>();
        entityBuilder.Property(x => x.Id).HasName("order_id");
        entityBuilder.Property(x => x.CustomerName).HasName("customer_name").IsRequired(true);
        entityBuilder.Property(x => x.Total).HasName("total");

        var model = builder.Build();

        var options = new JsonSerializerOptions();
        options.AddFluentJson(model);

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
        var result = JsonSerializer.Deserialize<Order>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(orderId);
        result.CustomerName.Should().Be("Acme Corp");
        result.UnmappedProperty.Should().Be(0); // Ignored implicitly because not mapped
        result.Total.Should().Be(99.99m); // Successfully injected via <Total>k__BackingField!
    }

    [Fact]
    public void Serializer_Should_NotThrow_WhenDeserializingUnmappedClass()
    {
        var builder = new JsonModelBuilder();
        var options = new JsonSerializerOptions().AddFluentJson(builder.Build());
        
        var json = """{"Name":"Unmapped"}""";
        var result = JsonSerializer.Deserialize<UnmappedClass>(json, options);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Unmapped");
    }

    [Fact]
    public void Modifier_Should_ReturnEarly_WhenTypeIsNotObject()
    {
        var modifier = new FluentJsonTypeModifier(new ThrowingModel());
        var resolver = new DefaultJsonTypeInfoResolver();
        
        var typeInfo = resolver.GetTypeInfo(typeof(int[]), new JsonSerializerOptions());
        
        modifier.Modify(typeInfo);
    }

    [Fact]
    public void AddFluentJson_Should_ThrowArgumentNullException_WhenArgumentsAreNull()
    {
        var options = new JsonSerializerOptions();
        var model = new JsonModelBuilder().Build();

        Action act1 = () => FluentJsonSerializerOptionsExtensions.AddFluentJson(null!, model);
        act1.Should().Throw<ArgumentNullException>().WithParameterName("options");

        Action act2 = () => options.AddFluentJson(null!);
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
            
        var options = new JsonSerializerOptions().AddFluentJson(builder.Build());

        var bId = Guid.NewGuid();
        var originalB = new ClassB(bId, new ClassA("TestName"), "MySecretValue");

        // Act
        var json = JsonSerializer.Serialize(originalB, options);
        var deserializedB = JsonSerializer.Deserialize<ClassB>(json, options);

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
}
