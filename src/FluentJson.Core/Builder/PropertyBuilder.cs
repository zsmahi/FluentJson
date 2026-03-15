using System;
using System.Reflection;
using FluentJson.Core.Metadata;

namespace FluentJson.Core.Builder;

/// <summary>
/// Configures the serialization rules for a specific property or field.
/// </summary>
/// <typeparam name="TProperty">The type of the mapped member.</typeparam>
public class PropertyBuilder<TProperty> : IPropertyBuilder
{
    private string? _jsonName;
    private bool _isRequired;
    private bool _isIgnored;

    /// <summary>
    /// Overrides the default JSON property name (which is the member's name) with a custom string.
    /// </summary>
    /// <param name="jsonName">The custom JSON name to use.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public PropertyBuilder<TProperty> HasName(string jsonName)
    {
        if (string.IsNullOrWhiteSpace(jsonName))
        {
            throw new ArgumentException("Json name cannot be null or whitespace.", nameof(jsonName));
        }

        _jsonName = jsonName;
        return this;
    }

    /// <summary>
    /// Marks the property as required during deserialization.
    /// </summary>
    /// <param name="required">If true, the property is required. Defaults to true.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public PropertyBuilder<TProperty> IsRequired(bool required = true)
    {
        _isRequired = required;
        return this;
    }

    /// <summary>
    /// Instructs the framework to completely ignore this member during serialization and deserialization.
    /// </summary>
    /// <param name="ignored">If true, the member is ignored. Defaults to true.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public PropertyBuilder<TProperty> Ignore(bool ignored = true)
    {
        _isIgnored = ignored;
        return this;
    }

    void IPropertyBuilder.SetIgnored(bool ignored)
    {
        _isIgnored = ignored;
    }

    IJsonProperty IPropertyBuilder.Build(MemberInfo memberInfo, string defaultName)
    {
        // Default json name is just the property name for now
        // A real builder would allow overriding this via this.HasName("...")
        return new JsonProperty(_jsonName ?? defaultName, memberInfo, _isRequired, _isIgnored);
    }
}
