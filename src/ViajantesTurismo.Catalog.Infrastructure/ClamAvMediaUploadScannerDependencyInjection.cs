using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>
/// Provides dependency-injection registration for the ClamAV media upload scanner.
/// </summary>
internal static class ClamAvMediaUploadScannerDependencyInjection
{
    /// <summary>
    /// Adds the production ClamAV scanner.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddClamAvMediaUploadScanner(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<ClamAvMediaUploadScannerOptions>()
            .BindConfiguration(ClamAvMediaUploadScannerOptions.SectionName)
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IValidateOptions<ClamAvMediaUploadScannerOptions>,
                ClamAvMediaUploadScannerOptionsValidator>());
        services.AddSingleton<IMediaUploadScanner, ClamAvMediaUploadScanner>();
        services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics.AddMeter(ClamAvMediaUploadScannerTelemetry.Name))
            .WithTracing(tracing => tracing.AddSource(ClamAvMediaUploadScannerTelemetry.Name));

        return services;
    }
}
