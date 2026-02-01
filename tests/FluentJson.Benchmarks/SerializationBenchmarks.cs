using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using System.Text.Json;
using FluentJson.Abstractions;
using FluentJson.Builders;
using FluentJson.SystemTextJson;
using Newtonsoft.Json;

namespace FluentJson.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SerializationBenchmarks
{
    private static readonly SampleModel _data = new() { Id = 1, Name = "Architect", Value = 42.5 };

    private JsonSerializerOptions _stjOptions;
    private JsonSerializerSettings _newtonsoftSettings;
    private JsonSerializerOptions _nativeStjOptions;

    public class SampleModelConfiguration : IJsonEntityTypeConfiguration<SampleModel>
    {
        public void Configure(JsonEntityTypeBuilder<SampleModel> builder)
        {
            builder.Property(x => x.Name).HasPropertyName("display_name");
            builder.Property(x => x.Value).HasPropertyName("score");
        }
    }

    [GlobalSetup]
    public void Setup()
    {
        // 1. Fluent STJ Configuration
        var stjBuilder = new JsonModelBuilder();
        stjBuilder.ApplyConfiguration(new SampleModelConfiguration());
        _stjOptions = stjBuilder.Build();

        // 2. Native STJ Options
        _nativeStjOptions = new JsonSerializerOptions();

        // 3. Fluent Newtonsoft Configuration
        var nwBuilder = new FluentJson.NewtonsoftJson.JsonModelBuilder();
        nwBuilder.ApplyConfiguration(new SampleModelConfiguration());
        _newtonsoftSettings = nwBuilder.Build();
    }

    [Benchmark(Baseline = true)]
    public string Native_STJ()
    {
        return System.Text.Json.JsonSerializer.Serialize(_data, _nativeStjOptions);
    }

    [Benchmark]
    public string Fluent_STJ()
    {
        return System.Text.Json.JsonSerializer.Serialize(_data, _stjOptions);
    }

    [Benchmark]
    public string Fluent_Newtonsoft()
    {
        return JsonConvert.SerializeObject(_data, _newtonsoftSettings);
    }

    [Benchmark]
    public string Native_Newtonsoft()
    {
        return JsonConvert.SerializeObject(_data);
    }
}

public class SampleModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public double Value { get; set; }
}
