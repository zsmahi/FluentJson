using FluentAssertions;
using FluentJson.Builders;
using FluentJson.Exceptions;
using FluentJson.Internal;

namespace FluentJson.Tests.Core;

public class ValidationRulesTests
{
    private class BadEntity
    {
        public string PropA { get; set; } = "";
        public string PropB { get; set; } = "";
    }

    private class PolymorphicEntity { }

    [Fact]
    public void Should_Throw_When_Duplicate_Json_Names_Are_Configured()
    {
        var builder = new JsonEntityTypeBuilder<BadEntity>();

        builder.Property(x => x.PropA).HasPropertyName("same_id");
        builder.Property(x => x.PropB).HasPropertyName("same_id");

        Action act = () =>
        {
            Definitions.JsonEntityDefinition def = ((IJsonEntityTypeBuilderAccessor)builder).Definition;
            ModelValidator.ValidateDefinition(typeof(BadEntity), def);
        };

        act.Should().Throw<FluentJsonValidationException>()
           .WithMessage("*Duplicate JSON property names*");
    }

    [Fact]
    public void Should_Throw_When_Polymorphism_Has_No_Subtypes()
    {
        var builder = new JsonEntityTypeBuilder<PolymorphicEntity>();

        builder.HasDiscriminator("type");

        Action act = () =>
        {
            Definitions.JsonEntityDefinition def = ((IJsonEntityTypeBuilderAccessor)builder).Definition;
            ModelValidator.ValidateDefinition(typeof(PolymorphicEntity), def);
        };

        act.Should().Throw<FluentJsonValidationException>()
           .WithMessage("*no subtypes*");
    }
}
