using System;
using System.Linq.Expressions;
using System.Reflection;

namespace FluentJson.Internal;

/// <summary>
/// A centralized factory for compiling high-performance property and field accessors using Expression Trees.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Metaprogramming / Flyweight Factory.
/// </para>
/// <para>
/// This utility eliminates the performance overhead of standard Reflection (<c>MethodInfo.Invoke</c> or <c>PropertyInfo.GetValue</c>) 
/// by dynamically compiling type-safe delegates at runtime. 
/// </para>
/// <para>
/// <strong>Architectural Note:</strong>
/// The generated delegates (<c>Func&lt;object, object&gt;</c> and <c>Action&lt;object, object&gt;</c>) serve as a 
/// "Universal Adapter," allowing the JSON engines to interact with private fields or properties of any type 
/// (struct or class) uniformly, with performance characteristics near that of direct C# code execution.
/// </para>
/// </remarks>
public static class AccessorFactory
{
    /// <summary>
    /// Compiles a delegate that retrieves the value of a member (property or field).
    /// </summary>
    /// <param name="member">The member metadata.</param>
    /// <returns>A compiled function taking the target object instance and returning the member value.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Technical Detail:</strong>
    /// Automatically handles the necessary casting logic. If the target entity is a value type (struct), 
    /// <c>Expression.Unbox</c> is used; otherwise, <c>Expression.Convert</c> is used. This ensures 
    /// safe access without invalid cast exceptions at runtime.
    /// </para>
    /// </remarks>
    public static Func<object, object?> CreateGetter(MemberInfo member)
    {
        ParameterExpression targetParam = Expression.Parameter(typeof(object), "target");
        Type declaringType = member.DeclaringType!;

        UnaryExpression castTarget = declaringType.IsValueType
            ? Expression.Unbox(targetParam, declaringType)
            : Expression.Convert(targetParam, declaringType);

        MemberExpression memberAccess = Expression.MakeMemberAccess(castTarget, member);
        UnaryExpression castResult = Expression.Convert(memberAccess, typeof(object));

        return Expression.Lambda<Func<object, object?>>(castResult, targetParam).Compile();
    }

    /// <summary>
    /// Compiles a delegate that sets the value of a member (property or field).
    /// </summary>
    /// <param name="member">The member metadata.</param>
    /// <returns>A compiled action that assigns a value to the member on the target object instance.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Technical Detail:</strong>
    /// The generated setter handles type coercion for the incoming <c>value</c> parameter. 
    /// It unboxes the value if the target property is a value type, ensuring compatibility 
    /// with the generic <c>object</c> signature required by the serialization engines.
    /// </para>
    /// </remarks>
    public static Action<object, object?> CreateSetter(MemberInfo member)
    {
        ParameterExpression targetParam = Expression.Parameter(typeof(object), "target");
        ParameterExpression valueParam = Expression.Parameter(typeof(object), "value");
        Type declaringType = member.DeclaringType!;

        UnaryExpression castTarget = declaringType.IsValueType
            ? Expression.Unbox(targetParam, declaringType)
            : Expression.Convert(targetParam, declaringType);

        Type propertyType = (member is FieldInfo f) ? f.FieldType : ((PropertyInfo)member).PropertyType;

        UnaryExpression castValue = propertyType.IsValueType
            ? Expression.Unbox(valueParam, propertyType)
            : Expression.Convert(valueParam, propertyType);

        MemberExpression memberAccess = Expression.MakeMemberAccess(castTarget, member);
        BinaryExpression assign = Expression.Assign(memberAccess, castValue);

        return Expression.Lambda<Action<object, object?>>(assign, targetParam, valueParam).Compile();
    }
}
