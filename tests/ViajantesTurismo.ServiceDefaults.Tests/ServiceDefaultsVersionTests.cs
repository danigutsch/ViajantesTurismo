using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;

namespace ViajantesTurismo.ServiceDefaults.Tests;

public static class ServiceDefaultsVersionTests
{
    [Fact]
    public static void Add_service_defaults_wires_shared_application_version_logging()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        // Act
        builder.AddServiceDefaults();
        using var host = builder.Build();
        var hostedServices = host.Services.GetServices<IHostedService>();

        // Assert
        hostedServices.ShouldContain(service => string.Equals(
            service.GetType().Name,
            "ApplicationVersionLoggingService",
            StringComparison.Ordinal));
        hostedServices.ShouldContain(service => string.Equals(
            service.GetType().Namespace,
            "SharedKernel.Observability",
            StringComparison.Ordinal));
    }

    [Fact]
    public static void Explicitly_disabled_observability_preserves_health_checks()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        // Act
        builder.AddServiceDefaults(includeObservabilityAndServiceDiscovery: false);
        using var host = builder.Build();
        var healthCheckService = host.Services.GetService<HealthCheckService>();
        var tracerProvider = host.Services.GetService<TracerProvider>();
        var hostedServices = host.Services.GetServices<IHostedService>();

        // Assert
        healthCheckService.ShouldNotBeNull();
        tracerProvider.ShouldBeNull();
        hostedServices.ShouldNotContain(service => string.Equals(
            service.GetType().Name,
            "ApplicationVersionLoggingService",
            StringComparison.Ordinal));
    }
}
