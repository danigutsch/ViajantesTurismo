using ViajantesTurismo.Admin.Contracts.Http;

namespace ViajantesTurismo.Admin.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.SecurityCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, SharedKernel.Testing.TestTraitValues.TestServerHost)]
public sealed class ErrorDocumentationCacheTests
{
    private static readonly Uri Catalog = new("/api/v1/docs/errors", UriKind.Relative);

    [Fact]
    public async Task Catalog_and_entry_allow_only_private_revalidation()
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var list = await client.GetAsync(Catalog, ct);
        list.EnsureSuccessStatusCode();
        var entries = await list.Content.ReadFromJsonAsync<GetErrorDocumentationDto[]>(ct);
        var entryUri = new Uri($"{Catalog}/{Uri.EscapeDataString(entries.ShouldNotBeNull().First().Identifier)}", UriKind.Relative);
        using var entry = await client.GetAsync(entryUri, ct);
        entry.EnsureSuccessStatusCode();

        // Assert
        foreach (var response in new[] { list, entry })
        {
            var cache = response.Headers.CacheControl.ShouldNotBeNull();
            cache.Private.ShouldBeTrue();
            cache.NoCache.ShouldBeTrue();
            cache.Public.ShouldBeFalse();
            response.Headers.ETag.ShouldNotBeNull();
        }

        list.Headers.ETag.ShouldNotBe(entry.Headers.ETag);

        using var conditional = new HttpRequestMessage(HttpMethod.Get, entryUri);
        conditional.Headers.IfNoneMatch.Add(entry.Headers.ETag.ShouldNotBeNull());
        using var unchanged = await client.SendAsync(conditional, ct);
        unchanged.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        unchanged.Headers.ETag.ShouldBe(entry.Headers.ETag);
        unchanged.Headers.CacheControl.ShouldNotBeNull().NoCache.ShouldBeTrue();
        var body = await unchanged.Content.ReadAsStringAsync(ct);
        body.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("*", HttpStatusCode.NotModified)]
    [InlineData("\"different-version\"", HttpStatusCode.OK)]
    public async Task Catalog_evaluates_wildcard_and_unmatched_validators(string tag, HttpStatusCode expected)
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        using var request = new HttpRequestMessage(HttpMethod.Get, Catalog);
        request.Headers.IfNoneMatch.ParseAdd(tag);

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(expected);
    }

    [Fact]
    public async Task Revalidation_cannot_bypass_authorization_or_match_a_missing_entry()
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create();
        using var anonymousClient = factory.CreateClient();
        using var adminClient = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(adminClient, "Admin");
        var ct = TestContext.Current.CancellationToken;
        using var unauthorizedRequest = new HttpRequestMessage(HttpMethod.Get, Catalog);
        using var missingRequest = new HttpRequestMessage(HttpMethod.Get, new Uri($"{Catalog}/unknown", UriKind.Relative));
        unauthorizedRequest.Headers.IfNoneMatch.ParseAdd("*");
        missingRequest.Headers.IfNoneMatch.ParseAdd("*");

        // Act
        using var unauthorized = await anonymousClient.SendAsync(unauthorizedRequest, ct);
        using var missing = await adminClient.SendAsync(missingRequest, ct);

        // Assert
        unauthorized.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        unauthorized.Headers.ETag.ShouldBeNull();
        missing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        missing.Headers.ETag.ShouldBeNull();
        missing.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
    }
}
