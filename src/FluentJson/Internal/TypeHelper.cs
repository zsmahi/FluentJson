using System;
using System.Linq.Expressions;
using System.Reflection;

namespace FluentJson.Internal;

/// <summary>
/// Provides low-level utility methods for analyzing and inspecting Expression Trees.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Helper / Static Utility.
/// </para>
/// <para>
/// This class acts as the bridge between the strong-typed fluent API (using lambdas) and the underlying 
/// reflection-based configuration engine. It is responsible for safely extracting <see cref="MemberInfo"/> 
/// metadata from expressions like <c>x => x.MyProperty</c>.
/// </para>
/// </remarks>
internal static class TypeHelper
{
    /// <summary>
    /// Extracts the <see cref="PropertyInfo"/> metadata from a property accessor lambda.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <typeparam name="TProp">The type of the property.</typeparam>
    /// <param name="expression">A lambda expression selecting a property (e.g., <c>x => x.MyProperty</c>).</param>
    /// <returns>The reflection metadata for the selected property.</returns>
    /// <exception cref="ArgumentException">Thrown if the expression refers to a field or a method instead of a property.</exception>
    public static PropertyInfo GetProperty<T, TProp>(Expression<Func<T, TProp>> expression)
    {
        MemberInfo member = GetMember(expression);
        if (member is PropertyInfo prop)
        {
            return prop;
        }
        throw new ArgumentException($"The expression '{expression}' refers to a field, not a property. Use HasField() for fields.");
    }

    /// <summary>
    /// Extracts the <see cref="FieldInfo"/> metadata from a field accessor lambda.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <typeparam name="TProp">The type of the field.</typeparam>
    /// <param name="expression">A lambda expression selecting a field (e.g., <c>x => x._myField</c>).</param>
    /// <returns>The reflection metadata for the selected field.</returns>
    /// <exception cref="ArgumentException">Thrown if the expression refers to a property or a method instead of a field.</exception>
    public static FieldInfo GetField<T, TProp>(Expression<Func<T, TProp>> expression)
    {
        MemberInfo member = GetMember(expression);
        if (member is FieldInfo field)
        {
            return field;
        }
        throw new ArgumentException($"The expression '{expression}' refers to a property, not a field.");
    }

    /// <summary>
    /// Core logic to extract <see cref="MemberInfo"/> from a lambda expression.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Architectural Note:</strong>
    /// This method is robust against implicit compiler behaviors. Specifically, when a value type property 
    /// (e.g., <c>int Id</c>) is selected in a lambda returning <c>object</c> (or a generic type that boxes), 
    /// the C# compiler wraps the access in a <c>Convert()</c> operation (UnaryExpression). 
    /// This method automatically unwraps this boxing operation to reach the underlying MemberExpression.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The input type of the function.</typeparam>
    /// <typeparam name="TValue">The return type of the function.</typeparam>
    /// <param name="expression">The lambda expression to inspect.</param>
    /// <returns>The <see cref="MemberInfo"/> representing the accessed property or field.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="expression"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if the expression is not a simple member access (e.g., if it contains method calls, math operations, 
    /// or accesses variables from a closure).
    /// </exception>
    public static MemberInfo GetMember<T, TValue>(Expression<Func<T, TValue>> expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        Expression body = expression.Body;

        // Unwrap implicit boxing (Convert(x.Prop)) if present
        if (body is UnaryExpression unaryExpression)
        {
            body = unaryExpression.Operand;
        }

        if (body is MemberExpression memberExpression)
        {
            if (memberExpression.Member is PropertyInfo || memberExpression.Member is FieldInfo)
            {
                // Safety Check: Ensure the member is accessed on the parameter 'x', not on a captured closure variable.
                // This prevents errors like "x => localVariable.Property" which is valid C# but invalid for configuration mapping.
                if (memberExpression.Expression is ParameterExpression param && param.Name == expression.Parameters[0].Name)
                {
                    return memberExpression.Member;
                }
            }
        }

        throw new ArgumentException(
            $"The expression '{expression}' is not a valid property or field access. " +
            "Ensure you are selecting a member directly on the input parameter (e.g., x => x.Name).");
    }
}
