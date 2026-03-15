# FluentJson Architecture

FluentJson is meticulously engineered around three core concepts: the **Builder Pattern**, the **Freeze Pattern**, and **Compiled Expressions**. This guide explains the lifecycle of a FluentJson configuration.

## 1. The Configuration Phase (Builder Pattern)
During application startup, you use `JsonModelBuilder` and `EntityTypeBuilder<T>` to configure your domain mappings.
This phase is entirely mutable. You can map properties, ignore them, or load external `IJsonTypeConfiguration<T>` classes from other assemblies for modularity.

```csharp
var builder = new JsonModelBuilder();
builder.Entity<User>()
       .Property(u => u.Id).HasName("userId")
       .Property<string>("_passwordHash").Ignore();
```

## 2. The Freeze Pattern
Once configuration is complete, you call `JsonModelBuilder.Build()`. This transitions the framework from a mutable configuration state into a deeply immutable metadata state (`IJsonModel`, `IJsonEntity`, `IJsonProperty`).

**Why freeze?**
By freezing the configuration upon startup, FluentJson guarantees absolute thread safety during the highly concurrent and intense serialization phase without relying on locking overhead. The JSON engines (Newtonsoft & System.Text.Json) read from these immutable models seamlessly.

## 3. High Performance via Compiled Expressions
A primary goal of FluentJson is to bypass the standard, slow reflection methods typically used for private member access, while avoiding the risks of `FormatterServices.GetUninitializedObject(Type)`.

When an entity is mapped, `EntityTypeBuilder` creates an `Expression.New` tree for the parameterless constructor (even if it is marked as `private` to protect domain invariants) and compiles it into a `Func<object>`. This pre-compiled factory is then injected into the underlying JSON engines, achieving near-native instantiation speeds while respecting strict Domain-Driven Design rules.
