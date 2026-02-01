using System;

namespace FluentJson.Exceptions;

/// <summary>
/// Thrown during the serialization or deserialization process when a runtime error occurs.
/// Wraps underlying errors from user-defined converters or accessors.
/// </summary>
public class FluentJsonRuntimeException : FluentJsonException
{
    public FluentJsonRuntimeException(string message) : base(message) { }
    public FluentJsonRuntimeException(string message, Exception innerException) : base(message, innerException) { }
}
