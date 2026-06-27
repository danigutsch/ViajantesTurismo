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
    /// Adds an options type, registers its validator, and validates it during application startup.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <typeparam name="TValidator">The validator type.</typeparam>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddValidatedOptions<TOptions, TValidator>(this IServiceCollection services)
        where TOptions : class
        where TValidator : class, IValidateOptions<TOptions>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<TOptions>().ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<TOptions>, TValidator>());

        return services;
    }
}
