using System;

namespace FluentJson.Core.Exceptions;

/// <summary>
/// Represents errors that occur when an invalid lambda expression is passed to the Fluent API.
/// </summary>
/// <remarks>
/// Typically thrown when attempting to map nested properties or method calls instead of simple member access.
/// </remarks>
public class FluentJsonExpressionException : FluentJsonConfigurationException
{
    public FluentJsonExpressionException(string message) : base(message)
    {
    }

    public FluentJsonExpressionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
