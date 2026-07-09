using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.ServiceDefaults.Tests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ApiIntegrationScope)]
[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ServiceDefaultsArea)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class ServiceDefaultsHealthEndpointTests
{
    [Fact]
    public async Task Map_default_endpoints_exposes_production_safe_health_checks()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });
        builder.WebHost.UseTestServer();
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty;
        builder.AddServiceDefaults();

        await using var app = builder.Build();
        app.MapDefaultEndpoints();
        await app.StartAsync(TestContext.Current.CancellationToken);
        using var client = app.GetTestClient();

        // Act
        using var healthResponse = await client.GetAsync(
            new Uri(EndpointPaths.Health, UriKind.Relative),
            TestContext.Current.CancellationToken);
        var healthBody = await healthResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var aliveResponse = await client.GetAsync(
            new Uri(EndpointPaths.Aliveness, UriKind.Relative),
            TestContext.Current.CancellationToken);
        var aliveBody = await aliveResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        healthResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        healthResponse.Content.Headers.ContentType?.MediaType.ShouldBe("text/plain");
        healthBody.ShouldBe("Healthy");
        aliveResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        aliveResponse.Content.Headers.ContentType?.MediaType.ShouldBe("text/plain");
        aliveBody.ShouldBe("Healthy");
    }
}
