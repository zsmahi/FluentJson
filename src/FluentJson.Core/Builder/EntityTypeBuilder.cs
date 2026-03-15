using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FluentJson.Core.Exceptions;
using FluentJson.Core.Metadata;

namespace FluentJson.Core.Builder;

/// <summary>
/// Provides a fluent API to configure JSON serialization rules for a specific entity type.
/// </summary>
/// <typeparam name="TEntity">The type of the entity being configured.</typeparam>
/// <remarks>
/// Use this builder to map properties or fields, enforce required paths, or completely ignore members. 
/// It allows configuring private state and accessing private parameterless constructors.
/// </remarks>
public class EntityTypeBuilder<TEntity> : IEntityTypeBuilder
{
    private readonly Dictionary<MemberInfo, object> _propertyBuilders = new();

    IJsonEntity IEntityTypeBuilder.Build()
    {
        var properties = new List<IJsonProperty>();
        
        foreach (var kvp in _propertyBuilders)
        {
            var memberInfo = kvp.Key;
            var builder = (IPropertyBuilder)kvp.Value;
            
            // Allow mapping both Properties and Fields
            Type memberType = memberInfo switch
            {
                PropertyInfo p => p.PropertyType,
                FieldInfo f => f.FieldType,
                _ => throw new FluentJsonConfigurationException($"Member '{memberInfo.Name}' is neither a property nor a field.")
            };

            properties.Add(builder.Build(memberInfo, memberInfo.Name));
        }

        var constructorInfo = typeof(TEntity).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            Type.EmptyTypes,
            null);

        if (constructorInfo == null)
        {
            throw new FluentJsonConfigurationException($"No parameterless constructor found for type {typeof(TEntity)}. A private parameterless constructor is required for DDD invariant safety.");
        }

        var newExpression = Expression.New(constructorInfo);
        var castExpression = Expression.Convert(newExpression, typeof(object));
        var factory = Expression.Lambda<Func<object>>(castExpression).Compile();

        return new JsonEntity(typeof(TEntity), properties.AsReadOnly(), factory);
    }

    /// <summary>
    /// Configures a property or field using a strongly-typed expression.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property being configured.</typeparam>
    /// <param name="propertyExpression">A lambda expression representing the member access (e.g., <c>x => x.Name</c>).</param>
    /// <returns>A <see cref="PropertyBuilder{TProperty}"/> to further configure the mapped member.</returns>
    /// <remarks>
    /// This is the preferred method for mapping members as it provides compile-time safety and refactoring support.
    /// </remarks>
    public PropertyBuilder<TProperty> Property<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
    {
        var memberExpression = propertyExpression.Body as MemberExpression;

        // For cases where there's a unary expression (e.g. boxing)
        if (memberExpression == null && propertyExpression.Body is UnaryExpression unaryExpression)
        {
            memberExpression = unaryExpression.Operand as MemberExpression;
        }

        if (memberExpression == null || memberExpression.Expression is not ParameterExpression)
        {
            throw new FluentJsonExpressionException("Expression must be a simple member access (e.g., x => x.Name). Nested properties or methods are not supported.");
        }

        var memberInfo = memberExpression.Member;

        if (!_propertyBuilders.TryGetValue(memberInfo, out var builder))
        {
            builder = new PropertyBuilder<TProperty>();
            _propertyBuilders[memberInfo] = builder;
        }

        return (PropertyBuilder<TProperty>)builder;
    }

    /// <summary>
    /// Configures a property or field by its string name.
    /// </summary>
    /// <typeparam name="TProperty">The type of the target member to configure.</typeparam>
    /// <param name="propertyName">The name of the property or field to map.</param>
    /// <returns>A <see cref="PropertyBuilder{TProperty}"/> to further configure the mapped member.</returns>
    /// <remarks>
    /// Useful for mapping private fields or properties that are not accessible via standard lambda expressions.
    /// </remarks>
    public PropertyBuilder<TProperty> Property<TProperty>(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentException("Property name cannot be null or empty.", nameof(propertyName));

        var memberInfo = typeof(TEntity).GetMember(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault();

        if (memberInfo == null)
        {
            throw new FluentJsonConfigurationException($"Member '{propertyName}' not found on type {typeof(TEntity)}.");
        }

        if (!_propertyBuilders.TryGetValue(memberInfo, out var builder))
        {
            builder = new PropertyBuilder<TProperty>();
            _propertyBuilders[memberInfo] = builder;
        }

        return (PropertyBuilder<TProperty>)builder;
    }

    /// <summary>
    /// Instructs the framework to completely ignore a specific property or field during serialization and deserialization.
    /// </summary>
    /// <param name="propertyExpression">A lambda expression representing the member to ignore.</param>
    /// <returns>The same <see cref="EntityTypeBuilder{TEntity}"/> instance so that multiple calls can be chained.</returns>
    public EntityTypeBuilder<TEntity> Ignore(Expression<Func<TEntity, object?>> propertyExpression)
    {
        var memberExpression = propertyExpression.Body as MemberExpression;

        if (memberExpression == null && propertyExpression.Body is UnaryExpression unaryExpression)
        {
            memberExpression = unaryExpression.Operand as MemberExpression;
        }

        if (memberExpression == null || memberExpression.Expression is not ParameterExpression)
        {
            throw new FluentJsonExpressionException("Expression must be a simple member access (e.g., x => x.Name). Nested properties or methods are not supported.");
        }

        var memberInfo = memberExpression.Member;

        if (!_propertyBuilders.TryGetValue(memberInfo, out var builder))
        {
            builder = new PropertyBuilder<object>();
            _propertyBuilders[memberInfo] = builder;
        }

        ((IPropertyBuilder)builder).SetIgnored(true);

        return this;
    }
}
