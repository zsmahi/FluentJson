# Domain-Driven Design Guide (The "Hero Case")

One of the core motivations behind FluentJson is keeping Domain models pristine and completely decoupled perfectly from infrastructure concerns like JSON mapping. This is our "Hero Case".

## The Problem
When using `System.Text.Json` or `Newtonsoft.Json`, developers are often forced to pollute their clean Domain entities with serialization attributes (e.g., `[JsonConstructor]`, `[JsonPropertyName]`). Furthermore, to enable deserialization, you might be required to open your internal state securely or expose a public parameterless constructor, violating the core principle of encapsulation.

## The FluentJson Solution
FluentJson provides an external configuration block (via the `JsonModelBuilder`), separating JSON mapping concerns from business rules completely.

### 1. Mapping Private Fields
Instead of compromising behavior by adding getters and setters solely for serialization, you can map JSON properties directly to private fields using FluentJson's expression builder or string builder.

```csharp
builder.Entity<Order>()
       .Property<string>("_orderNumber")
       .HasName("orderNumber");
```

### 2. Protecting Invariants with Private Constructors
A robust and secure Domain entity must prevent anemic, empty instantiations. FluentJson requires a parameterless constructor, but crucially, **it can be marked as `private`**.

When you build your configuration, FluentJson's expression compiler will locate that private constructor and assemble an optimized factory delegate. This mechanism safely bypasses legacy workarounds and completely avoids `FormatterServices.GetUninitializedObject(Type)`, guaranteeing that the instantiation does not circumvent your required internal setups or rules.

### 3. Modularizing Configurations
In complex Enterprise systems, mixing configurations together creates unmaintainable mess. `IJsonTypeConfiguration<T>` is provided so you can keep mapping instructions physically isolated from your entities.

## Conclusion
FluentJson's decoupled nature paired with advanced compiled expressions empowers you to finally write clean C# DDD models without compromise or performance overhead.
