using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace SharedKernel.Configuration;

/// <summary>
/// Provides configuration-related dependency injection helpers.
/// </summary>
public static class OptionsServiceCollectionExtensions
{
    /// <summary>
    /// Binds an options type, registers its validator, registers the validated value, and validates it during application startup.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The configuration section to bind.</param>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <typeparam name="TValidator">The validator type.</typeparam>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddValidatedOptions<TOptions, TValidator>(
        this IServiceCollection? services,
        IConfiguration? configuration)
        where TOptions : class
        where TValidator : class, IValidateOptions<TOptions>
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<TOptions>(configuration);
        services.AddOptions<TOptions>().ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<TOptions>, TValidator>());
        services.TryAddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<TOptions>>().Value);

        return services;
    }
}
