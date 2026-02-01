using System;
using System.Reflection;
using FluentJson.Exceptions;
using FluentJson.Internal;
using Newtonsoft.Json.Serialization;

namespace FluentJson.NewtonsoftJson.Internal;

internal class FluentExpressionValueProvider(MemberInfo memberInfo) : IValueProvider
{
    private readonly MemberInfo _memberInfo = memberInfo ?? throw new ArgumentNullException(nameof(memberInfo));

    private readonly Func<object, object?> _getter = AccessorFactory.CreateGetter(memberInfo);

    // Marked as nullable to handle read-only members returned from AccessorFactory
    private readonly Action<object, object?>? _setter = AccessorFactory.CreateSetter(memberInfo);

    public object? GetValue(object target)
    {
        try
        {
            return _getter(target);
        }
        catch (Exception ex)
        {
            throw new FluentJsonRuntimeException(
                $"Error reading member '{_memberInfo.Name}' on type '{target.GetType().Name}'.", ex);
        }
    }

    public void SetValue(object target, object? value)
    {
        // If the setter is null (e.g., member is readonly), we skip the assignment
        if (_setter == null)
        {
            return;
        }

        try
        {
            _setter(target, value);
        }
        catch (Exception ex)
        {
            throw new FluentJsonRuntimeException(
                $"Error writing to member '{_memberInfo.Name}' on type '{target.GetType().Name}'.", ex);
        }
    }
}
