using System;

namespace FluentJson.Core.Exceptions;

/// <summary>
/// Represents errors that occur during the configuration and compilation phases of FluentJson.
/// </summary>
public class FluentJsonConfigurationException : Exception
{
    public FluentJsonConfigurationException(string message) : base(message)
    {
    }

    public FluentJsonConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
