using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace FluentJson.NewtonsoftJson.Internal;

/// <summary>
/// A value provider that forces a specific value during serialization (Get),
/// but delegates to the underlying member during deserialization (Set).
/// </summary>
internal class FixedValueProvider(object fixedValue, PropertyInfo? underlyingProperty) : IValueProvider
{
    private readonly object _fixedValue = fixedValue;
    private readonly PropertyInfo? _underlyingProperty = underlyingProperty;

    public object? GetValue(object target) => _fixedValue;

    public void SetValue(object target, object? value)
    {
        if (_underlyingProperty != null && _underlyingProperty.CanWrite)
        {
            _underlyingProperty.SetValue(target, value);
        }
    }
}
