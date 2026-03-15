using System;
using FluentAssertions;
using FluentJson.Core.Builder;
using FluentJson.Newtonsoft;
using FluentJson.SystemTextJson;
using Newtonsoft.Json;
using Xunit;

namespace FluentJson.FunctionalTests;

public class FlatteningTests
{
    public struct UserId
    {
        public Guid Value { get; }
        public UserId(Guid value) { Value = value; }
    }

    public class User
    {
        public UserId Id { get; set; }
        public string Username { get; set; } = string.Empty;
    }

    [Fact]
    public void DualEngine_Should_Flatten_StronglyTypedId_ToScalarAndBack()
    {
        var expectedGuid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var user = new User { Id = new UserId(expectedGuid), Username = "admin" };

        var builder = new JsonModelBuilder();
        builder.Entity<User>().Property(x => x.Id).HasName("id").HasConversion<Guid>(id => id.Value, scalar => new UserId(scalar));
        builder.Entity<User>().Property(x => x.Username).HasName("userName");

        var model = builder.Build();

        // System.Text.Json Test
        var stjOptions = new System.Text.Json.JsonSerializerOptions();
        stjOptions.AddFluentJson(model);

        string stjJson = System.Text.Json.JsonSerializer.Serialize(user, stjOptions);
        
        // Assert JSON is flat
        stjJson.Should().Contain($"\"id\":\"{expectedGuid}\"");
        stjJson.Should().NotContain("Value");

        var stjResult = System.Text.Json.JsonSerializer.Deserialize<User>(stjJson, stjOptions);
        stjResult.Should().NotBeNull();
        stjResult!.Id.Value.Should().Be(expectedGuid);
        stjResult.Username.Should().Be("admin");


        // Newtonsoft.Json Test
        var nwSettings = new JsonSerializerSettings();
        nwSettings.AddFluentJson(model);

        string nwJson = JsonConvert.SerializeObject(user, nwSettings);

        // Assert JSON is flat
        nwJson.Should().Contain($"\"id\":\"{expectedGuid}\"");
        nwJson.Should().NotContain("Value");

        var nwResult = JsonConvert.DeserializeObject<User>(nwJson, nwSettings);
        nwResult.Should().NotBeNull();
        nwResult!.Id.Value.Should().Be(expectedGuid);
        nwResult.Username.Should().Be("admin");
    }
}
