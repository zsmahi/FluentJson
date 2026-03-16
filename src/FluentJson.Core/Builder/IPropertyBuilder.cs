using System.Reflection;

using FluentJson.Core.Metadata;

namespace FluentJson.Core.Builder;

/// <summary>
/// An internal abstraction for finalizing a mapped member's configuration.
/// </summary>
internal interface IPropertyBuilder
{
    public IJsonProperty Build(MemberInfo memberInfo, string defaultName);
    public void SetIgnored(bool ignored);
}
