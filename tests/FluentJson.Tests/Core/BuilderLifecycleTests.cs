using FluentAssertions;
using FluentJson.Builders;
using FluentJson.Definitions;
using FluentJson.Exceptions;

namespace FluentJson.Tests.Core;

public class BuilderLifecycleTests
{
    // Dummy entity used strictly for testing the builder mechanics
    private class TestEntity
    {
        public string Name { get; set; } = "";
    }

    [Fact]
    public void PropertyBuilder_Should_Cache_Instances_Per_Property()
    {
        // Arrange
        var builder = new JsonEntityTypeBuilder<TestEntity>();

        // Act
        // Request configuration for the same property twice
        JsonPropertyBuilder<TestEntity, string> propBuilder1 = builder.Property(x => x.Name);
        JsonPropertyBuilder<TestEntity, string> propBuilder2 = builder.Property(x => x.Name);

        // Assert
        // Architectural Requirement: The builder must return the exact same instance 
        // to preserve the configuration state (chaining continuity).
        propBuilder1.Should().BeSameAs(propBuilder2);
    }

    [Fact]
    public void Builder_Should_Use_ShadowState_Before_Apply()
    {
        // Arrange
        var builder = new JsonEntityTypeBuilder<TestEntity>();

        // Act
        // Configure using fluent API (writes to internal Shadow State)
        builder.Property(x => x.Name).HasPropertyName("customer_name");

        // Assert
        // Force access to the internal definition via the interface to verify the "Commit" logic
        JsonEntityDefinition definition = ((IJsonEntityTypeBuilderAccessor)builder).Definition;
        JsonPropertyDefinition propDef = definition.GetOrCreateMember(typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!);

        // The state must have been flushed from the Builder to the Definition
        propDef.JsonName.Should().Be("customer_name");
    }

    [Fact]
    public void Freeze_Should_Make_Configuration_Immutable()
    {
        // Arrange
        var builder = new JsonEntityTypeBuilder<TestEntity>();
        builder.Property(x => x.Name).HasPropertyName("initial");

        // Simulate the Build() phase completion
        JsonEntityDefinition definition = ((IJsonEntityTypeBuilderAccessor)builder).Definition;
        definition.Freeze();

        // Assert - Verify State
        definition.IsFrozen.Should().BeTrue();

        // Assert - Security Check
        // Attempting to modify the configuration after freeze must throw a specific exception
        Action act = () => definition.EnablePolymorphism("type");

        act.Should().Throw<FluentJsonConfigurationException>()
           .WithMessage("*frozen*");
    }

    [Fact]
    public void Freeze_Should_Propagate_Recursively_To_Properties()
    {
        // Arrange
        var builder = new JsonEntityTypeBuilder<TestEntity>();
        builder.Property(x => x.Name).HasOrder(1);

        JsonEntityDefinition definition = ((IJsonEntityTypeBuilderAccessor)builder).Definition;

        // Act
        definition.Freeze();

        // Assert
        // The freeze state must cascade down to child property definitions
        JsonPropertyDefinition propDef = definition.Properties.Values.First();
        propDef.IsFrozen.Should().BeTrue();
    }
}
