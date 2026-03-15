using System;
using Microsoft.Extensions.DependencyInjection;
using FluentJson.Core.Builder;
using FluentJson.Core.Metadata;

namespace FluentJson.DependencyInjection;

/// <summary>
/// Extension methods for registering FluentJson with standard Microsoft Dependency Injection containers.
/// </summary>
public static class FluentJsonServiceCollectionExtensions
{
    /// <summary>
    /// Registers the FluentJson ecosystem into the service collection.
    /// </summary>
    /// <param name="services">The core DI container.</param>
    /// <param name="configure">A delegate to configure the global <see cref="JsonModelBuilder"/>.</param>
    /// <returns>The original service collection, enabling chaining.</returns>
    /// <remarks>
    /// This method compiles the <see cref="JsonModelBuilder"/> into a singleton <see cref="IJsonModel"/> and registers it in the container for adapter injection.
    /// </remarks>
    public static IServiceCollection AddFluentJson(this IServiceCollection services, Action<JsonModelBuilder> configure)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var builder = new JsonModelBuilder();
        configure(builder);
        var model = builder.Build();

        services.AddSingleton<IJsonModel>(model);

        return services;
    }
}
