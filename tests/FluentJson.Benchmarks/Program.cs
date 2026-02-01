using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;
using FluentJson.Benchmarks;

var config = ManualConfig.Create(DefaultConfig.Instance)
             .AddExporter(MarkdownExporter.GitHub);

BenchmarkRunner.Run<SerializationBenchmarks>(config);
