using Microsoft.Extensions.Configuration;
using SharedKernel.Testing;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Branding;

[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
[Trait(TestTraitNames.AreaName, TestAreas.Branding)]
public sealed class BrandingLogoCorsEndpointTests
{
    private const string AllowedOrigin = "https://management.example";
    private const string DeniedOrigin = "https://unlisted.example";

    [Theory]
    [InlineData("PUT", AllowedOrigin, true)]
    [InlineData("DELETE", AllowedOrigin, true)]
    [InlineData("PUT", DeniedOrigin, false)]
    [InlineData("DELETE", DeniedOrigin, false)]
    public async Task Logo_mutation_preflight_respects_the_origin_allowlist(string method, string origin, bool isAllowed)
    {
        // Arrange
        await using var root = BrandingApiTestHost.CreateAnonymous();
        await using var factory = root.WithWebHostBuilder(host => host.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Security:Cors:AllowedOrigins:0"] = AllowedOrigin })));
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, new Uri(BrandingLogoRequests.ManagementLogoPath, UriKind.Relative));
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", method);
        request.Headers.Add("Access-Control-Request-Headers", "authorization,if-match");

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBe(isAllowed);
        response.Headers.Contains("Access-Control-Allow-Credentials").ShouldBeFalse();
        if (isAllowed)
        {
            response.Headers.GetValues("Access-Control-Allow-Origin").ShouldHaveSingleItem().ShouldBe(origin);
            response.Headers.GetValues("Access-Control-Allow-Methods").ShouldHaveSingleItem().ShouldBe(method);
        }
    }
}
