using FluentAssertions;
using FluentJson.Abstractions;
using FluentJson.Builders;
using System.Text.Json;

namespace FluentJson.Tests.Integration;

public class AdvancedTypeTests
{
    public enum Status { Active, Inactive }

    public abstract class Account { public Status State { get; set; } }
    public class PremiumAccount : Account { public string Perks { get; set; } = ""; }

    public class AccountConfiguration : IJsonEntityTypeConfiguration<Account>
    {
        public void Configure(JsonEntityTypeBuilder<Account> builder)
        {
            builder.HasDiscriminator(x => x.State)
                   .HasSubType<PremiumAccount>(Status.Active);
        }
    }

    [Fact]
    public void SystemTextJson_Should_Handle_Enum_Discriminators()
    {
        var builder = new SystemTextJson.JsonModelBuilder();
        builder.ApplyConfiguration(new AccountConfiguration());
        JsonSerializerOptions options = builder.Build();

        var account = new PremiumAccount { State = Status.Active, Perks = "Unlimited" };
        string json = JsonSerializer.Serialize<Account>(account, options);

        json.Should().Contain("\"State\":\"Active\""); // Or 0 depending on naming policy

        Account? deserialized = JsonSerializer.Deserialize<Account>(json, options);
        deserialized.Should().BeOfType<PremiumAccount>();
    }
}
