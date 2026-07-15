using System.Net;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>
/// Verifies the Management BFF exchanges one server-side token for each backend audience.
/// </summary>
public sealed class AudienceTokenExchangeHandlerTests
{
    [Fact]
    public async Task Exchanges_one_token_per_backend_audience_without_forwarding_the_source_token()
    {
        // Arrange
        await using var host = AudienceTokenExchangeTestHost.Create();
        using var adminExchangeHandler = host.CreateHandler(ApiAudienceNames.Admin);
        using var catalogExchangeHandler = host.CreateHandler(ApiAudienceNames.Catalog);
        using var brandingExchangeHandler = host.CreateHandler(ApiAudienceNames.Branding);
        using var adminSourceHandler = new SourceAccessTokenHandler("source-token") { InnerHandler = adminExchangeHandler };
        using var catalogSourceHandler = new SourceAccessTokenHandler("source-token") { InnerHandler = catalogExchangeHandler };
        using var brandingSourceHandler = new SourceAccessTokenHandler("source-token") { InnerHandler = brandingExchangeHandler };
        using var adminClient = new HttpMessageInvoker(adminSourceHandler, disposeHandler: false);
        using var catalogClient = new HttpMessageInvoker(catalogSourceHandler, disposeHandler: false);
        using var brandingClient = new HttpMessageInvoker(brandingSourceHandler, disposeHandler: false);

        // Act
        using var adminResponse = await adminClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://admin.example.test/"), Xunit.TestContext.Current.CancellationToken);
        using var catalogResponse = await catalogClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://catalog.example.test/"), Xunit.TestContext.Current.CancellationToken);
        using var brandingResponse = await brandingClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://branding.example.test/"), Xunit.TestContext.Current.CancellationToken);

        // Assert
        adminResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        catalogResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        brandingResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        host.TokenEndpoint.Requests.Select(static request => request["audience"])
            .ShouldBe([ApiAudienceNames.Admin, ApiAudienceNames.Catalog, ApiAudienceNames.Branding]);
        host.TokenEndpoint.Requests.Select(static request => request["scope"])
            .ShouldBe([ApiAudienceNames.Admin, ApiAudienceNames.Catalog, ApiAudienceNames.Branding]);
        host.TokenEndpoint.Requests.Select(static request => request["subject_token"])
            .ShouldBe(["source-token", "source-token", "source-token"]);
        host.TokenEndpoint.Requests.Select(static request => request["subject_token_type"])
            .ShouldBe([
                "urn:ietf:params:oauth:token-type:access_token",
                "urn:ietf:params:oauth:token-type:access_token",
                "urn:ietf:params:oauth:token-type:access_token"
            ]);
        host.TokenEndpoint.Requests.Select(static request => request["requested_token_type"])
            .ShouldBe([
                "urn:ietf:params:oauth:token-type:access_token",
                "urn:ietf:params:oauth:token-type:access_token",
                "urn:ietf:params:oauth:token-type:access_token"
            ]);
        host.Backend.AuthorizationHeaders.ShouldBe([
            "Bearer token-for-admin-api",
            "Bearer token-for-catalog-api",
            "Bearer token-for-branding-api"
        ]);
        host.Backend.AuthorizationHeaders.ShouldNotContain("Bearer source-token");
    }

    [Fact]
    public async Task Reuses_a_valid_exchanged_token_from_the_protected_server_side_cache()
    {
        // Arrange
        await using var host = AudienceTokenExchangeTestHost.Create();
        using var exchangeHandler = host.CreateHandler(ApiAudienceNames.Admin);
        using var sourceHandler = new SourceAccessTokenHandler("source-token") { InnerHandler = exchangeHandler };
        using var client = new HttpMessageInvoker(sourceHandler, disposeHandler: false);

        // Act
        using var firstResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://admin.example.test/first"), Xunit.TestContext.Current.CancellationToken);
        using var secondResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://admin.example.test/second"), Xunit.TestContext.Current.CancellationToken);

        // Assert
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        host.TokenEndpoint.Requests.Count.ShouldBe(1);
        host.Backend.AuthorizationHeaders.ShouldBe(["Bearer token-for-admin-api", "Bearer token-for-admin-api"]);
    }

    [Fact]
    public async Task Rejects_an_invalid_exchange_response_without_forwarding_the_source_token()
    {
        // Arrange
        await using var host = AudienceTokenExchangeTestHost.Create();
        host.TokenEndpoint.ResponseFactory = static _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"access_token\":\"token-for-admin-api\",\"expires_in\":300,\"token_type\":\"Bearer\",\"issued_token_type\":\"refresh_token\"}",
                System.Text.Encoding.UTF8,
                "application/json")
        };
        using var exchangeHandler = host.CreateHandler(ApiAudienceNames.Admin);
        using var sourceHandler = new SourceAccessTokenHandler("source-token") { InnerHandler = exchangeHandler };
        using var client = new HttpMessageInvoker(sourceHandler, disposeHandler: false);

        // Act
        Func<Task> action = async () =>
        {
            using var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://admin.example.test/"), Xunit.TestContext.Current.CancellationToken);
        };

        // Assert
        await action.ShouldThrow<InvalidOperationException>();
        host.Backend.AuthorizationHeaders.ShouldBeEmpty();
    }

    [Fact]
    public async Task Rejects_an_exchange_response_that_returns_the_source_token()
    {
        // Arrange
        await using var host = AudienceTokenExchangeTestHost.Create();
        host.TokenEndpoint.ResponseFactory = static _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"access_token\":\"source-token\",\"expires_in\":300,\"token_type\":\"Bearer\",\"issued_token_type\":\"urn:ietf:params:oauth:token-type:access_token\"}",
                System.Text.Encoding.UTF8,
                "application/json")
        };
        using var exchangeHandler = host.CreateHandler(ApiAudienceNames.Admin);
        using var sourceHandler = new SourceAccessTokenHandler("source-token") { InnerHandler = exchangeHandler };
        using var client = new HttpMessageInvoker(sourceHandler, disposeHandler: false);

        // Act
        Func<Task> action = async () =>
        {
            using var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://admin.example.test/"), Xunit.TestContext.Current.CancellationToken);
        };

        // Assert
        await action.ShouldThrow<InvalidOperationException>();
        host.Backend.AuthorizationHeaders.ShouldBeEmpty();
    }

    [Fact]
    public async Task Rejects_an_exchanged_token_cache_entry_transplanted_from_another_source_token()
    {
        // Arrange
        await using var host = AudienceTokenExchangeTestHost.Create();
        host.TokenEndpoint.ResponseFactory = static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"access_token\":\"token-for-" + request["subject_token"] + "\",\"expires_in\":300,\"token_type\":\"Bearer\",\"issued_token_type\":\"urn:ietf:params:oauth:token-type:access_token\"}",
                System.Text.Encoding.UTF8,
                "application/json")
        };
        using var firstExchangeHandler = host.CreateHandler(ApiAudienceNames.Admin);
        using var firstSourceHandler = new SourceAccessTokenHandler("source-token-a") { InnerHandler = firstExchangeHandler };
        using var firstClient = new HttpMessageInvoker(firstSourceHandler, disposeHandler: false);

        // Act
        using var firstResponse = await firstClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://admin.example.test/first"), Xunit.TestContext.Current.CancellationToken);
        var transplantedValue = await host.Cache.GetAsync(
            AudienceTokenExchangeTestHost.GetAudienceTokenCacheKey(ApiAudienceNames.Admin, "source-token-a"),
            Xunit.TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("The source audience-token entry was not stored.");
        await host.Cache.SetAsync(
            AudienceTokenExchangeTestHost.GetAudienceTokenCacheKey(ApiAudienceNames.Admin, "source-token-b"),
            transplantedValue,
            new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions(),
            Xunit.TestContext.Current.CancellationToken);
        using var secondExchangeHandler = host.CreateHandler(ApiAudienceNames.Admin);
        using var secondSourceHandler = new SourceAccessTokenHandler("source-token-b") { InnerHandler = secondExchangeHandler };
        using var secondClient = new HttpMessageInvoker(secondSourceHandler, disposeHandler: false);
        using var secondResponse = await secondClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://admin.example.test/second"), Xunit.TestContext.Current.CancellationToken);

        // Assert
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        host.TokenEndpoint.Requests.Count.ShouldBe(2);
        host.Backend.AuthorizationHeaders.ShouldBe(["Bearer token-for-source-token-a", "Bearer token-for-source-token-b"]);
    }

    [Fact]
    public async Task Does_not_forward_a_cached_token_that_equals_the_source_token()
    {
        // Arrange
        await using var host = AudienceTokenExchangeTestHost.Create();
        await host.StoreProtectedAudienceTokenEntry(
            ApiAudienceNames.Admin,
            "source-token",
            "source-token",
            DateTimeOffset.UtcNow.AddMinutes(5),
            Xunit.TestContext.Current.CancellationToken);
        using var exchangeHandler = host.CreateHandler(ApiAudienceNames.Admin);
        using var sourceHandler = new SourceAccessTokenHandler("source-token") { InnerHandler = exchangeHandler };
        using var client = new HttpMessageInvoker(sourceHandler, disposeHandler: false);

        // Act
        using var response = await client.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://admin.example.test/"),
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        host.TokenEndpoint.Requests.Count.ShouldBe(1);
        host.Backend.AuthorizationHeaders.ShouldBe(["Bearer token-for-admin-api"]);
        host.Backend.AuthorizationHeaders.ShouldNotContain("Bearer source-token");
    }

    [Fact]
    public async Task Exchanges_a_lowercase_bearer_source_token()
    {
        // Arrange
        await using var host = AudienceTokenExchangeTestHost.Create();
        using var exchangeHandler = host.CreateHandler(ApiAudienceNames.Admin);
        using var sourceHandler = new SourceAccessTokenHandler("source-token", "bearer") { InnerHandler = exchangeHandler };
        using var client = new HttpMessageInvoker(sourceHandler, disposeHandler: false);

        // Act
        using var response = await client.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://admin.example.test/"),
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        host.TokenEndpoint.Requests.Count.ShouldBe(1);
        host.Backend.AuthorizationHeaders.ShouldBe(["Bearer token-for-admin-api"]);
    }
}
