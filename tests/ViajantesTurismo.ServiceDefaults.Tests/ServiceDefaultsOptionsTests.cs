using Microsoft.Extensions.Hosting;

namespace ViajantesTurismo.ServiceDefaults.Tests;

[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ServiceDefaultsArea)]
public static class ServiceDefaultsOptionsTests
{
    [Fact]
    public static void Host_defaults_keep_the_plain_shared_resilience_handler()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        // Act
        HostDefaults.Add(builder);
        using var host = HostDefaults.Build(builder);
        var options = host.Options;

        // Assert
        options.AddResilience.ShouldBeTrue();
        options.ConfigureResilience.ShouldBeNull();
        options.SensitiveTelemetryAttributes.ShouldContain("customer.email");
    }
}
