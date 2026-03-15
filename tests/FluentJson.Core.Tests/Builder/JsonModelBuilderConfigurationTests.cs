using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using FluentJson.Core.Builder;
using Xunit;

namespace FluentJson.Core.Tests.Builder;

public class JsonModelBuilderConfigurationTests
{
    private class DummyClass
    {
        public string Name { get; set; } = string.Empty;
    }

    private class AnotherClass
    {
        public int Value { get; set; }
    }

    private interface IDummyInterface { }

    private class DummyConfiguration : IJsonTypeConfiguration<DummyClass>, IDummyInterface
    {
        public void Configure(EntityTypeBuilder<DummyClass> builder)
        {
            builder.Property(x => x.Name).HasName("dummy_name").IsRequired(true);
        }
    }

    private abstract class AbstractConfiguration : IJsonTypeConfiguration<DummyClass>
    {
        public abstract void Configure(EntityTypeBuilder<DummyClass> builder);
    }

    private class AnotherConfiguration : IJsonTypeConfiguration<AnotherClass>
    {
        public void Configure(EntityTypeBuilder<AnotherClass> builder)
        {
            builder.Property(x => x.Value).HasName("another_value");
        }
    }

    private class MissingConstructorConfiguration : IJsonTypeConfiguration<DummyClass>
    {
        public MissingConstructorConfiguration(int someValue)
        {
        }

        public void Configure(EntityTypeBuilder<DummyClass> builder)
        {
        }
    }

    [Fact]
    public void ApplyConfiguration_Should_ExecuteConfigurationClass()
    {
        // Arrange
        var builder = new JsonModelBuilder();
        var config = new DummyConfiguration();

        // Act
        builder.ApplyConfiguration(config);
        var model = builder.Build();

        // Assert
        model.Entities.Should().HaveCount(1);
        var entity = model.Entities.First();
        entity.EntityType.Should().Be(typeof(DummyClass));
        entity.Properties.Should().ContainSingle(p => p.Name == "dummy_name" && p.IsRequired);
    }

    [Fact]
    public void ApplyConfiguration_Should_ThrowArgumentNullException_WhenConfigurationIsNull()
    {
        var builder = new JsonModelBuilder();
        Action act = () => builder.ApplyConfiguration<DummyClass>(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
    }

    private class MockAssembly : Assembly
    {
        private readonly Type[] _types;
        public MockAssembly(params Type[] types) { _types = types; }
        public override Type[] GetTypes() => _types;
    }

    [Fact]
    public void ApplyConfigurationsFromAssembly_Should_ScanAndRegisterAllConfigurations()
    {
        var builder = new JsonModelBuilder();
        
        // Pass a mock assembly so we only scan valid classes, avoiding MissingConstructorConfiguration
        // Includes an abstract class to ensure !t.IsAbstract is correctly covered
        var mockAssembly = new MockAssembly(typeof(DummyConfiguration), typeof(AnotherConfiguration), typeof(AbstractConfiguration));

        builder.ApplyConfigurationsFromAssembly(mockAssembly);
        var model = builder.Build();

        model.Entities.Should().HaveCount(2);
        
        var dummyEntity = model.Entities.FirstOrDefault(e => e.EntityType == typeof(DummyClass));
        dummyEntity.Should().NotBeNull();
        dummyEntity!.Properties.Should().Contain(p => p.Name == "dummy_name");

        var anotherEntity = model.Entities.FirstOrDefault(e => e.EntityType == typeof(AnotherClass));
        anotherEntity.Should().NotBeNull();
        anotherEntity!.Properties.Should().Contain(p => p.Name == "another_value");
    }

    [Fact]
    public void ApplyConfigurationsFromAssembly_Should_ThrowArgumentException_WhenClassLacksParameterlessConstructor()
    {
        var builder = new JsonModelBuilder();
        
        // Pass a mock assembly that ONLY has the missing constructor config
        var mockAssembly = new MockAssembly(typeof(MissingConstructorConfiguration));
        
        Action act = () => builder.ApplyConfigurationsFromAssembly(mockAssembly);
        
        act.Should().Throw<ArgumentException>()
            .WithMessage("*does not have a public parameterless constructor*")
            .WithParameterName("assembly");
    }

    [Fact]
    public void ApplyConfigurationsFromAssembly_Should_ThrowArgumentNullException_WhenAssemblyIsNull()
    {
        var builder = new JsonModelBuilder();
        Action act = () => builder.ApplyConfigurationsFromAssembly(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("assembly");
    }
}
