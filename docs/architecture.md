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

## 2. The Freeze Pattern (Thread Safety & Concurrency)
Once configuration is complete, you call `JsonModelBuilder.Build()`. This transitions the framework from a mutable configuration state into a deeply immutable metadata state (`IJsonModel`, `IJsonEntity`, `IJsonProperty`).

**Why freeze?**
By freezing the configuration upon startup, FluentJson guarantees **absolute thread safety** during the highly concurrent and intense serialization phase. No locking mechanisms (which slow down throughput) are needed during runtime. The JSON engines (Newtonsoft & System.Text.Json) read from these thread-safe immutable dictionaries seamlessly.

## 3. High Performance via Compiled Expressions
A primary goal of FluentJson is to bypass the standard, slow reflection methods typically used for private member access, while avoiding the risks of `FormatterServices.GetUninitializedObject(Type)`.

When an entity is mapped, `EntityTypeBuilder` creates an `Expression.New` tree for the parameterless constructor (even if it is marked as `private` to protect domain invariants) and compiles it into a `Func<object>`. This pre-compiled factory is then injected into the underlying JSON engines, achieving near-native instantiation speeds while respecting strict Domain-Driven Design rules.

## 4. Polymorphism & Recursive Hierarchy Resolution
To support rich Domain-Driven Design cases, FluentJson natively supports polymorphic serialization and inheritance. 

The architecture relies on configuring a **Discriminator** (usually a specific JSON property like `$type` or `type`) on the base entity or interface. Using the Builder, developers map derived types to specific discriminator values.

```csharp
builder.Entity<Payment>()
       .HasDiscriminator("type")
       .HasDerivedType<CreditCardPayment>("credit_card")
       .HasDerivedType<PayPalPayment>("paypal");
```

During the Freeze Phase, the `IJsonModel` will construct a type-resolution dictionary. This enables the runtime engine to instantiate the correct concrete type dynamically based on the JSON payload's discriminator value.

Furthermore, when the adapters inspect an object (like a derived `CreditCardPayment`), FluentJson uses **Recursive Property Resolution**. The internal system walks up the inheritance chain (via `BaseType`) to aggregate mappings from the base `Payment` configuration. This resolves duplication completely, meaning inherited properties are mapped using their abstract configuration seamlessly alongside the derived properties.

## 5. Zero-Allocation Scan Strategy
To resolve polymorphic objects securely and efficiently, FluentJson avoids parsing the whole structure into an interim layer (like a `JsonDocument` or `JObject` when possible) just to find the type discriminator. 

For `System.Text.Json`, FluentJson aggressively leverages a **cloned `Utf8JsonReader`**. It fast-forwards through the raw bytes, applying direct **UTF8 byte sequence matching** against the configured discriminator property names and values. This achieves Zero-Allocation discriminator discovery; the heap isn't littered with string instances just to determine the derived type.

## 6. Sanitized Exception Policy
To safely interoperate in environments exposing APIs externally or writing to centralized sinks, the internal `FluentJsonException` hierarchy and converters enforce a **Sanitized Exception** policy.

Error messages involving failed polymorphic instantiation or mismatched conversion parameters are scrubbed of sensitive information. The metadata model's internal representations (such as underlying private field names, fully-qualified assembly types, or constructor profiles) are explicitly sanitized from standard exception messages, preventing potential reflection-metadata leaks that attackers could act upon.
