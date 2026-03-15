using System.Reflection;
using FluentJson.Core.Metadata;

namespace FluentJson.Core.Builder;

/// <summary>
/// An internal abstraction for finalizing a mapped member's configuration.
/// </summary>
internal interface IPropertyBuilder
{
    IJsonProperty Build(MemberInfo memberInfo, string defaultName);
    void SetIgnored(bool ignored);
}
