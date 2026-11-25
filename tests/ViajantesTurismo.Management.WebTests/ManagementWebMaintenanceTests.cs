using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using SharedKernel.Testing;
using ViajantesTurismo.Management.Web.Components.Pages.Customers.Create;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Management.WebTests;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.HostName, TestTraitValues.TestServerHost)]
[TestScope(TestScope.Integration)]
public sealed class ManagementWebMaintenanceTests
{
    [Fact]
    public async Task Management_maintenance_page_requires_no_sign_in_and_preserves_security_headers()
    {
        // Arrange
        var ct = Xunit.TestContext.Current.CancellationToken;
        var configuration = ManagementAuthenticationTestConfiguration.CreateSettings();
        configuration[HttpMaintenanceConfiguration.EnabledKey] = "true";
        await using var factory = WebApplicationTestHost.Create<CustomerCreationState>(
            environment: Environments.Development,
            configuration: configuration);
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
        response.Headers.Contains("Content-Security-Policy").ShouldBeTrue();
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("text/html");
        body.ShouldContain("<h1>Scheduled maintenance</h1>", StringComparison.Ordinal);
    }
}
