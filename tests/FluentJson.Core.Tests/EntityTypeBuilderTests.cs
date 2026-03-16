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

    [Fact]
    public void HasDiscriminator_Should_SetDiscriminatorPropertyName()
    {
        var builder = new EntityTypeBuilder<Customer>();
        var resultBuilder = builder.HasDiscriminator("type");

        var entity = ((IEntityTypeBuilder)builder).Build();

        resultBuilder.Should().BeSameAs(builder);
        entity.DiscriminatorPropertyName.Should().Be("type");
    }

    [Fact]
    public void HasDiscriminator_Should_ThrowArgumentException_WhenNameIsNullOrWhitespace()
    {
        var builder = new EntityTypeBuilder<Customer>();

        Action act = () => builder.HasDiscriminator("");
        var ex = act.Should().Throw<ArgumentException>().WithParameterName("propertyName").And;
        ex.Message.Should().Contain("Discriminator property name cannot be null or empty.");

        Action act2 = () => builder.HasDiscriminator(null!);
        act2.Should().Throw<ArgumentException>().WithParameterName("propertyName");
    }

    private class BasePerson { }
    private class VipPerson : BasePerson { }

    [Fact]
    public void HasDerivedType_Should_RegisterDerivedType_And_ReturnSameBuilder()
    {
        var builder = new EntityTypeBuilder<BasePerson>();

        var resultBuilder = builder.HasDerivedType<VipPerson>("vip");
        var entity = ((IEntityTypeBuilder)builder).Build();

        resultBuilder.Should().BeSameAs(builder);
        entity.DerivedTypes.Should().ContainKey("vip").WhoseValue.Should().Be(typeof(VipPerson));
    }

    [Fact]
    public void HasDerivedType_Should_ThrowArgumentNullException_WhenValueIsNull()
    {
        var builder = new EntityTypeBuilder<BasePerson>();

        Action act = () => builder.HasDerivedType<VipPerson>(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("value");
    }

    [Fact]
    public void HasDerivedType_Should_ThrowConfigurationException_WhenValueAlreadyMapped()
    {
        var builder = new EntityTypeBuilder<BasePerson>();
        builder.HasDerivedType<VipPerson>("vip");

        Action act = () => builder.HasDerivedType<VipPerson>("vip");
        act.Should().Throw<FluentJsonConfigurationException>()
           .WithMessage($"The discriminator value 'vip' has already been mapped for base type {typeof(BasePerson)}.");
    }

    [Fact]
    public void PreserveReferences_Should_SetPreserveReferencesFlag_ToTrueByDefault()
    {
        var builder = new EntityTypeBuilder<Customer>();

        var resultBuilder = builder.PreserveReferences();
        var entity = ((IEntityTypeBuilder)builder).Build();

        resultBuilder.Should().BeSameAs(builder);
        entity.ShouldPreserveReferences.Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PreserveReferences_Should_RespectPassedValue(bool preserve)
    {
        var builder = new EntityTypeBuilder<Customer>();
        builder.PreserveReferences(preserve);

        var entity = ((IEntityTypeBuilder)builder).Build();
        entity.ShouldPreserveReferences.Should().Be(preserve);
    }

    private abstract class BaseClass { }

    [Fact]
    public void Build_Should_CreateThrowingFactory_ForAbstractTypes()
    {
        var builder = new EntityTypeBuilder<BaseClass>();
        var entity = ((IEntityTypeBuilder)builder).Build();

        entity.EntityType.Should().Be(typeof(BaseClass));
        entity.ConstructorFactory.Should().NotBeNull();

        Action act = () => entity.ConstructorFactory();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage($"Cannot instantiate abstract class {typeof(BaseClass)}");
    }

    private class FieldModel
    {
#pragma warning disable 0169
        private readonly int _privateField;
#pragma warning restore 0169
        public int PublicProperty { get; set; }
        public void SomeMethod() { }
    }

    [Fact]
    public void PropertyString_Should_MapPrivateFieldsAndProperties()
    {
        var builder = new EntityTypeBuilder<FieldModel>();

        var fieldBuilder = builder.Property<int>("_privateField");
        var propBuilder = builder.Property<int>("PublicProperty");

        fieldBuilder.Should().NotBeNull();
        propBuilder.Should().NotBeNull();
    }

    [Fact]
    public void PropertyString_Should_ThrowArgumentException_WhenNameIsNullOrWhitespace()
    {
        var builder = new EntityTypeBuilder<FieldModel>();
        Action act = () => builder.Property<int>("");
        act.Should().Throw<ArgumentException>()
           .WithParameterName("propertyName")
           .WithMessage("*Property name cannot be null or empty.*");
    }

    [Fact]
    public void PropertyString_Should_ThrowConfigurationException_WhenMemberNotFound()
    {
        var builder = new EntityTypeBuilder<FieldModel>();
        Action act = () => builder.Property<int>("DoesNotExist");
        act.Should().Throw<FluentJsonConfigurationException>()
           .WithMessage($"Member 'DoesNotExist' not found on type {typeof(FieldModel)}.");
    }

    [Fact]
    public void Build_Should_ThrowConfigurationException_WhenMappedMemberIsNotPropertyOrField()
    {
        var builder = new EntityTypeBuilder<FieldModel>();
        // Using reflection to bypass builder mapping logic and force method info into Dictionary
        var dictField = typeof(EntityTypeBuilder<FieldModel>).GetField("_propertyBuilders", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var dict = (System.Collections.Generic.Dictionary<System.Reflection.MemberInfo, object>)dictField.GetValue(builder)!;

        var methodInfo = typeof(FieldModel).GetMethod(nameof(FieldModel.SomeMethod))!;
        dict[methodInfo] = new PropertyBuilder<int>();

        Action act = () => ((IEntityTypeBuilder)builder).Build();
        act.Should().Throw<FluentJsonConfigurationException>()
           .WithMessage("Member 'SomeMethod' is neither a property nor a field.");
    }
}
