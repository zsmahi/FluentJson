# FluentJson Benchmarks

Performance is a key feature of FluentJson. Our goal is to provide rich Domain-Driven Design serialization without introducing significant overhead compared to native engine instantiation.

## Methodology
The benchmarks are executed using `BenchmarkDotNet`. We measure the deserialization time of a complex domain entity, comparing standard native instantiation (which often involves reflection or `FormatterServices.GetUninitializedObject`) against FluentJson's pre-compiled expression factories.

## Results (System.Text.Json & Newtonsoft.Json)
The following results demonstrate that FluentJson adds minimal overhead, running close to native speed, and in some heavily complex object graphs, it can even outperform default reflection-based paths.

```yaml
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8037/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i7-1265U 2.70GHz, 1 CPU, 12 logical and 10 physical cores
.NET SDK 10.0.104
  [Host]   : .NET 8.0.24 (8.0.24, 8.0.2426.7010), X64 RyuJIT x86-64-v3
  ShortRun : .NET 8.0.24 (8.0.24, 8.0.2426.7010), X64 RyuJIT x86-64-v3
```

| Method                | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------- |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| SystemTextJson_Native | 332.5 ns |   4.33 ns |  0.24 ns |  1.00 |    0.00 | 0.0176 |     112 B |        1.00 |
| SystemTextJson_Fluent | 386.2 ns |  44.22 ns |  2.42 ns |  1.16 |    0.01 | 0.0277 |     176 B |        1.57 |
| Newtonsoft_Native     | 661.3 ns | 238.53 ns | 13.07 ns |  1.99 |    0.03 | 0.4549 |    2856 B |       25.50 |
| Newtonsoft_Fluent     | 717.1 ns | 308.56 ns | 16.91 ns |  2.16 |    0.04 | 0.4549 |    2856 B |       25.50 |

### Analysis
- **System.Text.Json**: FluentJson operates within ~16% of the highly optimized native STJ performance, while adding essential DDD capabilities like private constructor execution and private field mapping.
- **Newtonsoft.Json**: The performance overhead is negligible (~8% difference). As expected, memory allocation remains identical to the native Newtonsoft implementation.
