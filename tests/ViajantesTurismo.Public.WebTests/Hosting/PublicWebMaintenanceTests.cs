using Microsoft.AspNetCore.Mvc.Testing;
using ViajantesTurismo.Resources;
using TestTraits = ViajantesTurismo.Public.WebTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Public.WebTests.Hosting;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.HostName, TestTraits.TestServerHost)]
[TestScope(TestScope.Integration)]
public sealed class PublicWebMaintenanceTests
{
    [Fact]
    public async Task Maintenance_serves_an_uncacheable_page_before_the_usual_redirects()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var factory = PublicWebTestHost.CreateFactory(configuration: new Dictionary<string, string?>
        {
            [HttpMaintenanceConfiguration.EnabledKey] = "true"
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://testserver")
        });

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("text/html");
        body.ShouldContain("<h1>Scheduled maintenance</h1>", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/health", HttpStatusCode.ServiceUnavailable, "Unhealthy")]
    [InlineData("/alive", HttpStatusCode.OK, "Healthy")]
    public async Task Maintenance_fails_readiness_and_preserves_liveness(string path, HttpStatusCode expected, string status)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var factory = PublicWebTestHost.CreateFactory(configuration: new Dictionary<string, string?>
        {
            [HttpMaintenanceConfiguration.EnabledKey] = "true"
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://testserver") });

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // Assert
        response.StatusCode.ShouldBe(expected);
        body.ShouldBe(status);
    }

    [Fact]
    public async Task An_invalid_maintenance_switch_stops_host_startup()
    {
        // Arrange
        await using var factory = PublicWebTestHost.CreateFactory(configuration: new Dictionary<string, string?>
        {
            [HttpMaintenanceConfiguration.EnabledKey] = "invalid"
        });
        Action start = () => factory.CreateClient();

        // Act
        var error = start.ShouldThrow<InvalidOperationException>();

        // Assert
        error.Message.ShouldContain(HttpMaintenanceConfiguration.EnabledKey, StringComparison.Ordinal);
    }
}
