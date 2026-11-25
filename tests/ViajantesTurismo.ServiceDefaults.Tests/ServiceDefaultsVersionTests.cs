using Microsoft.Extensions.Hosting;

namespace ViajantesTurismo.ServiceDefaults.Tests;

public static class ServiceDefaultsVersionTests
{
    [Fact]
    public static void Host_defaults_wire_shared_application_version_logging()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        // Act
        HostDefaults.Add(builder);
        using var host = HostDefaults.Build(builder);
        var hostedServices = host.HostedServices;

        // Assert
        hostedServices.ShouldContain(service =>
            string.Equals(service.GetType().Name, "ApplicationVersionLoggingService", StringComparison.Ordinal)
            && string.Equals(service.GetType().Namespace, "SharedKernel.Observability", StringComparison.Ordinal));
    }
}
