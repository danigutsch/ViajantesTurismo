using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PandaBear.ServiceDefaults;

namespace ViajantesTurismo.ServiceDefaults.Tests;

[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.ServiceDefaultsArea)]
public static class ServiceDefaultsOptionsTests
{
    [Fact]
    public static void Host_defaults_keep_the_plain_shared_resilience_handler()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        // Act
        HostDefaults.Add(builder);
        using var host = builder.Build();
        var options = host.Services.GetRequiredService<ServiceDefaultsOptions>();

        // Assert
        options.AddResilience.ShouldBeTrue();
        options.ConfigureResilience.ShouldBeNull();
        options.SensitiveTelemetryAttributes.ShouldContain("customer.email");
    }
}
