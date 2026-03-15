using System;
using Newtonsoft.Json;
using FluentJson.Core.Metadata;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FluentJson.Newtonsoft.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace FluentJson.Newtonsoft;

/// <summary>
/// Provides extension methods to easily integrate FluentJson into Newtonsoft.Json's native settings.
/// </summary>
public static class FluentJsonSerializerSettingsExtensions
{
    /// <summary>
    /// Injects FluentJson's configuration into the provided <see cref="JsonSerializerSettings"/>.
    /// </summary>
    /// <param name="settings">The Newtonsoft settings object to mutate.</param>
    /// <param name="model">The compiled FluentJson metadata model.</param>
    /// <returns>The mutated settings instance for method chaining.</returns>
    /// <remarks>
    /// This method replaces the active <see cref="Newtonsoft.Json.Serialization.IContractResolver"/> with a <see cref="FluentJsonContractResolver"/>.
    /// </remarks>
    public static JsonSerializerSettings AddFluentJson(this JsonSerializerSettings settings, IJsonModel model)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));

        settings.ContractResolver = new FluentJsonContractResolver(model);
        return settings;
    }
}
