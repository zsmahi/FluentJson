using System;

namespace FluentJson.Exceptions;

/// <summary>
/// Serves as the base class for all exceptions thrown by the FluentJson library.
/// Enables global catching strategies for library-specific errors.
/// </summary>
public abstract class FluentJsonException : Exception
{
    protected FluentJsonException(string message) : base(message) { }
    protected FluentJsonException(string message, Exception innerException) : base(message, innerException) { }
}
