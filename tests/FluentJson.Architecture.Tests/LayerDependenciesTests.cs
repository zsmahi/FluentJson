using NetArchTest.Rules;

using Xunit;

namespace FluentJson.Architecture.Tests;

public class LayerDependenciesTests
{
    [Fact]
    public void Core_ShouldNot_HaveDependenciesOn_Infrastructure_Or_ExternalSerializers()
    {
        // Arrange
        var coreAssembly = System.Reflection.Assembly.Load("FluentJson.Core");

        // Act
        var result = Types.InAssembly(coreAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "FluentJson.SystemTextJson",
                "FluentJson.Newtonsoft",
                "System.Text.Json",
                "Newtonsoft.Json"
            )
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, "FluentJson.Core should not depend on infrastructure or external serializers.");
    }
}
