# FluentJson

> **A humble but powerful zero-attribute JSON configuration library for .NET.**

FluentJson allows true separation of Domain Models (DDD) from serialization concerns, bringing the beauty of the Builder Pattern to `System.Text.Json` and `Newtonsoft.Json`.

## ✨ Key Features
- **Zero-Attribute Serialization**: Keep your Domain pristine. No `[JsonConstructor]`, no `[JsonPropertyName]`.
- **Engine Parity**: Write your configuration once. Use it seamlessly with both `System.Text.Json` and `Newtonsoft.Json`.
- **High Performance**: Powered by compiled expression trees, FluentJson achieves near-native instantiation speeds while avoiding the slow `FormatterServices.GetUninitializedObject(Type)`.
- **Deep DDD Support**: Safely instantiate entities using private parameterless constructors and map properties directly to private fields.

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

## ⚡ Performance
FluentJson uses compiled expressions to bypass reflection bottlenecks. Below are the benchmarks displaying our highly optimized pipeline against the native engines:

| Method                | Mean     | Ratio | Allocated |
|---------------------- |---------:|------:|----------:|
| SystemTextJson_Native | 332.5 ns |  1.00 |     112 B |
| SystemTextJson_Fluent | 386.2 ns |  1.16 |     176 B |
| Newtonsoft_Native     | 661.3 ns |  1.99 |    2856 B |
| Newtonsoft_Fluent     | 717.1 ns |  2.16 |    2856 B |

*See [docs/benchmarks.md](docs/benchmarks.md) for full details.*

## 📚 Navigation & Technical Depth
Want to explore how it all works under the hood? Check out the `docs/` folder:
- [Architecture & Lifecycle](docs/architecture.md)
- [DDD Guide: The "Hero Case"](docs/ddd-guide.md)
- [Full Performance Benchmarks](docs/benchmarks.md)

## 🗺️ Limitations & Roadmap
As a community-driven tool, we believe in humility and transparency. Currently, FluentJson focuses exclusively on the core mapping of properties and fields.

**Coming soon on the Roadmap:**
- Support for polymorphic serialization.
- Automatic configuration discovery via Source Generators for zero-overhead startup time.

---
*Built with ❤️ for clean C# architectures.*
