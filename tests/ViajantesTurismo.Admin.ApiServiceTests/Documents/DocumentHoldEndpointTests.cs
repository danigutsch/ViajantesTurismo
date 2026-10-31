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

    [Theory]
    [InlineData("1")]
    [InlineData("99")]
    [InlineData("legalclaim")]
    [InlineData("LegalClaim,Investigation")]
    public async Task A_basis_that_is_not_an_exact_member_name_is_rejected(string basis)
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        var holds = new InMemoryDispositionHoldStore();
        await using var factory = DocumentHoldTestHost.Create(documentId, holds);
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var body = $$"""{"basis":"{{basis}}","purposeCode":"purpose.claim-001","authorityReference":"authority:case-0001","reviewDueAt":"2999-01-01T00:00:00Z"}""";
        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

        // Act
        using var response = await client.PostAsync(new Uri($"/api/v1/documents/{documentId}/holds", UriKind.Relative), content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        holds.Holds.ShouldBeEmpty();
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

    [Fact]
    public async Task A_created_hold_is_returned_at_the_location_of_the_201_response()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        var holds = new InMemoryDispositionHoldStore();
        await using var factory = DocumentHoldTestHost.Create(documentId, holds);
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        using var content = new StringContent(ValidBody, System.Text.Encoding.UTF8, "application/json");
        using var created = await client.PostAsync(new Uri($"/api/v1/documents/{documentId}/holds", UriKind.Relative), content, TestContext.Current.CancellationToken);

        // Act
        using var response = await client.GetAsync(created.Headers.Location, TestContext.Current.CancellationToken);

        // Assert
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        created.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe($"/api/v1/documents/{documentId}/holds/{holds.Holds.ShouldHaveSingleItem().Id}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("Admin", HttpStatusCode.NotFound)]
    [InlineData("Operator", HttpStatusCode.Forbidden)]
    [InlineData("Reader", HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    public async Task Only_the_hold_permission_may_read_one_hold(string? role, HttpStatusCode expected)
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
        using var response = await client.GetAsync(new Uri($"/api/v1/documents/{documentId}/holds/{Guid.CreateVersion7()}", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(expected);
    }

    [Fact]
    public async Task A_hold_of_another_document_is_not_found_without_echoing_ids()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        var otherDocumentId = Guid.CreateVersion7();
        var holds = new InMemoryDispositionHoldStore();
        await using var factory = DocumentHoldTestHost.Create(documentId, holds);
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var holdId = Guid.CreateVersion7();

        // Act
        using var unknown = await client.GetAsync(new Uri($"/api/v1/documents/{documentId}/holds/{holdId}", UriKind.Relative), TestContext.Current.CancellationToken);
        using var wrongDocument = await client.GetAsync(new Uri($"/api/v1/documents/{otherDocumentId}/holds/{holdId}", UriKind.Relative), TestContext.Current.CancellationToken);
        var text = await unknown.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        unknown.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        wrongDocument.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        text.ShouldNotContain(holdId.ToString(), StringComparison.OrdinalIgnoreCase);
        text.ShouldNotContain(documentId.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Admin", HttpStatusCode.OK)]
    [InlineData("Operator", HttpStatusCode.Forbidden)]
    [InlineData("Reader", HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    public async Task Only_the_hold_permission_may_read_the_hold_overview(string? role, HttpStatusCode expected)
    {
        // Arrange
        await using var factory = DocumentHoldTestHost.Create(Guid.CreateVersion7(), new InMemoryDispositionHoldStore());
        using var client = factory.CreateClient();
        if (role is not null)
        {
            AdminApiTestHost.ConfigureAuthenticatedClient(client, role);
        }

        // Act
        using var response = await client.GetAsync(new Uri("/api/v1/documents/holds", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(expected);
    }

    [Fact]
    public async Task The_overview_filters_by_state_and_basis_and_returns_codes_and_ids_only()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        var holds = new InMemoryDispositionHoldStore();
        await using var factory = DocumentHoldTestHost.Create(documentId, holds);
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        using var legalBody = new StringContent(ValidBody, System.Text.Encoding.UTF8, "application/json");
        using var legal = await client.PostAsync(new Uri($"/api/v1/documents/{documentId}/holds", UriKind.Relative), legalBody, TestContext.Current.CancellationToken);
        using var investigationBody = new StringContent(ValidBody.Replace("LegalClaim", "Investigation", StringComparison.Ordinal), System.Text.Encoding.UTF8, "application/json");
        using var investigation = await client.PostAsync(new Uri($"/api/v1/documents/{documentId}/holds", UriKind.Relative), investigationBody, TestContext.Current.CancellationToken);

        // Act
        using var all = await client.GetAsync(new Uri("/api/v1/documents/holds?state=Active&basis=Investigation&page=1&pageSize=10", UriKind.Relative), TestContext.Current.CancellationToken);
        using var overdue = await client.GetAsync(new Uri("/api/v1/documents/holds?state=OverdueReview", UriKind.Relative), TestContext.Current.CancellationToken);
        var allJson = await all.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var overdueJson = await overdue.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        legal.StatusCode.ShouldBe(HttpStatusCode.Created);
        investigation.StatusCode.ShouldBe(HttpStatusCode.Created);
        all.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = System.Text.Json.JsonDocument.Parse(allJson);
        var item = document.RootElement.GetProperty("items").EnumerateArray().ShouldHaveSingleItem();
        item.GetProperty("hold").GetProperty("basis").GetString().ShouldBe("Investigation");
        document.RootElement.GetProperty("hasMore").GetBoolean().ShouldBeFalse();
        using var overdueDocument = System.Text.Json.JsonDocument.Parse(overdueJson);
        overdueDocument.RootElement.GetProperty("items").GetArrayLength().ShouldBe(0);
    }

    [Theory]
    [InlineData("state=SYNTHETIC_jane.doe")]
    [InlineData("state=active")]
    [InlineData("basis=SYNTHETIC_jane.doe")]
    [InlineData("basis=1")]
    [InlineData("releasedSince=SYNTHETIC_jane.doe")]
    [InlineData("releasedSince=2026-10-31")]
    [InlineData("page=SYNTHETIC_jane.doe")]
    [InlineData("page=0")]
    [InlineData("page=-1")]
    [InlineData("pageSize=SYNTHETIC_jane.doe")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("pageSize=99999999999")]
    public async Task Invalid_overview_filters_are_rejected_as_problems_without_echoing_input(string query)
    {
        // Arrange
        await using var factory = DocumentHoldTestHost.Create(Guid.CreateVersion7(), new InMemoryDispositionHoldStore());
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");

        // Act
        using var response = await client.GetAsync(new Uri($"/api/v1/documents/holds?{query}", UriKind.Relative), TestContext.Current.CancellationToken);
        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/problem+json");
        text.ShouldNotContain("SYNTHETIC", StringComparison.Ordinal);
        text.ShouldNotContain("99999999999", StringComparison.Ordinal);
    }
}
