using FluentAssertions;
using System.Text.Json;
using NewtonsoftSerial = Newtonsoft.Json;

namespace FluentJson.Tests.Integration;

public class GlobalSettingsTests
{
    public class UserProfile
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
    }

    [Fact]
    public void Newtonsoft_Should_Respect_SnakeCase_Convention()
    {
        // Arrange
        var builder = new NewtonsoftJson.JsonModelBuilder();
        builder.UseSnakeCaseNamingConvention(); // Global Config
        NewtonsoftSerial.JsonSerializerSettings settings = builder.Build();

        var user = new UserProfile { FirstName = "Architect", LastName = "Code" };

        // Act
        string json = NewtonsoftSerial.JsonConvert.SerializeObject(user, settings);

        // Assert
        json.Should().Contain("\"first_name\":\"Architect\"");
        json.Should().Contain("\"last_name\":\"Code\"");
    }

    [Fact]
    public void SystemTextJson_Should_Respect_SnakeCase_Convention()
    {
        // Arrange
        var builder = new SystemTextJson.JsonModelBuilder();
        builder.UseSnakeCaseNamingConvention(); // Global Config
        JsonSerializerOptions options = builder.Build();

        var user = new UserProfile { FirstName = "Architect", LastName = "Code" };

        // Act
        string json = JsonSerializer.Serialize(user, options);

        // Assert
        json.Should().Contain("\"first_name\":\"Architect\"");
        json.Should().Contain("\"last_name\":\"Code\"");
    }
}
