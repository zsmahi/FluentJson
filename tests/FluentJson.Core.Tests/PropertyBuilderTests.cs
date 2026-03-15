using System;
using FluentAssertions;
using FluentJson.Core.Builder;
using FluentJson.Core.Metadata;
using Xunit;

namespace FluentJson.Core.Tests;

public class PropertyBuilderTests
{
    private record Product(string Name);

    [Fact]
    public void HasName_Should_UpdateMetadataJsonName()
    {
        // Arrange
        var builder = new PropertyBuilder<string>();
        var propertyInfo = typeof(Product).GetProperty(nameof(Product.Name))!;

        // Act
        builder.HasName("product_name");
        var property = ((IPropertyBuilder)builder).Build(propertyInfo, "Name");

        // Assert
        property.Name.Should().Be("product_name");
    }

    [Fact]
    public void IsRequired_Should_UpdateMetadataFlag()
    {
        // Arrange
        var builder = new PropertyBuilder<string>();
        var propertyInfo = typeof(Product).GetProperty(nameof(Product.Name))!;

        // Act
        builder.IsRequired(true);
        var property = ((IPropertyBuilder)builder).Build(propertyInfo, "Name");

        // Assert
        property.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void IsRequired_Should_DefaultToTrue_WhenNoArgumentPassed()
    {
        // Arrange
        var builder = new PropertyBuilder<string>();
        var propertyInfo = typeof(Product).GetProperty(nameof(Product.Name))!;

        // Act
        builder.IsRequired();
        var property = ((IPropertyBuilder)builder).Build(propertyInfo, "Name");

        // Assert
        property.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void HasName_Should_ThrowArgumentException_WhenNameIsNullOrWhiteSpace()
    {
        // Arrange
        var builder = new PropertyBuilder<string>();

        // Act & Assert
        Action act1 = () => builder.HasName(null!);
        var exception1 = act1.Should().Throw<ArgumentException>().And;
        exception1.Message.Should().Contain("Json name cannot be null or whitespace.");
        exception1.ParamName.Should().Be("jsonName");

        Action act2 = () => builder.HasName("");
        var exception2 = act2.Should().Throw<ArgumentException>().And;
        exception2.Message.Should().Contain("Json name cannot be null or whitespace.");
        exception2.ParamName.Should().Be("jsonName");

        Action act3 = () => builder.HasName("   ");
        var exception3 = act3.Should().Throw<ArgumentException>().And;
        exception3.Message.Should().Contain("Json name cannot be null or whitespace.");
        exception3.ParamName.Should().Be("jsonName");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Ignore_Should_SetIsIgnoredCorrectly(bool ignored)
    {
        var builder = new PropertyBuilder<int>();
        builder.Ignore(ignored);

        var propertyInfo = typeof(Product).GetProperty(nameof(Product.Name))!;
        var result = ((IPropertyBuilder)builder).Build(propertyInfo, "Name");

        result.IsIgnored.Should().Be(ignored);
    }

    [Fact]
    public void Ignore_Should_DefaultToTrue_WhenNoArgumentPassed()
    {
        var builder = new PropertyBuilder<int>();
        builder.Ignore();

        var propertyInfo = typeof(Product).GetProperty(nameof(Product.Name))!;
        var result = ((IPropertyBuilder)builder).Build(propertyInfo, "Name");

        result.IsIgnored.Should().BeTrue();
    }

    [Fact]
    public void BuilderMethods_Should_ReturnSameBuilderInstanceForChaining()
    {
        // Arrange
        var builder = new PropertyBuilder<string>();

        // Act
        var result1 = builder.HasName("test");
        var result2 = builder.IsRequired();
        var result3 = builder.Ignore();

        // Assert
        result1.Should().BeSameAs(builder);
        result2.Should().BeSameAs(builder);
        result3.Should().BeSameAs(builder);
    }
}
