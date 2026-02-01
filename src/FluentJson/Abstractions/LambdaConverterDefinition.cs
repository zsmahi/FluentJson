using System;

namespace FluentJson.Abstractions;

/// <summary>
/// Represents the type-agnostic base contract for lambda-based conversion strategies.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Type Erasure / Template Method.
/// </para>
/// <para>
/// This abstract class allows the generic configuration model to be stored in heterogeneous collections 
/// (e.g., <c>Dictionary&lt;MemberInfo, IConverterDefinition&gt;</c>) where the specific generic arguments 
/// <c>TModel</c> and <c>TJson</c> are not known or vary between properties.
/// </para>
/// <para>
/// <strong>Architectural Note:</strong>
/// It exposes the raw <see cref="Delegate"/> and <see cref="Type"/> metadata, enabling the concrete serializer adapters 
/// (Newtonsoft/STJ) to construct the appropriate generic converters at runtime via reflection, without breaking strong typing in the domain configuration.
/// </para>
/// </remarks>
public abstract class LambdaConverterDefinition : IConverterDefinition
{
    /// <summary>
    /// Gets the CLR type of the domain model property.
    /// </summary>
    public abstract Type ModelType { get; }

    /// <summary>
    /// Gets the CLR type of the intermediate JSON representation (surrogate).
    /// </summary>
    public abstract Type JsonType { get; }

    /// <summary>
    /// Gets the delegate responsible for serializing the model to the JSON surrogate.
    /// </summary>
    public abstract Delegate ConvertToDelegate { get; }

    /// <summary>
    /// Gets the delegate responsible for deserializing the JSON surrogate back to the model.
    /// </summary>
    public abstract Delegate ConvertFromDelegate { get; }
}

/// <summary>
/// A strongly-typed container capturing the conversion logic between a domain model and a JSON surrogate.
/// </summary>
/// <typeparam name="TModel">The type of the property in the domain entity.</typeparam>
/// <typeparam name="TJson">The intermediate type used for JSON serialization (e.g., string, int).</typeparam>
/// <remarks>
/// <para>
/// <strong>Performance Note:</strong>
/// Unlike Expression Trees which might require compilation, this class captures pre-compiled 
/// <see cref="Func{T,TResult}"/> delegates directly from the user's code. This ensures zero-overhead 
/// during the setup phase and optimal execution speed during serialization.
/// </para>
/// </remarks>
public class LambdaConverterDefinition<TModel, TJson>(Func<TModel, TJson> convertTo, Func<TJson, TModel> convertFrom) : LambdaConverterDefinition
{
    /// <summary>
    /// Gets the strongly-typed function for serialization (Model -> JSON).
    /// </summary>
    public Func<TModel, TJson> ConvertTo { get; } = convertTo ?? throw new ArgumentNullException(nameof(convertTo));

    /// <summary>
    /// Gets the strongly-typed function for deserialization (JSON -> Model).
    /// </summary>
    public Func<TJson, TModel> ConvertFrom { get; } = convertFrom ?? throw new ArgumentNullException(nameof(convertFrom));

    /// <inheritdoc />
    public override Type ModelType => typeof(TModel);

    /// <inheritdoc />
    public override Type JsonType => typeof(TJson);

    /// <inheritdoc />
    public override Delegate ConvertToDelegate => ConvertTo;

    /// <inheritdoc />
    public override Delegate ConvertFromDelegate => ConvertFrom;
}
