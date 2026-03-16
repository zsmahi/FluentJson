using System;

using FluentAssertions;

using FluentJson.Core.Builder;

using Xunit;

namespace FluentJson.Core.Tests;

public class JsonModelBuilderTests
{
    private record Customer(Guid Id, string Name)
    {
        private Customer() : this(Guid.Empty, string.Empty) { }
    }

    [Fact]
    public void Entity_Should_ReturnNewEntityTypeBuilder_ForGivenType()
    {
        // Arrange
        var builder = new JsonModelBuilder();

        // Act
        var entityBuilder = builder.Entity<Customer>();

        // Assert
        entityBuilder.Should().NotBeNull();
        entityBuilder.Should().BeOfType<EntityTypeBuilder<Customer>>();
    }

    [Fact]
    public void Entity_Should_ReturnSameInstance_WhenCalledMultipleTimesForSameType()
    {
        // Arrange
        var builder = new JsonModelBuilder();

        // Act
        var entityBuilder1 = builder.Entity<Customer>();
        var entityBuilder2 = builder.Entity<Customer>();

        // Assert
        entityBuilder1.Should().BeSameAs(entityBuilder2);
    }

    [Fact]
    public void Build_Should_ReturnImmutableJsonModel_WithConfiguredEntitiesAndProperties()
    {
        // Arrange
        var builder = new JsonModelBuilder();
        var entityBuilder = builder.Entity<Customer>();
        entityBuilder.Property(x => x.Name);
        entityBuilder.Property(x => x.Id);

        // Act
        var model = builder.Build();

        // Assert
        model.Should().NotBeNull();
        model.Entities.Should().HaveCount(1);

        var entity = model.Entities[0];
        entity.EntityType.Should().Be(typeof(Customer));
        entity.Properties.Should().HaveCount(2);

        entity.Properties.Should().Contain(p => p.Name == "Name" && p.MemberInfo.Name == "Name");
        entity.Properties.Should().Contain(p => p.Name == "Id" && p.MemberInfo.Name == "Id");
    }

    [Fact]
    public void JsonModel_Should_ThrowArgumentNullException_WhenEntitiesAreNull()
    {
        Action act = () => new FluentJson.Core.Metadata.JsonModel(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void JsonEntity_Should_ThrowArgumentNullException_WhenParametersAreNull()
    {
        Action act1 = () => new FluentJson.Core.Metadata.JsonEntity(null!, [], () => new object(), null, new System.Collections.ObjectModel.ReadOnlyDictionary<object, System.Type>(new System.Collections.Generic.Dictionary<object, System.Type>()), false);
        act1.Should().Throw<ArgumentNullException>();

        Action act2 = () => new FluentJson.Core.Metadata.JsonEntity(typeof(Customer), null!, () => new object(), null, new System.Collections.ObjectModel.ReadOnlyDictionary<object, System.Type>(new System.Collections.Generic.Dictionary<object, System.Type>()), false);
        act2.Should().Throw<ArgumentNullException>();

        Action act3 = () => new FluentJson.Core.Metadata.JsonEntity(typeof(Customer), [], null!, null, new System.Collections.ObjectModel.ReadOnlyDictionary<object, System.Type>(new System.Collections.Generic.Dictionary<object, System.Type>()), false);
        act3.Should().Throw<ArgumentNullException>();

        Action act4 = () => new FluentJson.Core.Metadata.JsonEntity(typeof(Customer), [], () => new object(), null, null!, false);
        act4.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void JsonProperty_Should_ThrowArgumentNullException_WhenParametersAreNull()
    {
        var dummyProperty = typeof(Customer).GetProperty(nameof(Customer.Name))!;

        Action act1 = () => new FluentJson.Core.Metadata.JsonProperty("Name", null!, false, false, null, null, null);
        act1.Should().Throw<ArgumentNullException>();

        Action act2 = () => new FluentJson.Core.Metadata.JsonProperty(null!, dummyProperty, false, false, null, null, null);
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void JsonProperty_Should_ThrowArgumentException_WhenMemberIsNotPropertyOrField()
    {
        var dummyMethod = typeof(Customer).GetMethod(nameof(Customer.ToString))!;

        Action act = () => new FluentJson.Core.Metadata.JsonProperty("Name", dummyMethod, false, false, null, null, null);
        act.Should().Throw<ArgumentException>()
           .WithParameterName("memberInfo")
           .WithMessage("*Member must be a property or a field*");
    }

    [Fact]
    public void Build_Should_SetIsRequiredToFalse_ByDefault()
    {
        // Arrange
        var builder = new JsonModelBuilder();
        builder.Entity<Customer>().Property(x => x.Name);

        // Act
        var model = builder.Build();

        // Assert
        model.Entities[0].Properties[0].IsRequired.Should().BeFalse();
    }
}
