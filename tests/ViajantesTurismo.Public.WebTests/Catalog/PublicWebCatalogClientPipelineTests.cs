using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using TestTraits = ViajantesTurismo.Public.WebTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Public.WebTests.Catalog;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.HostName, TestTraits.TestServerHost)]
[TestScope(TestScope.Integration)]
public sealed class PublicWebCatalogClientPipelineTests
{
    private const string CatalogClientName = nameof(IPublicCatalogApiClient);
    private const string ResolvingHandlerTypeName = "Microsoft.Extensions.ServiceDiscovery.Http.ResolvingHttpDelegatingHandler";

    [Fact]
    public async Task Catalog_client_resolves_the_address_once_after_the_resilience_handler()
    {
        // Arrange
        await using var factory = PublicWebTestHost.CreateFactory();
        var handlers = factory.Services.GetRequiredService<IHttpMessageHandlerFactory>();

        // Act
        var chain = PublicWebCatalogClientPipelineTestsHelpers.HandlerTypeNames(handlers.CreateHandler(CatalogClientName));

        // Assert
        var resilience = chain.IndexOf(typeof(ResilienceHandler).FullName!);
        resilience.ShouldBeGreaterThanOrEqualTo(0);
        chain.Count(static name => name == ResolvingHandlerTypeName).ShouldBe(1);
        chain.IndexOf(ResolvingHandlerTypeName).ShouldBeGreaterThan(resilience);
    }

    [Fact]
    public async Task Catalog_client_does_not_retry_a_public_catalog_quota_answer()
    {
        // Arrange
        const string quotaPath = "/api/v1/public/catalog/tours";
        using var catalog = new RecordingHttpMessageHandler(HttpStatusCode.TooManyRequests);
        await using var host = PublicWebTestHost.CreateFactory(
            configuration: new Dictionary<string, string?> { ["services:catalog-api:https:0"] = "https://catalog.test" });
        await using var factory = host.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.ConfigureHttpClientDefaults(http => http.ConfigurePrimaryHttpMessageHandler(() => catalog))));
        using var client = factory.Services.GetRequiredService<IHttpClientFactory>().CreateClient(CatalogClientName);

        // Act
        using var response = await client.GetAsync(new Uri(quotaPath, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        catalog.Requests.ShouldHaveSingleItem(request => request.PathAndQuery == quotaPath);
    }
}
