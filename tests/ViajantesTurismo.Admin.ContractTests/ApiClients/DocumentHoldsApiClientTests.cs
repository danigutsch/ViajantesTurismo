using System.Net;
using PandaBear.HttpClients;
using PandaBear.Testing.Contracts;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.Http;

namespace ViajantesTurismo.Admin.ContractTests.ApiClients;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, Infrastructure.TestTraits.ContractCategory)]
public sealed class DocumentHoldsApiClientTests
{
    private const string HoldJson = """
        {
          "id":"33333333-3333-3333-3333-333333333333",
          "basis":"LegalClaim",
          "purposeCode":"PURPOSE-1",
          "authorityReference":"AUTH-1",
          "placedBy":"actor-1",
          "placedAt":"2026-10-01T09:00:00Z",
          "effectiveFrom":"2026-10-01T09:00:00Z",
          "reviewDueAt":"2027-01-01T09:00:00Z",
          "isRestraining":true
        }
        """;

    [Fact]
    public async Task PlaceHold_posts_the_codes_to_the_lineage_route_and_reads_the_created_hold()
    {
        // Arrange
        HttpRequestMessage? captured = null;
        string? body = null;
        using var httpClient = ContractHttpClientTestHelper.CreateClient(request =>
        {
            captured = request;
            body = request.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken).GetAwaiter().GetResult();
            return ContractHttpClientTestHelper.JsonResponse(HoldJson, HttpStatusCode.Created);
        });
        var sut = new DocumentsApiClient(httpClient);
        var documentId = Guid.CreateVersion7();
        var dto = new CreateDocumentHoldDto
        {
            Basis = ContractConstants.DocumentHoldBasisLegalClaim,
            PurposeCode = "PURPOSE-1",
            AuthorityReference = "AUTH-1",
            ReviewDueAt = DateTimeOffset.Parse("2027-01-01T09:00:00Z", System.Globalization.CultureInfo.InvariantCulture)
        };

        // Act
        var outcome = await sut.PlaceHold(documentId, dto, TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        outcome.Value!.Basis.ShouldBe("LegalClaim");
        captured!.Method.ShouldBe(HttpMethod.Post);
        captured.RequestUri!.PathAndQuery.ShouldBe($"/api/v1/documents/{documentId}/holds");
        body!.ShouldContain("\"authorityReference\":\"AUTH-1\"", StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListHolds_reads_every_hold_of_the_lineage()
    {
        // Arrange
        using var httpClient = ContractHttpClientTestHelper.CreateClient(_ => ContractHttpClientTestHelper.JsonResponse($"[{HoldJson}]", HttpStatusCode.OK));
        var sut = new DocumentsApiClient(httpClient);

        // Act
        var outcome = await sut.ListHolds(Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        // Assert
        outcome.Value.ShouldNotBeNull().ShouldHaveSingleItem().IsRestraining.ShouldBeTrue();
    }

    [Fact]
    public async Task ReleaseHold_posts_the_reason_code_to_the_release_route()
    {
        // Arrange
        HttpRequestMessage? captured = null;
        string? body = null;
        using var httpClient = ContractHttpClientTestHelper.CreateClient(request =>
        {
            captured = request;
            body = request.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken).GetAwaiter().GetResult();
            return ContractHttpClientTestHelper.JsonResponse(HoldJson, HttpStatusCode.OK);
        });
        var sut = new DocumentsApiClient(httpClient);
        var documentId = Guid.CreateVersion7();
        var holdId = Guid.CreateVersion7();

        // Act
        var outcome = await sut.ReleaseHold(documentId, holdId, new ReleaseDocumentHoldDto { ReasonCode = "CLAIM-CLOSED" }, TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        captured!.RequestUri!.PathAndQuery.ShouldBe($"/api/v1/documents/{documentId}/holds/{holdId}/release");
        body.ShouldBe("{\"reasonCode\":\"CLAIM-CLOSED\"}");
    }

    [Fact]
    public async Task ReleaseHold_maps_a_second_release_to_a_conflict_outcome()
    {
        // Arrange
        using var httpClient = ContractHttpClientTestHelper.CreateClient(_ => ContractHttpClientTestHelper.JsonResponse("{}", HttpStatusCode.Conflict));
        var sut = new DocumentsApiClient(httpClient);

        // Act
        var outcome = await sut.ReleaseHold(Guid.CreateVersion7(), Guid.CreateVersion7(), new ReleaseDocumentHoldDto { ReasonCode = "X" }, TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.Conflict);
    }

    [Fact]
    public async Task ListHoldOverview_sends_only_the_given_filters_escaped_and_reads_the_page()
    {
        // Arrange
        HttpRequestMessage? captured = null;
        using var httpClient = ContractHttpClientTestHelper.CreateClient(request =>
        {
            captured = request;
            return ContractHttpClientTestHelper.JsonResponse(
                $$"""{"items":[{"lineageId":"44444444-4444-4444-4444-444444444444","documentId":"55555555-5555-5555-5555-555555555555","hold":{{HoldJson}}},{"lineageId":"66666666-6666-6666-6666-666666666666","documentId":null,"hold":{{HoldJson}}}],"page":2,"pageSize":10,"hasMore":true}""",
                HttpStatusCode.OK);
        });
        var sut = new DocumentsApiClient(httpClient);

        // Act
        var outcome = await sut.ListHoldOverview(new DocumentHoldOverviewQuery("OverdueReview", "LegalClaim", null, 2, 10), TestContext.Current.CancellationToken);

        // Assert
        captured!.RequestUri!.PathAndQuery.ShouldBe("/api/v1/documents/holds?state=OverdueReview&basis=LegalClaim&page=2&pageSize=10");
        outcome.Value!.HasMore.ShouldBeTrue();
        outcome.Value.Items.Count.ShouldBe(2);
        outcome.Value.Items[0].DocumentId.ShouldBe(Guid.Parse("55555555-5555-5555-5555-555555555555"));
        outcome.Value.Items[1].DocumentId.ShouldBeNull();
    }
}
