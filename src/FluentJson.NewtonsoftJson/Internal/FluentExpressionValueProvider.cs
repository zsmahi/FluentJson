using System;
using System.Reflection;
using FluentJson.Exceptions;
using FluentJson.Internal;
using Newtonsoft.Json.Serialization;

namespace FluentJson.NewtonsoftJson.Internal;

/// <summary>
/// A lightweight adapter that bridges Newtonsoft.Json's IValueProvider 
/// to the centralized, cached AccessorFactory in the Core library.
/// </summary>
internal class FluentExpressionValueProvider(MemberInfo memberInfo) : IValueProvider
{
    private readonly MemberInfo _memberInfo = memberInfo ?? throw new ArgumentNullException(nameof(memberInfo));

    // We store the delegates directly to avoid dictionary lookups on every call (micro-optimization).
    // The compilation/caching happens inside AccessorFactory.createXXX().
    private readonly Func<object, object?> _getter = AccessorFactory.CreateGetter(memberInfo);
    private readonly Action<object, object?> _setter = AccessorFactory.CreateSetter(memberInfo);

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
