using PandaBear.Testing;
using ViajantesTurismo.Admin.Testing;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.ApiServiceTests.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, AdminTestTraitValues.GeneratedDocumentsCapability)]
[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
public sealed class DocumentHoldEndpointTests
{
    private const string ValidBody = """{"basis":"LegalClaim","purposeCode":"purpose.claim-001","authorityReference":"authority:case-0001","reviewDueAt":"2999-01-01T00:00:00Z"}""";

    [Theory]
    [InlineData("Admin", HttpStatusCode.Created)]
    [InlineData("Operator", HttpStatusCode.Forbidden)]
    [InlineData("Reader", HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    public async Task Only_the_hold_permission_may_place_a_hold(string? role, HttpStatusCode expected)
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        var holds = new InMemoryDispositionHoldStore();
        await using var factory = DocumentHoldTestHost.Create(documentId, holds);
        using var client = factory.CreateClient();
        if (role is not null)
        {
            AdminApiTestHost.ConfigureAuthenticatedClient(client, role);
        }

        using var content = new StringContent(ValidBody, System.Text.Encoding.UTF8, "application/json");

        // Act
        using var response = await client.PostAsync(new Uri($"/api/v1/documents/{documentId}/holds", UriKind.Relative), content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(expected);
        holds.Holds.Count.ShouldBe(expected == HttpStatusCode.Created ? 1 : 0);
    }

    [Theory]
    [InlineData("Admin", HttpStatusCode.OK)]
    [InlineData("Operator", HttpStatusCode.Forbidden)]
    [InlineData("Reader", HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    public async Task Only_the_hold_permission_may_list_holds(string? role, HttpStatusCode expected)
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        await using var factory = DocumentHoldTestHost.Create(documentId, new InMemoryDispositionHoldStore());
        using var client = factory.CreateClient();
        if (role is not null)
        {
            AdminApiTestHost.ConfigureAuthenticatedClient(client, role);
        }

        // Act
        using var response = await client.GetAsync(new Uri($"/api/v1/documents/{documentId}/holds", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Operator", HttpStatusCode.Forbidden)]
    [InlineData("Reader", HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    public async Task Only_the_hold_permission_may_release_a_hold(string? role, HttpStatusCode expected)
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        await using var factory = DocumentHoldTestHost.Create(documentId, new InMemoryDispositionHoldStore());
        using var client = factory.CreateClient();
        if (role is not null)
        {
            AdminApiTestHost.ConfigureAuthenticatedClient(client, role);
        }

        using var content = new StringContent("""{"reasonCode":"reason.case-closed"}""", System.Text.Encoding.UTF8, "application/json");

        // Act
        using var response = await client.PostAsync(new Uri($"/api/v1/documents/{documentId}/holds/{Guid.CreateVersion7()}/release", UriKind.Relative), content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(expected);
    }

    [Fact]
    public async Task The_placing_and_releasing_actor_come_from_the_principal_not_the_body()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        var holds = new InMemoryDispositionHoldStore();
        await using var factory = DocumentHoldTestHost.Create(documentId, holds);
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        const string forged = """{"basis":"LegalClaim","purposeCode":"purpose.claim-001","authorityReference":"authority:case-0001","reviewDueAt":"2999-01-01T00:00:00Z","placedBy":"forged-actor"}""";
        using var placeContent = new StringContent(forged, System.Text.Encoding.UTF8, "application/json");
        using var placeResponse = await client.PostAsync(new Uri($"/api/v1/documents/{documentId}/holds", UriKind.Relative), placeContent, TestContext.Current.CancellationToken);
        var holdId = holds.Holds.ShouldHaveSingleItem().Id;
        using var releaseContent = new StringContent("""{"reasonCode":"reason.case-closed","releasedBy":"forged-actor"}""", System.Text.Encoding.UTF8, "application/json");

        // Act
        using var releaseResponse = await client.PostAsync(new Uri($"/api/v1/documents/{documentId}/holds/{holdId}/release", UriKind.Relative), releaseContent, TestContext.Current.CancellationToken);

        // Assert
        placeResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        releaseResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var hold = holds.Holds.ShouldHaveSingleItem();
        hold.PlacedBy.ShouldBe("test-user");
        hold.ReleasedBy.ShouldBe("test-user");
    }

    [Fact]
    public async Task A_second_release_returns_a_conflict_problem()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        var holds = new InMemoryDispositionHoldStore();
        await using var factory = DocumentHoldTestHost.Create(documentId, holds);
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        using var placeContent = new StringContent(ValidBody, System.Text.Encoding.UTF8, "application/json");
        using var placeResponse = await client.PostAsync(new Uri($"/api/v1/documents/{documentId}/holds", UriKind.Relative), placeContent, TestContext.Current.CancellationToken);
        var releaseRoute = new Uri($"/api/v1/documents/{documentId}/holds/{holds.Holds.ShouldHaveSingleItem().Id}/release", UriKind.Relative);
        using var firstContent = new StringContent("""{"reasonCode":"reason.case-closed"}""", System.Text.Encoding.UTF8, "application/json");
        using var firstRelease = await client.PostAsync(releaseRoute, firstContent, TestContext.Current.CancellationToken);
        using var secondContent = new StringContent("""{"reasonCode":"reason.case-closed"}""", System.Text.Encoding.UTF8, "application/json");

        // Act
        using var secondRelease = await client.PostAsync(releaseRoute, secondContent, TestContext.Current.CancellationToken);

        // Assert
        placeResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        firstRelease.StatusCode.ShouldBe(HttpStatusCode.OK);
        secondRelease.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task A_rejected_request_does_not_echo_the_submitted_content()
    {
        // Arrange
        const string marker = "SYNTHETIC jane.doe@example.invalid";
        var documentId = Guid.CreateVersion7();
        var holds = new InMemoryDispositionHoldStore();
        await using var factory = DocumentHoldTestHost.Create(documentId, holds);
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var body = $$"""{"basis":"LegalClaim","purposeCode":"{{marker}}","authorityReference":"authority:case-0001","reviewDueAt":"2999-01-01T00:00:00Z"}""";
        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

        // Act
        using var response = await client.PostAsync(new Uri($"/api/v1/documents/{documentId}/holds", UriKind.Relative), content, TestContext.Current.CancellationToken);
        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        text.ShouldNotContain("jane.doe", StringComparison.Ordinal);
        text.ShouldNotContain("SYNTHETIC", StringComparison.Ordinal);
        holds.Holds.ShouldBeEmpty();
    }
}
