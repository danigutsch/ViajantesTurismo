namespace ViajantesTurismo.Admin.IntegrationTests.Documents;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.IntegrationScope)]
public sealed class DocumentApiAuthorizationTests(ApiFixture fixture)
{
    [Theory]
    [MemberData(nameof(DocumentApiRouteCases.All), MemberType = typeof(DocumentApiRouteCases))]
    public async Task Document_routes_reject_anonymous_callers(string method, string route)
    {
        // Arrange
        using var client = fixture.CreateAnonymousClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), new Uri(route, UriKind.Relative));

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(DocumentApiRouteCases.All), MemberType = typeof(DocumentApiRouteCases))]
    public async Task Authenticated_operator_cannot_manage_document_routes(string method, string route)
    {
        // Arrange
        using var client = await fixture.CreateOperatorClient(TestContext.Current.CancellationToken);
        using var request = new HttpRequestMessage(new HttpMethod(method), new Uri(route, UriKind.Relative));

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
