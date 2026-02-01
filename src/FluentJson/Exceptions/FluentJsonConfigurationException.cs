using System;

namespace FluentJson.Exceptions;

/// <summary>
/// Thrown when an error occurs during the configuration discovery or instantiation phase.
/// Typically caused by missing constructors, dependency injection failures, or invalid type hierarchies.
/// </summary>
public sealed class FluentJsonConfigurationException : FluentJsonException
{
    public FluentJsonConfigurationException(string message) : base(message) { }
    public FluentJsonConfigurationException(string message, Exception innerException) : base(message, innerException) { }
}
