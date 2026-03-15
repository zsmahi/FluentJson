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

## Managing Complexity: The OCP Pattern
In complex Enterprise systems, configuring everything in one file creates unmaintainable messes. FluentJson adheres to the **Open-Closed Principle (OCP)** using `IJsonTypeConfiguration<T>`.

You can keep mapping instructions physically isolated from your entities, usually inside the Infrastructure layer. You then scan your assemblies automatically using `.ApplyConfigurationsFromAssembly()`.

```csharp
// Infrastructure/Configuration/OrderConfiguration.cs
public class OrderConfiguration : IJsonTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property<string>("_orderNumber").HasName("orderNumber");
        builder.PreserveReferences();
    }
}
```

## A Real-World Snippet
Here is a comprehensive summary of FluentJson tackling a core DDD scenario: An entity with a private constructor, an encapsulated collection, a circular reference, and a Strongly-Typed ID value object.

```csharp
// 1. Domain
public record OrderId(Guid Value);

public class Order
{
    private List<OrderItem> _items;

    public OrderId Id { get; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    private Order() { } // Invariant protection
}

public class OrderItem
{
    public Order ParentOrder { get; } // Circular Reference
}

// 2. Configuration (OCP)
public class OrderConfig : IJsonTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        // Flatten StronglyTypedId automatically!
        builder.Property(x => x.Id)
               .HasConversion(id => id.Value, value => new OrderId(value));

        // Let the engine know about cyclic graphs
        builder.PreserveReferences();
    }
}
```

## Conclusion
FluentJson's decoupled nature paired with advanced compiled expressions empowers you to finally write clean C# DDD models without compromise or performance overhead.
