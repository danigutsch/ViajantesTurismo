using System.Net;
using Microsoft.Extensions.Hosting;
using SharedKernel.Testing;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.HostName, TestTraitValues.TestServerHost)]
public sealed class ForwardedHeadersHostTests
{
    [Fact]
    public async Task Trusted_loopback_forwarded_https_request_avoids_https_redirect()
    {
        // Arrange
        var configuration = ManagementAuthenticationTestConfiguration.CreateSettings();
        configuration["https_port"] = "443";
        configuration["Security:ForwardedHeaders:KnownProxies:0"] = "127.0.0.1";
        await using var factory = WebApplicationTestHost.Create<CustomerCreationState>(
            environment: Environments.Development,
            configuration: configuration);
        using var client = new HttpClient(factory.Server.CreateHandler(
            context => context.Connection.RemoteIpAddress = IPAddress.Loopback))
        {
            BaseAddress = new Uri("http://localhost")
        };
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/alive", UriKind.Relative));
        request.Headers.Add("X-Forwarded-Proto", "https");

        // Act
        using var response = await client.SendAsync(request, Xunit.TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Configured_untrusted_loopback_forwarded_https_request_remains_redirected()
    {
        // Arrange
        var configuration = ManagementAuthenticationTestConfiguration.CreateSettings();
        configuration["https_port"] = "443";
        configuration["Security:ForwardedHeaders:KnownProxies:0"] = "192.0.2.1";
        await using var factory = WebApplicationTestHost.Create<CustomerCreationState>(
            environment: Environments.Development,
            configuration: configuration);
        using var client = new HttpClient(factory.Server.CreateHandler(
            context => context.Connection.RemoteIpAddress = IPAddress.Loopback))
        {
            BaseAddress = new Uri("http://localhost")
        };
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/alive", UriKind.Relative));
        request.Headers.Add("X-Forwarded-Proto", "https");

        // Act
        using var response = await client.SendAsync(request, Xunit.TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.TemporaryRedirect);
    }
}
