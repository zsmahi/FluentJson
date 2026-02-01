namespace FluentJson;

/// <summary>
/// Defines supported naming strategies for JSON property keys in a library-agnostic way.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Enum Abstraction.
/// </para>
/// <para>
/// This enum abstracts the naming policies provided by different JSON libraries (e.g., <c>CamelCaseNamingStrategy</c> in Newtonsoft 
/// vs <c>JsonNamingPolicy.CamelCase</c> in System.Text.Json). It allows the configuration to remain portable 
/// by avoiding direct dependencies on library-specific naming classes.
/// </para>
/// </remarks>
public enum NamingConvention
{
    /// <summary>
    /// Uses the exact name of the property as defined in the C# class (typically PascalCase).
    /// </summary>
    /// <example>Property <c>FirstName</c> becomes <c>"FirstName"</c>.</example>
    Default,

    /// <summary>
    /// Converts property names to camelCase (first letter lowercase).
    /// </summary>
    /// <example>Property <c>FirstName</c> becomes <c>"firstName"</c>.</example>
    CamelCase,

    /// <summary>
    /// Converts property names to snake_case (lowercase with underscores).
    /// </summary>
    /// <example>Property <c>FirstName</c> becomes <c>"first_name"</c>.</example>
    SnakeCase
}
