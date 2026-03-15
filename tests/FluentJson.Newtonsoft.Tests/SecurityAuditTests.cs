using System;
using Newtonsoft.Json;
using FluentAssertions;
using FluentJson.Core.Builder;
using Xunit;

namespace FluentJson.Newtonsoft.Tests;

public class SecurityAuditTests
{
    public abstract class Animal { public string Name { get; set; } = ""; }
    public class Dog : Animal { public string Breed { get; set; } = ""; }
    public class MaliciousAnimal : Animal { public string ExecuteCommand { get; set; } = ""; }

    [Fact]
    public void PolymorphicInjection_ShouldFailFast_WhenUnmappedTypeIsRequested()
    {
        var builder = new JsonModelBuilder();
        builder.Entity<Animal>()
            .HasDiscriminator("type")
            .HasDerivedType<Dog>("dog");

        var settings = new JsonSerializerSettings().AddFluentJson(builder.Build());

        var maliciousJson = """{"type": "MaliciousAnimal", "Name": "Evil", "ExecuteCommand": "rm -rf"}""";

        Action act = () => JsonConvert.DeserializeObject<Animal>(maliciousJson, settings);

        var ex = act.Should().Throw<Exception>().And;
        // The exception should mention the unmapped type without leaking internal info
        ex.Message.Should().Contain("MaliciousAnimal").And.NotContain("Newtonsoft.Json");
    }

    public class Node { public Node? Next { get; set; } }

    [Fact]
    public void DoS_MaxDepth_ShouldThrow_InsteadOfStackOverflow()
    {
        var builder = new JsonModelBuilder();
        builder.Entity<Node>().Property(x => x.Next);
        var settings = new JsonSerializerSettings().AddFluentJson(builder.Build());

        string json = "{}";
        for (int i = 0; i < 1000; i++)
        {
            json = $$"""{"Next": {{json}}}""";
        }

        Action act = () => JsonConvert.DeserializeObject<Node>(json, settings);

        act.Should().Throw<JsonReaderException>();
    }

    public class UserProfile
    {
        public string Username { get; set; } = "";
        
        private bool _isAdmin = false;
        // Also add auto-property with private setter just to be thorough
        public bool IsSuperUser { get; private set; }
        
        public bool IsAdmin => _isAdmin;
        public bool GetIsAdminField() => _isAdmin;
    }

    [Fact]
    public void Overposting_ShouldNotBind_UnmappedPrivateFields()
    {
        var builder = new JsonModelBuilder();
        builder.Entity<UserProfile>().Property(x => x.Username);
        
        var settings = new JsonSerializerSettings().AddFluentJson(builder.Build());

        var json = """
        { 
            "Username": "hacker", 
            "_isAdmin": true, 
            "IsAdmin": true,
            "IsSuperUser": true,
            "<IsSuperUser>k__BackingField": true
        }
        """;

        var profile = JsonConvert.DeserializeObject<UserProfile>(json, settings);

        profile.Should().NotBeNull();
        profile!.Username.Should().Be("hacker");
        profile.GetIsAdminField().Should().BeFalse(); // Must NOT be modified!
        profile.IsSuperUser.Should().BeFalse(); // Must NOT be modified!
    }

    [Fact]
    public void Exception_ShouldNotLeak_InternalPathsOrStackTraceInMessage()
    {
        var builder = new JsonModelBuilder();
        builder.Entity<Animal>()
            .HasDiscriminator("type")
            .HasDerivedType<Dog>("dog");

        var settings = new JsonSerializerSettings().AddFluentJson(builder.Build());
        var maliciousJson = """{"type": "MaliciousAnimal", "Name": "Evil"}""";

        Action act = () => JsonConvert.DeserializeObject<Animal>(maliciousJson, settings);

        var ex = act.Should().Throw<Exception>().And;
        
        ex.Message.Should().NotContain(":\\").And.NotContain("/");
    }
}
