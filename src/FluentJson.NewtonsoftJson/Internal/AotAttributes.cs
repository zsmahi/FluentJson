namespace System.Diagnostics.CodeAnalysis;

// Only compile these attributes if the target framework is older than .NET 5.0
// (e.g., netstandard2.0, net48), as they are missing from the BCL there.
#if !NET5_0_OR_GREATER

/// <summary>
/// Indicates that the specified method requires dynamic access to code that is not referenced statically,
/// for example, through <see cref="Reflection"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Architectural Note:</strong>
/// This attribute is a "Polyfill". It allows libraries targeting older frameworks (like netstandard2.0)
/// to provide hints to the modern .NET Trimmer (ILLink) and NativeAOT compiler.
/// </para>
/// <para>
/// When a consumer uses <c>FluentJson</c> in an AOT application, calls to methods decorated with this attribute
/// will generate a warning, alerting them that they must manually preserve the types being reflected upon.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
internal sealed class RequiresUnreferencedCodeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequiresUnreferencedCodeAttribute"/> class
    /// with the specified message.
    /// </summary>
    /// <param name="message">
    /// A message containing information about the usage of unreferenced code.
    /// </param>
    public RequiresUnreferencedCodeAttribute(string message)
    {
        Message = message;
    }

    /// <summary>
    /// Gets a message containing information about the usage of unreferenced code.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets or sets an optional URL that contains more information about the method,
    /// why it requires unreferenced code, and what options a consumer has to deal with it.
    /// </summary>
    public string? Url { get; set; }
}

#endif
