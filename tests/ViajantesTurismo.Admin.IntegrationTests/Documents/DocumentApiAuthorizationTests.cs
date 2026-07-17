namespace ViajantesTurismo.Admin.IntegrationTests.Documents;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.IntegrationScope)]
public sealed class DocumentApiAuthorizationTests(ApiFixture fixture)
{
    [Fact]
    public async Task Document_routes_reject_anonymous_callers()
    {
        // Arrange
        using var client = fixture.CreateAnonymousClient();

        // Act
        using var response = await client.GetAsync(
            new Uri($"/api/v1/documents/{Guid.NewGuid()}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Authenticated_operator_cannot_download_document_artifacts()
    {
        // Arrange
        using var client = await fixture.CreateOperatorClient(TestContext.Current.CancellationToken);

        // Act
        using var response = await client.GetAsync(
            new Uri($"/api/v1/documents/{Guid.CreateVersion7()}/download", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
