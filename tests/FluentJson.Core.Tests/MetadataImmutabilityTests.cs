using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using FluentAssertions;

using FluentJson.Core.Builder;
using FluentJson.Core.Metadata;

using Xunit;

namespace FluentJson.Core.Tests;

public class MetadataImmutabilityTests
{
    private class Dummy
    {
        public int Id { get; set; }
    }

    [Fact]
    public void JsonModelBuilder_Build_Should_PerformSnapshot_AndBeDetached()
    {
        // Arrange
        var builder = new JsonModelBuilder();
        builder.Entity<Dummy>().Property(x => x.Id).HasName("id1");

        // Act
        var model = builder.Build();

        // Attempt to mutate the builder after building
        builder.Entity<Dummy>().Property(x => x.Id).HasName("id2");
        var newModel = builder.Build();

        // Assert
        model.Should().NotBeSameAs(newModel);

        var originalEntity = model.Entities.Single();
        var originalProperty = originalEntity.Properties.Single();
        originalProperty.Name.Should().Be("id1");

        var newEntity = newModel.Entities.Single();
        var newProperty = newEntity.Properties.Single();
        newProperty.Name.Should().Be("id2");
    }

    [Fact]
    public void MetadataInterfaces_Should_HaveNoPublicOrInternalSetters()
    {
        var interfaces = new[] { typeof(IJsonModel), typeof(IJsonEntity), typeof(IJsonProperty) };

        foreach (var type in interfaces)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var property in properties)
            {
                var setMethod = property.GetSetMethod(nonPublic: true);
                setMethod.Should().BeNull($"Property {property.Name} on {type.Name} should not have a setter.");
            }
        }
    }

    [Fact]
    public void MetadataImplementations_Should_BeImmutable()
    {
        var builder = new JsonModelBuilder();
        builder.Entity<Dummy>().Property(x => x.Id).HasName("id");
        var model = builder.Build();

        // Attempt to cast and mutate collections
        var entities = model.Entities;
        Action mutateEntities = () => ((IList<IJsonEntity>)entities).Add(null!);
        mutateEntities.Should().Throw<NotSupportedException>();

        var properties = entities.Single().Properties;
        Action mutateProperties = () => ((IList<IJsonProperty>)properties).Add(null!);
        mutateProperties.Should().Throw<NotSupportedException>();
    }
}
