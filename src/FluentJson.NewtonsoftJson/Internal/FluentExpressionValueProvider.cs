using FluentJson.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace FluentJson.NewtonsoftJson.Internal;

/// <summary>
/// A high-performance value provider implementation using compiled Expression Trees.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Proxy / Cache.
/// </para>
/// <para>
/// This class replaces the default reflection-based <see cref="IValueProvider"/> of Newtonsoft.Json. 
/// Instead of using slow <c>GetValue</c>/<c>SetValue</c> reflection calls on every access, 
/// it compiles strongly-typed delegates (via <see cref="AccessorFactory"/>) and caches them statically.
/// </para>
/// <para>
/// <strong>Performance:</strong>
/// This approach achieves property access speeds comparable to direct C# code execution, significantly 
/// reducing the overhead of serialization for large object graphs.
/// </para>
/// </remarks>
internal class FluentExpressionValueProvider : IValueProvider
{
    private readonly MemberInfo _memberInfo;

    // Static Cache: Ensures compilation happens only once per member across the entire app lifetime.
    private static readonly ConcurrentDictionary<MemberInfo, Func<object, object?>> _getterCache = new();
    private static readonly ConcurrentDictionary<MemberInfo, Action<object, object?>> _setterCache = new();

    /// <summary>
    /// Initializes a new instance of the compiled value provider.
    /// </summary>
    /// <param name="memberInfo">The member (Field or Property) to access.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="memberInfo"/> is null.</exception>
    public FluentExpressionValueProvider(MemberInfo memberInfo)
    {
        if (memberInfo == null)
        {
            throw new ArgumentNullException(nameof(memberInfo));
        }
        _memberInfo = memberInfo;
    }

    /// <summary>
    /// Retrieves the value from the target object using the cached compiled delegate.
    /// </summary>
    /// <param name="target">The object instance.</param>
    /// <returns>The value of the member.</returns>
    /// <exception cref="JsonSerializationException">Thrown if the delegate execution fails.</exception>
    public object? GetValue(object target)
    {
        try
        {
            // Delegates to the Shared Kernel factory and caches the result.
            // 'static' lambda prevents closure allocation for cache lookups.
            Func<object, object?> getter = _getterCache.GetOrAdd(_memberInfo, static m => AccessorFactory.CreateGetter(m));
            return getter(target);
        }
        catch (Exception ex)
        {
            throw new JsonSerializationException($"Error reading member '{_memberInfo.Name}' on type '{target.GetType().Name}'.", ex);
        }
    }

    /// <summary>
    /// Sets the value on the target object using the cached compiled delegate.
    /// </summary>
    /// <param name="target">The object instance.</param>
    /// <param name="value">The value to assign.</param>
    /// <exception cref="JsonSerializationException">Thrown if the delegate execution fails.</exception>
    public void SetValue(object target, object? value)
    {
        try
        {
            // Delegates to the Shared Kernel factory and caches the result.
            Action<object, object?> setter = _setterCache.GetOrAdd(_memberInfo, static m => AccessorFactory.CreateSetter(m));
            setter(target, value);
        }
        catch (Exception ex)
        {
            throw new JsonSerializationException($"Error writing to member '{_memberInfo.Name}' on type '{target.GetType().Name}'.", ex);
        }
    }
}
