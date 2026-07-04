using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
}
