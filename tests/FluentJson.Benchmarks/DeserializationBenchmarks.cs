using System;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using FluentJson.Core.Builder;
using FluentJson.Newtonsoft;
using FluentJson.SystemTextJson;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace FluentJson.Benchmarks;

public class StandardProduct
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class StandardProductConfiguration : IJsonTypeConfiguration<StandardProduct>
{
    public void Configure(EntityTypeBuilder<StandardProduct> builder)
    {
        builder.Property(x => x.Id).HasName("id");
        builder.Property(x => x.Name).HasName("name");
        builder.Property(x => x.Price).HasName("price");
    }
}

public abstract class BasePayment
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
}

public class CardPayment : BasePayment
{
    public string CardNumber { get; set; } = string.Empty;
}

public class BasePaymentConfiguration : IJsonTypeConfiguration<BasePayment>
{
    public void Configure(EntityTypeBuilder<BasePayment> builder)
    {
        builder.Property(x => x.Id).HasName("id");
        builder.Property(x => x.Amount).HasName("amt");
        builder.HasDiscriminator("type")
               .HasDerivedType<CardPayment>("card");
    }
}

public class CardPaymentConfiguration : IJsonTypeConfiguration<CardPayment>
{
    public void Configure(EntityTypeBuilder<CardPayment> builder)
    {
        builder.Property(x => x.CardNumber).HasName("cardNum");
    }
}

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

public class UserConfiguration : IJsonTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(x => x.Id).HasName("id").HasConversion<Guid>(id => id.Value, scalar => new UserId(scalar));
        builder.Property(x => x.Username).HasName("userName");
    }
}

[MemoryDiagnoser]
[ShortRunJob]
public class DeserializationBenchmarks
{
    private JsonSerializerOptions _vanillaStjOptions = null!;
    private JsonSerializerOptions _fluentStjOptions = null!;
    private JsonSerializerSettings _vanillaNwSettings = null!;
    private JsonSerializerSettings _fluentNwSettings = null!;
    private string _jsonPayload = null!;
    private string _polyPayload = null!;
    private string _flatPayload = null!;

    [GlobalSetup]
    public void Setup()
    {
        var builder = new JsonModelBuilder();
        builder.ApplyConfiguration(new StandardProductConfiguration());
        builder.ApplyConfiguration(new BasePaymentConfiguration());
        builder.ApplyConfiguration(new CardPaymentConfiguration());
        builder.ApplyConfiguration(new UserConfiguration());
        var model = builder.Build();

        _vanillaStjOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _fluentStjOptions = new JsonSerializerOptions();
        _fluentStjOptions.AddFluentJson(model);

        _vanillaNwSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        _fluentNwSettings = new JsonSerializerSettings();
        _fluentNwSettings.AddFluentJson(model);

        _jsonPayload = $$"""
        {
            "id": "{{Guid.NewGuid()}}",
            "name": "Standard Widget",
            "price": 9.99
        }
        """;

        _polyPayload = $$"""
        {
            "type": "card",
            "id": "{{Guid.NewGuid()}}",
            "amt": 49.99,
            "cardNum": "1234-5678-9012-3456"
        }
        """;

        _flatPayload = $$"""
        {
            "id": "{{Guid.NewGuid()}}",
            "userName": "benchmark_user"
        }
        """;
    }

    [Benchmark(Baseline = true)]
    public StandardProduct SystemTextJson_Native()
    {
        return System.Text.Json.JsonSerializer.Deserialize<StandardProduct>(_jsonPayload, _vanillaStjOptions)!;
    }

    [Benchmark]
    public StandardProduct SystemTextJson_Fluent()
    {
        return System.Text.Json.JsonSerializer.Deserialize<StandardProduct>(_jsonPayload, _fluentStjOptions)!;
    }

    [Benchmark]
    public StandardProduct Newtonsoft_Native()
    {
        return global::Newtonsoft.Json.JsonConvert.DeserializeObject<StandardProduct>(_jsonPayload, _vanillaNwSettings)!;
    }

    [Benchmark]
    public StandardProduct Newtonsoft_Fluent()
    {
        return global::Newtonsoft.Json.JsonConvert.DeserializeObject<StandardProduct>(_jsonPayload, _fluentNwSettings)!;
    }

    [Benchmark]
    public BasePayment Poly_SystemTextJson_Fluent()
    {
        return System.Text.Json.JsonSerializer.Deserialize<BasePayment>(_polyPayload, _fluentStjOptions)!;
    }

    [Benchmark]
    public BasePayment Poly_Newtonsoft_Fluent()
    {
        return global::Newtonsoft.Json.JsonConvert.DeserializeObject<BasePayment>(_polyPayload, _fluentNwSettings)!;
    }

    [Benchmark]
    public User Flat_SystemTextJson_Fluent()
    {
        return System.Text.Json.JsonSerializer.Deserialize<User>(_flatPayload, _fluentStjOptions)!;
    }

    [Benchmark]
    public User Flat_Newtonsoft_Fluent()
    {
        return global::Newtonsoft.Json.JsonConvert.DeserializeObject<User>(_flatPayload, _fluentNwSettings)!;
    }
}
