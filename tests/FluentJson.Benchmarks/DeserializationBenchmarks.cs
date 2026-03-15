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

[MemoryDiagnoser]
[ShortRunJob]
public class DeserializationBenchmarks
{
    private JsonSerializerOptions _vanillaStjOptions = null!;
    private JsonSerializerOptions _fluentStjOptions = null!;
    private JsonSerializerSettings _vanillaNwSettings = null!;
    private JsonSerializerSettings _fluentNwSettings = null!;
    private string _jsonPayload = null!;

    [GlobalSetup]
    public void Setup()
    {
        var builder = new JsonModelBuilder();
        builder.ApplyConfiguration(new StandardProductConfiguration());
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
}
