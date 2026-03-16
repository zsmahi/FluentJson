# Zero-Allocation Scanning

Performance and memory efficiency are paramount in modern .NET applications. FluentJson was engineered to integrate flawlessly into high-throughput systems. One of the most critical optimizations resides within its Polymorphic Engine.

## The Problem: Discriminator Discovery
When deserializing polymorphic payloads, the engine must extract the runtime type "discriminator" (e.g., `"$type": "credit_card"`) before it can instantiate the object and deserialize its properties.
Historically, this required reading the JSON stream into an intermediary DOM (e.g., `JsonDocument` or `JObject`), finding the discriminator, and then converting back or reading the object into memory using string allocations.

## Zero-Allocation Strategy (System.Text.Json)
FluentJson completely bypasses intermediary DOM structures. It implements a **Zero-Allocation Scan** strategy when working with `System.Text.Json`.

1. **Reader Cloning**: When FluentJson needs to resolve a type discriminator, it clones the current `Utf8JsonReader`. Because `Utf8JsonReader` is a struct (`ref struct`), cloning it incurs zero heap allocations.
2. **Byte Sequence Matching**: As the cloned reader scans ahead for the discriminator property, FluentJson *avoids* allocating .NET `string` objects to compare keys and values. Instead, it matches the raw UTF-8 byte span (`reader.ValueSpan` / `reader.GetString()`) directly against pre-computed UTF-8 byte arrays of standard discriminators.

This technique guarantees that the heap is not continuously polluted with throwaway strings during intense API bursts just to figure out what object to create.

## Memory Efficiency vs. Flexibility
The trade-off for this extreme optimization is flexibility. By enforcing strict, pre-compiled configurations:
- We lose the ability to dynamically resolve completely unregistered internal types at runtime on-the-fly.
- We must pre-compile our Discriminator maps during the initial application startup.

However, in the context of strict Domain-Driven Design and microservices architectures, this trade-off heavily favors security, stability, and speed over unrestrained dynamic reflection.
