using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace FluentJson.Internal;

public static class AccessorFactory
{
    private static readonly ConcurrentDictionary<MemberInfo, Func<object, object?>> _getters = new();
    private static readonly ConcurrentDictionary<MemberInfo, Action<object, object?>> _setters = new();

    public static Func<object, object?> CreateGetter(MemberInfo member)
    {
        if (member == null) throw new ArgumentNullException(nameof(member));

        return _getters.GetOrAdd(member, static m =>
        {
            ParameterExpression targetParam = Expression.Parameter(typeof(object), "target");
            Type declaringType = m.DeclaringType!;

            UnaryExpression castTarget = declaringType.IsValueType
                ? Expression.Unbox(targetParam, declaringType)
                : Expression.Convert(targetParam, declaringType);

            MemberExpression memberAccess = Expression.MakeMemberAccess(castTarget, m);
            UnaryExpression castResult = Expression.Convert(memberAccess, typeof(object));

            return Expression.Lambda<Func<object, object?>>(castResult, targetParam).Compile();
        });
    }

    public static Action<object, object?>? CreateSetter(MemberInfo member)
    {
        if (member == null) throw new ArgumentNullException(nameof(member));

        // Check for read-only members to prevent Expression.Assign failure
        if (!IsWriteable(member))
        {
            return null;
        }

        return _setters.GetOrAdd(member, static m =>
        {
            ParameterExpression targetParam = Expression.Parameter(typeof(object), "target");
            ParameterExpression valueParam = Expression.Parameter(typeof(object), "value");
            Type declaringType = m.DeclaringType!;

            UnaryExpression castTarget = declaringType.IsValueType
                ? Expression.Unbox(targetParam, declaringType)
                : Expression.Convert(targetParam, declaringType);

            Type propertyType = (m is FieldInfo f) ? f.FieldType : ((PropertyInfo)m).PropertyType;

            UnaryExpression castValue = propertyType.IsValueType
                ? Expression.Unbox(valueParam, propertyType)
                : Expression.Convert(valueParam, propertyType);

            MemberExpression memberAccess = Expression.MakeMemberAccess(castTarget, m);
            BinaryExpression assign = Expression.Assign(memberAccess, castValue);

            return Expression.Lambda<Action<object, object?>>(assign, targetParam, valueParam).Compile();
        });
    }

    private static bool IsWriteable(MemberInfo member)
    {
        return member switch
        {
            PropertyInfo prop => prop.CanWrite,
            FieldInfo field => !field.IsInitOnly && !field.IsLiteral,
            _ => false
        };
    }
}
