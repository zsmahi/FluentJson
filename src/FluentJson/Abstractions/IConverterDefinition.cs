namespace FluentJson.Abstractions;

/// <summary>
/// Serves as the polymorphic root for defining JSON conversion strategies within the configuration model.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Strategy / Marker Interface.
/// </para>
/// <para>
/// This interface decouples the core definition storage (<see cref="FluentJson.Definitions.JsonPropertyDefinition"/>) 
/// from the concrete implementation of data conversion. It allows the library to support multiple 
/// conversion mechanisms (e.g., <see cref="TypeConverterDefinition"/> for <c>System.Type</c> based converters 
/// and <see cref="LambdaConverterDefinition"/> for inline delegate-based converters) seamlessly.
/// </para>
/// <para>
/// <strong>Architectural Note:</strong>
/// Being a marker interface (empty contract), it provides compile-time type safety for the configuration container 
/// while imposing no implementation constraints on the conversion definitions.
/// </para>
/// </remarks>
public interface IConverterDefinition
{
}
