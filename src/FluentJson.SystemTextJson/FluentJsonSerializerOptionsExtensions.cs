using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using FluentJson.Core.Metadata;

namespace FluentJson.SystemTextJson;

/// <summary>
/// Provides extension methods to easily integrate FluentJson into System.Text.Json's native options.
/// </summary>
public static class FluentJsonSerializerOptionsExtensions
{
    /// <summary>
    /// Injects FluentJson's configuration into the provided <see cref="JsonSerializerOptions"/>.
    /// </summary>
    /// <param name="options">The System.Text.Json options object to mutate.</param>
    /// <param name="model">The compiled FluentJson metadata model.</param>
    /// <returns>The mutated options instance for method chaining.</returns>
    /// <remarks>
    /// This method applies a <see cref="FluentJsonTypeModifier"/> to the TypeInfoResolver chain, allowing interception of the standard serialization pipeline.
    /// </remarks>
    public static JsonSerializerOptions AddFluentJson(this JsonSerializerOptions options, IJsonModel model)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        var modifier = new FluentJsonTypeModifier(model);

        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(modifier.Modify);

        options.TypeInfoResolver = resolver;

        return options;
    }
}
