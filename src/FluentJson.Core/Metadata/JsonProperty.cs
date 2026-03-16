using System;
using System.Reflection;

namespace FluentJson.Core.Metadata;

/// <summary>
/// Internal immutable implementation of <see cref="IJsonProperty"/>.
/// </summary>
internal class JsonProperty : IJsonProperty
{
    public string Name { get; }
    public MemberInfo MemberInfo { get; }
    public Type PropertyType { get; }
    public bool IsRequired { get; }
    public bool IsIgnored { get; }
    public Type? ConvertedType { get; }
    public Delegate? SerializeFunc { get; }
    public Delegate? DeserializeFunc { get; }

    public JsonProperty(
        string name,
        MemberInfo memberInfo,
        bool isRequired,
        bool isIgnored,
        Type? convertedType,
        Delegate? serializeFunc,
        Delegate? deserializeFunc)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        MemberInfo = memberInfo ?? throw new ArgumentNullException(nameof(memberInfo));
        PropertyType = memberInfo switch
        {
            PropertyInfo p => p.PropertyType,
            FieldInfo f => f.FieldType,
            _ => throw new ArgumentException("Member must be a property or a field", nameof(memberInfo))
        };
        IsRequired = isRequired;
        IsIgnored = isIgnored;
        ConvertedType = convertedType;
        SerializeFunc = serializeFunc;
        DeserializeFunc = deserializeFunc;
    }
}
