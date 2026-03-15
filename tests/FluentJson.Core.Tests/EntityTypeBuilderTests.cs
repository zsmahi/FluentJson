using System;
using FluentAssertions;
using FluentJson.Core.Builder;
using FluentJson.Core.Exceptions;
using Xunit;

namespace FluentJson.Core.Tests;

public class EntityTypeBuilderTests
{
    private record Product(int Id, string Title)
    {
        private Product() : this(0, string.Empty) { }
    }

    [Fact]
    public void Property_Should_ReturnNewPropertyBuilder_ForGivenProperty()
    {
        // Arrange
        var entityBuilder = new EntityTypeBuilder<Product>();

        // Act
        var propertyBuilder = entityBuilder.Property(x => x.Title);

        // Assert
        propertyBuilder.Should().NotBeNull();
        propertyBuilder.Should().BeOfType<PropertyBuilder<string>>();
    }

    [Fact]
    public void Property_Should_ReturnSameInstance_WhenCalledMultipleTimesForSameProperty()
    {
        // Arrange
        var entityBuilder = new EntityTypeBuilder<Product>();

        // Act
        var propertyBuilder1 = entityBuilder.Property(x => x.Title);
        var propertyBuilder2 = entityBuilder.Property(x => x.Title);

        // Assert
        propertyBuilder1.Should().BeSameAs(propertyBuilder2);
    }

    private record Customer(Guid Id, string Name)
    {
        private Customer() : this(Guid.Empty, string.Empty) { }
    }

    [Fact]
    public void Property_Should_ReturnSameInstance_EvenIf_ParameterNameDiffers()
    {
        // Arrange
        var builder = new JsonModelBuilder().Entity<Customer>();

        // Act
        var call1 = builder.Property(x => x.Name);
        var call2 = builder.Property(y => y.Name);

        // Assert
        call1.Should().BeSameAs(call2);
    }

    [Fact]
    public void Property_ShouldThrowFluentJsonExpressionException_WhenExpressionIsNotMemberAccess()
    {
        // Arrange
        var entityBuilder = new EntityTypeBuilder<Product>();

        // Act
        Action act = () => entityBuilder.Property(x => x.Title.ToUpper());

        // Assert
        act.Should().Throw<FluentJsonExpressionException>()
           .WithMessage("Expression must be a simple member access (e.g., x => x.Name). Nested properties or methods are not supported.");
    }

    [Fact]
    public void Property_ShouldThrowFluentJsonExpressionException_WhenExpressionIsNested()
    {
        // Arrange
        var entityBuilder = new EntityTypeBuilder<Product>();

        // Act
        Action act = () => entityBuilder.Property(x => x.Title.Length);

        // Assert
        act.Should().Throw<FluentJsonExpressionException>()
           .WithMessage("Expression must be a simple member access (e.g., x => x.Name). Nested properties or methods are not supported.");
    }

    [Fact]
    public void Property_Should_ReturnPropertyBuilder_WhenExpressionIsUnaryBoxing()
    {
        // Arrange
        var entityBuilder = new EntityTypeBuilder<Product>();

        // Act
        // Explicitly cast to object to generate a UnaryExpression (Convert) in the expression tree
        var propertyBuilder = entityBuilder.Property<object>(x => (object)x.Id);

        // Assert
        propertyBuilder.Should().NotBeNull();
        propertyBuilder.Should().BeOfType<PropertyBuilder<object>>();
    }

    private class Account
    {
        public Guid Id { get; private set; }

        private Account()
        {
        }

        public Account(Guid id)
        {
            Id = id;
        }
    }

    private class Order
    {
        public Guid Id { get; }

        public Order(Guid id)
        {
            Id = id;
        }
    }

    [Fact]
    public void Build_Should_CreateCompiledFactory_When_PrivateConstructorExists()
    {
        // Arrange
        var builder = new EntityTypeBuilder<Account>();
        // Using explicit interface cast since Build is an explicit interface method
        var entityBuilder = (IEntityTypeBuilder)builder;

        // Act
        var entity = entityBuilder.Build();

        // Assert
        entity.ConstructorFactory.Should().NotBeNull();
        var instance = entity.ConstructorFactory();
        instance.Should().NotBeNull();
        instance.Should().BeOfType<Account>();
    }

    [Fact]
    public void Build_Should_ThrowException_When_NoParameterlessConstructorExists()
    {
        // Arrange
        var builder = new EntityTypeBuilder<Order>();
        var entityBuilder = (IEntityTypeBuilder)builder;

        // Act
        Action act = () => entityBuilder.Build();

        // Assert
        act.Should().Throw<FluentJsonConfigurationException>()
           .WithMessage($"No parameterless constructor found for type {typeof(Order)}. A private parameterless constructor is required for DDD invariant safety.");
    }

    [Fact]
    public void Ignore_Should_MarkPropertyAsIgnored()
    {
        // Arrange
        var builder = new EntityTypeBuilder<Customer>();
        
        // Act
        var resultBuilder = builder.Ignore(x => x.Name);
        var entity = ((IEntityTypeBuilder)builder).Build();
        
        // Assert
        resultBuilder.Should().BeSameAs(builder);
        var property = entity.Properties.Should().ContainSingle().Subject;
        property.MemberInfo.Name.Should().Be(nameof(Customer.Name));
        property.IsIgnored.Should().BeTrue();
    }

    [Fact]
    public void Ignore_Should_HandleValueTypeProperties_WithUnaryBoxingExpression()
    {
        var builder = new EntityTypeBuilder<Customer>();
        
        var resultBuilder = builder.Ignore(x => x.Id);
        var entity = ((IEntityTypeBuilder)builder).Build();
        
        resultBuilder.Should().BeSameAs(builder);
        var property = entity.Properties.Should().ContainSingle().Subject;
        property.MemberInfo.Name.Should().Be(nameof(Customer.Id));
        property.IsIgnored.Should().BeTrue();
    }

    [Fact]
    public void Ignore_Should_UpdateExistingPropertyBuilder_WhenAlreadyConfigured()
    {
        var builder = new EntityTypeBuilder<Customer>();
        
        builder.Property(x => x.Name).HasName("customer_name").IsRequired();
        builder.Ignore(x => x.Name);

        var entity = ((IEntityTypeBuilder)builder).Build();
        
        var property = entity.Properties.Should().ContainSingle().Subject;
        property.Name.Should().Be("customer_name");
        property.IsRequired.Should().BeTrue();
        property.IsIgnored.Should().BeTrue();
    }

    [Fact]
    public void Ignore_ShouldThrowFluentJsonExpressionException_WhenExpressionIsNotMemberAccess()
    {
        // Arrange
        var entityBuilder = new EntityTypeBuilder<Customer>();

        // Act
        Action act = () => entityBuilder.Ignore(x => x.Name.ToUpper());

        // Assert
        act.Should().Throw<FluentJsonExpressionException>()
           .WithMessage("Expression must be a simple member access (e.g., x => x.Name). Nested properties or methods are not supported.");
    }

    [Fact]
    public void Ignore_ShouldThrowFluentJsonExpressionException_WhenExpressionIsNested()
    {
        // Arrange
        var entityBuilder = new EntityTypeBuilder<Customer>();

        // Act
        Action act = () => entityBuilder.Ignore(x => x.Name.Length);

        // Assert
        act.Should().Throw<FluentJsonExpressionException>()
           .WithMessage("Expression must be a simple member access (e.g., x => x.Name). Nested properties or methods are not supported.");
    }
}
