using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace FluentJson.Internal;

/// <summary>
/// A centralized factory for compiling high-performance property and field accessors using Expression Trees.
/// Includes an internal L1 cache to ensure delegates are compiled only once per member.
/// </summary>
public static class AccessorFactory
{
    // Thread-safe cache to prevent expensive recompilation of Expression Trees.
    private static readonly ConcurrentDictionary<MemberInfo, Func<object, object?>> _getters = new();
    private static readonly ConcurrentDictionary<MemberInfo, Action<object, object?>> _setters = new();

    /// <summary>
    /// Retrieves or compiles a delegate that gets the value of a member.
    /// </summary>
    public static Func<object, object?> CreateGetter(MemberInfo member)
    {
        if (member == null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        return _getters.GetOrAdd(member, static m =>
        {
            ParameterExpression targetParam = Expression.Parameter(typeof(object), "target");
            Type declaringType = m.DeclaringType!;

            // Cast the generic 'object' target to the specific entity type
            UnaryExpression castTarget = declaringType.IsValueType
                ? Expression.Unbox(targetParam, declaringType)
                : Expression.Convert(targetParam, declaringType);

            MemberExpression memberAccess = Expression.MakeMemberAccess(castTarget, m);

            // Box the result back to 'object'
            UnaryExpression castResult = Expression.Convert(memberAccess, typeof(object));

            return Expression.Lambda<Func<object, object?>>(castResult, targetParam).Compile();
        });
    }

    /// <summary>
    /// Retrieves or compiles a delegate that sets the value of a member.
    /// </summary>
    public static Action<object, object?> CreateSetter(MemberInfo member)
    {
        if (member == null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        return _setters.GetOrAdd(member, static m =>
        {
            ParameterExpression targetParam = Expression.Parameter(typeof(object), "target");
            ParameterExpression valueParam = Expression.Parameter(typeof(object), "value");
            Type declaringType = m.DeclaringType!;

            // 1. Cast Target
            UnaryExpression castTarget = declaringType.IsValueType
                ? Expression.Unbox(targetParam, declaringType)
                : Expression.Convert(targetParam, declaringType);

            // 2. Cast Value
            Type propertyType = (m is FieldInfo f) ? f.FieldType : ((PropertyInfo)m).PropertyType;
            UnaryExpression castValue = propertyType.IsValueType
                ? Expression.Unbox(valueParam, propertyType)
                : Expression.Convert(valueParam, propertyType);

            // 3. Assign
            MemberExpression memberAccess = Expression.MakeMemberAccess(castTarget, m);
            BinaryExpression assign = Expression.Assign(memberAccess, castValue);

            return Expression.Lambda<Action<object, object?>>(assign, targetParam, valueParam).Compile();
        });
    }
}
