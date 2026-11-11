using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PandaBear.ServiceDefaults;

namespace ViajantesTurismo.ServiceDefaults.Tests;

[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.ServiceDefaultsArea)]
public static class ServiceDefaultsOptionsTests
{
    [Fact]
    public static void Host_defaults_do_not_retry_every_http_client_request()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        // Act
        HostDefaults.Add(builder);
        using var host = builder.Build();
        var options = host.Services.GetRequiredService<ServiceDefaultsOptions>();

        // Assert
        options.AddResilience.ShouldBeFalse();
        options.SensitiveTelemetryAttributes.ShouldContain("customer.email");
    }
}
