using Newtonsoft.Json.Serialization;

namespace FluentJson.NewtonsoftJson.Internal;

/// <summary>
/// A specialized value provider that bypasses the object instance and always returns a constant, pre-defined value.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Virtual Property / Constant Injection.
/// </para>
/// <para>
/// This provider is used exclusively during the **Serialization** phase of polymorphic hierarchies. 
/// It allows the serializer to "inject" the discriminator value (e.g., <c>"type": "circle"</c>) into the generated JSON, 
/// even if the underlying CLR object (<c>Circle</c> class) does not strictly contain a property holding that value.
/// </para>
/// </remarks>
/// <param name="value">The constant value to be returned by <see cref="GetValue(object)"/>.</param>
internal class FixedValueProvider(object value) : IValueProvider
{
    private readonly object _value = value;

    /// <summary>
    /// Returns the configured constant value, ignoring the target object instance entirely.
    /// </summary>
    /// <param name="target">The object being serialized (ignored).</param>
    /// <returns>The fixed constant value.</returns>
    public object? GetValue(object target) => _value;

    /// <summary>
    /// Intentionally performs no operation.
    /// </summary>
    /// <param name="target">The target object.</param>
    /// <param name="value">The value to set.</param>
    /// <remarks>
    /// <para>
    /// <strong>Architectural Note:</strong>
    /// This method is a No-Op because this provider is designed for 'Output Injection' only. 
    /// We assume the discriminator is derived from the type itself, not stored in a mutable field 
    /// that needs to be updated during deserialization.
    /// </para>
    /// </remarks>
    public void SetValue(object target, object? value)
    {
        // No-Op: We do not write fixed values back to the entity.
    }
}
