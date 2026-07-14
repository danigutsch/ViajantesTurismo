namespace ViajantesTurismo.Admin.IntegrationTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.IntegrationScope)]
public sealed class AdminApiEndpointTests(ApiFixture fixture)
{
    [Fact]
    public async Task Robots_txt_disallows_admin_api_crawling()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        using var response = await fixture.Client.GetAsync(new Uri("/robots.txt", UriKind.Relative), cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/plain");
        response.Content.Headers.ContentType?.CharSet.ShouldBe("utf-8");
        body.ShouldBe("User-agent: *\nDisallow: /");
    }

    [Fact]
    public async Task Protected_admin_routes_reject_anonymous_callers()
    {
        // Arrange
        using var client = fixture.CreateAnonymousClient();

        // Act
        using var response = await client.GetAsync(
            new Uri($"/api/v1/tours/{Guid.NewGuid()}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
