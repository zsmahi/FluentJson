using System.Reflection;

using Newtonsoft.Json.Serialization;

namespace FluentJson.Newtonsoft;

/// <summary>
/// An internal Newtonsoft <see cref="IValueProvider"/> that forces reading and writing directly to a specific <see cref="PropertyInfo"/>, ignoring standard engine accessibility rules.
/// </summary>
internal class ExpressionValueProvider : IValueProvider
{
    private readonly PropertyInfo _propertyInfo;

    public ExpressionValueProvider(PropertyInfo propertyInfo)
    {
        _propertyInfo = propertyInfo;
    }

    public object? GetValue(object target)
    {
        return _propertyInfo.GetValue(target);
    }

    public void SetValue(object target, object? value)
    {
        _propertyInfo.SetValue(target, value);
    }
}
