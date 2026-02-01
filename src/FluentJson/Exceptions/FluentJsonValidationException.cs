namespace FluentJson.Exceptions;

/// <summary>
/// Thrown by the validator when the configuration model violates consistency rules.
/// Examples: Duplicate discriminator values, conflicting property mappings, or illogical constraints.
/// </summary>
public sealed class FluentJsonValidationException(string message)
    : FluentJsonException(message);
