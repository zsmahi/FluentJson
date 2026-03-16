# FluentJson

[![Documentation](https://img.shields.io/badge/docs-Technical%20Bible-brightgreen.svg)](https://zsmahi.github.io/FluentJson/)

> **Decoupled, high-performance JSON serialization for .NET DDD models. 100% Mutation Tested.**

FluentJson allows true separation of Domain Models (DDD) from serialization concerns, bringing the beauty of the Builder Pattern to `System.Text.Json` and `Newtonsoft.Json`.

## ✨ Key Features (The "Big Five")
- **Zero-Attribute Serialization**: Keep your Domain pristine. Map fields and private constructors without `[JsonConstructor]` or `[JsonPropertyName]`.
- **Advanced Polymorphism**: Map derived types cleanly using Discriminators directly via the API.
- **Value Object Flattening**: Automatically unwrap complex Strongly-Typed IDs or convert types into scalars seamlessly.
- **Circular Reference Handling**: Safely serialize and deserialize aggregate roots with circular parent/child relationships.
- **OCP Configuration**: Scan assemblies to automatically discover configurations using the `IJsonTypeConfiguration<T>` interface.

## ❓ Why FluentJson?
Modern enterprise applications rely heavily on Domain-Driven Design (DDD). However, the moment you introduce JSON serialization, you are often forced to pollute your clean domain models with serialization attributes or expose internal state to public getters/setters. FluentJson solves this exact problem by externalizing the mapping rules into a completely decoupled, immutable configuration layer.

## 🚀 Quick Start
```csharp
// 1. Your completely clean Domain Entity
public class User
{
    private string _passwordHash;
    
    public Guid Id { get; }
    public string Name { get; }

    // Private constructor for invariant safety
    private User() { } 

    public User(Guid id, string name, string passwordHash)
    {
        Id = id;
        Name = name;
        _passwordHash = passwordHash;
    }
}

// 2. Configure FluentJson
var builder = new JsonModelBuilder();
builder.Entity<User>()
       .Property(u => u.Id).HasName("userId")
       .Property<string>("_passwordHash").HasName("passwordHash");

var model = builder.Build();

// 3. Plug into your favorite Engine!

// For System.Text.Json
var options = new JsonSerializerOptions().AddFluentJson(model);
var user = JsonSerializer.Deserialize<User>("...", options);

// For Newtonsoft.Json
var settings = new JsonSerializerSettings().AddFluentJson(model);
var user2 = JsonConvert.DeserializeObject<User>("...", settings);
```

## ⚡ Performance (Zero-Allocation Engine)
FluentJson uses compiled expressions to bypass reflection bottlenecks. Furthermore, the Polymorphic Engine acts specifically as a **Zero-Allocation Scanner**, analyzing JSON payloads without instantiating intermediary strings. Below are the benchmarks displaying our highly optimized pipeline against the native engines:

| Method                | Mean     | Ratio | Allocated |
|---------------------- |---------:|------:|----------:|
| SystemTextJson_Native | 332.5 ns |  1.00 |     112 B |
| SystemTextJson_Fluent | 465.5 ns |  1.40 |     176 B |
| Newtonsoft_Native     | 661.3 ns |  1.99 |    2856 B |
| Newtonsoft_Fluent     | 859.6 ns |  2.60 |    2856 B |

*See [docs/benchmarks.md](docs/benchmarks.md) for full details.*

## 🔒 Security by Design
FluentJson is engineered for strict enterprise environments.

- **Polymorphic Injection (RCE) mitigation**: Resolves types using a strictly isolated, immutable dictionary defined during the builder phase. Unregistered discriminators instantly fail the serialization pipeline.
- **Deep Nesting (DoS) protection**: The underlying engines are cleanly passed through to prevent stack overflows, honoring maximum depth settings.
- **Shadow Field Mapping Disabled**: By default, mapping unconfigured internal fields is disabled, requiring explicit configuration, effectively creating a Zero-Trust object lifecycle.

## 🛡️ Reliability Guarantee
FluentJson is strictly test-driven. The `FluentJson.Core` library currently holds a **100.00% Mutation Score** (verified via Stryker), ensuring every branching condition, mapping rule, and lambda compilation is rigorously validated and unbreakable. See our [Quality Report](docs/quality-report.md).

## 📚 Navigation & Technical Depth
Want to explore how it all works under the hood? Check out the `docs/` folder:
- [Architecture & Lifecycle](docs/architecture.md)
- [DDD Guide: The "Hero Case"](docs/ddd-guide.md)
- [Full Performance Benchmarks](docs/benchmarks.md)
- [Quality Report / Testing Strategy](docs/quality-report.md)

### 🚀 Roadmap
- [x] Polymorphism & Value Object Flattening (Released in RC1)
- [x] Assembly-wide Configuration Discovery (Released in RC1)
- [ ] Source Generator Extensions: Eliminate Reflection during assembly scanning for even faster startup times.
- [ ] Custom Formatting/Precision: Specific handlers for DateTime and decimal precision at the property level.

---
*Built with ❤️ for clean C# architectures.*
