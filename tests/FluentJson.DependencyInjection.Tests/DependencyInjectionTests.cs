using System;
using System.Linq;
using FluentAssertions;
using FluentJson.Core.Builder;
using FluentJson.Core.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FluentJson.DependencyInjection.Tests;

public class DependencyInjectionTests
{
    private class DummyEntity { public Guid Id { get; set; } }

    [Fact]
    public void AddFluentJson_ShouldThrowArgumentNullException_WhenServicesIsNull()
    {
        IServiceCollection services = null!;
        bool configureCalled = false;
        
        Action act = () => services.AddFluentJson(b => { configureCalled = true; });
        
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
        configureCalled.Should().BeFalse();
    }

    [Fact]
    public void AddFluentJson_ShouldThrowArgumentNullException_WhenConfigureIsNull()
    {
        var services = new ServiceCollection();
        Action act = () => services.AddFluentJson(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }

    [Fact]
    public void AddFluentJson_ShouldRegisterJsonModelAsSingleton()
    {
        var services = new ServiceCollection();
        
        services.AddFluentJson(builder => 
        {
            builder.Entity<DummyEntity>();
        });

        // Add Microsoft.Extensions.DependencyInjection handles BuildServiceProvider
        var serviceProvider = services.BuildServiceProvider();
        
        // Assert resolution
        var model1 = serviceProvider.GetService<IJsonModel>();
        var model2 = serviceProvider.GetService<IJsonModel>();

        model1.Should().NotBeNull();
        model1.Should().BeSameAs(model2); // Singleton

        // Assert content
        model1!.Entities.Should().HaveCount(1);
        model1.Entities.First().EntityType.Should().Be(typeof(DummyEntity));
    }
}
