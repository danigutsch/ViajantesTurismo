using System.Net;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.Http;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Payments;

[Trait(TestTraitNames.CategoryName, SharedKernel.IntegrationTesting.IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[Trait(TestTraitNames.CapabilityName, ViajantesTurismo.Admin.Testing.AdminTestTraitValues.PaymentsCapability)]
[TestScope(TestScope.Integration)]
public sealed class PaymentInstructionReviewReadPostgreSqlTests(PostgreSqlTestServerFixture fixture) : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task Revision_card_returns_only_the_latest_decision_from_an_accumulated_review_history()
    {
        // Arrange
        const int reviewCount = 803;
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        var (bookingId, _) = await PaymentInstructionReviewScenario.Seed(scenario, reviewCount, ct);
        var probe = new PaymentInstructionReviewQueryProbe();
        await using var api = scenario.StartAdminApi(probe);
        using var admin = api.CreateClient();
        PostgreSqlPaymentScenario.Authenticate(admin, "Admin");
        var route = $"/api/v1/documents/bookings/{bookingId}/payment-instructions/page?page=1&pageSize=1";

        // Act
        using var legacyResponse = await admin.GetAsync(new Uri(route, UriKind.Relative), ct);
        var legacyQueryCount = probe.AuditQueries.Count;
        using var summaryResponse = await admin.GetAsync(new Uri(route + "&latestReviewOnly=true", UriKind.Relative), ct);
        var legacy = await legacyResponse.Content.ReadFromJsonAsync(PaymentFinancialContractJsonContext.Default.GetPaymentInstructionPageDto, ct);
        var summary = await summaryResponse.Content.ReadFromJsonAsync(PaymentFinancialContractJsonContext.Default.GetPaymentInstructionPageDto, ct);
        var summaryQueries = probe.AuditQueries.Skip(legacyQueryCount).ToArray();
        var defaultClient = new PaymentInstructionsApiClient(admin);
        var defaultResult = await defaultClient.ListPageForBooking(bookingId, 1, 1, ct);

        // Assert
        legacyResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        summaryResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var noStore = summaryResponse.Headers.CacheControl?.NoStore;
        noStore.ShouldNotBeNull().ShouldBeTrue();
        var original = legacy.ShouldNotBeNull().Items.ShouldHaveSingleItem();
        original.Reviews.Count.ShouldBe(reviewCount);
        var item = summary.ShouldNotBeNull().Items.ShouldHaveSingleItem();
        var latest = item.Reviews.ShouldHaveSingleItem();
        latest.ReviewerId.ShouldBe(PaymentInstructionReviewScenario.LatestReviewer);
        latest.ChangesRequested.ShouldBeTrue();
        item.DocumentVersion.ShouldBe(original.DocumentVersion);
        item.Approval.ShouldBe(original.Approval);
        summaryQueries.Length.ShouldBe(2);
        summaryQueries.ShouldAllSatisfy(sql => sql.ShouldContain("LIMIT", StringComparison.Ordinal));
        defaultResult.Value.ShouldNotBeNull().Items.ShouldHaveSingleItem().Reviews.Count.ShouldBe(reviewCount);
    }

    [Fact]
    public async Task Review_command_can_return_a_bounded_summary_without_changing_the_legacy_result()
    {
        // Arrange
        const int reviewCount = 803;
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        var (_, instructionId) = await PaymentInstructionReviewScenario.Seed(scenario, reviewCount, ct);
        await using var api = scenario.StartAdminApi();
        using var admin = api.CreateClient();
        PostgreSqlPaymentScenario.Authenticate(admin, "Admin");
        var route = $"/api/v1/documents/{instructionId}/payment-instructions";
        using var detailResponse = await admin.GetAsync(new Uri(route, UriKind.Relative), ct);
        var detail = await detailResponse.Content.ReadFromJsonAsync(PaymentFinancialContractJsonContext.Default.GetPaymentInstructionDto, ct);
        var current = detail.ShouldNotBeNull();
        var input = new ReviewPaymentInstructionsDto { ExpectedDocumentVersion = current.DocumentVersion, ChangesRequested = true };
        var defaultClient = new PaymentInstructionsApiClient(admin);
        using var summary = new HttpRequestMessage(HttpMethod.Post, new Uri(route + "/review?latestReviewOnly=true", UriKind.Relative))
        {
            Content = JsonContent.Create(input with { ChangesRequested = false }, PaymentFinancialContractJsonContext.Default.ReviewPaymentInstructionsDto),
        };
        summary.Headers.Add(ContractConstants.IdempotencyKeyHeaderName, "card-review");

        // Act
        var legacy = await defaultClient.Review(instructionId, input, "legacy-review", ct);
        using var summaryResponse = await admin.SendAsync(summary, ct);
        var summaryResult = await summaryResponse.Content.ReadFromJsonAsync(PaymentFinancialContractJsonContext.Default.GetPaymentInstructionDto, ct);

        // Assert
        summaryResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        legacy.StatusCode.ShouldNotBeNull().ShouldBe(HttpStatusCode.OK);
        legacy.Value.ShouldNotBeNull().Reviews.Count.ShouldBe(reviewCount + 1);
        var latest = summaryResult.ShouldNotBeNull().Reviews.ShouldHaveSingleItem();
        latest.ChangesRequested.ShouldBeFalse();
        await using var reader = scenario.Open();
        var stored = await reader.Instructions.GetById(instructionId, ct);
        stored.ShouldNotBeNull().Reviews.Count.ShouldBe(reviewCount + 2);
    }

    [Fact]
    public async Task Review_pages_read_only_the_requested_rows_and_keep_payment_management_authorization()
    {
        // Arrange
        const int reviewCount = 803;
        const int pageSize = 25;
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        var (_, instructionId) = await PaymentInstructionReviewScenario.Seed(scenario, reviewCount, ct);
        var probe = new PaymentInstructionReviewQueryProbe();
        await using var api = scenario.StartAdminApi(probe);
        using var admin = api.CreateClient();
        using var operatorClient = api.CreateClient();
        PostgreSqlPaymentScenario.Authenticate(admin, "Admin");
        PostgreSqlPaymentScenario.Authenticate(operatorClient, "Operator");
        var route = $"/api/v1/documents/{instructionId}/payment-instructions/reviews";

        // Act
        using var firstResponse = await admin.GetAsync(new Uri(route + "?page=1&pageSize=25", UriKind.Relative), ct);
        using var lastResponse = await admin.GetAsync(new Uri(route + "?page=999&pageSize=25", UriKind.Relative), ct);
        using var forbidden = await operatorClient.GetAsync(new Uri(route, UriKind.Relative), ct);
        using var invalid = await admin.GetAsync(new Uri(route + "?pageSize=101", UriKind.Relative), ct);
        var missingRoute = new Uri($"/api/v1/documents/{Guid.CreateVersion7()}/payment-instructions/reviews", UriKind.Relative);
        using var missing = await admin.GetAsync(missingRoute, ct);
        var firstPage = await firstResponse.Content.ReadFromJsonAsync(PaymentFinancialContractJsonContext.Default.GetPaymentInstructionReviewPageDto, ct);
        var lastPage = await lastResponse.Content.ReadFromJsonAsync(PaymentFinancialContractJsonContext.Default.GetPaymentInstructionReviewPageDto, ct);

        // Assert
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        lastResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        invalid.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        missing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var noStore = firstResponse.Headers.CacheControl?.NoStore;
        noStore.ShouldNotBeNull().ShouldBeTrue();
        var first = firstPage.ShouldNotBeNull();
        first.Page.ShouldBe(1);
        first.PageSize.ShouldBe(pageSize);
        first.Total.ShouldBe(reviewCount);
        first.Items.Count.ShouldBe(pageSize);
        first.Items.ShouldAllSatisfy(review => review.ChangesRequested.ShouldBeFalse());
        var last = lastPage.ShouldNotBeNull();
        last.Page.ShouldBe(33);
        last.Items.Count.ShouldBe(3);
        last.Items[^1].ReviewerId.ShouldBe(PaymentInstructionReviewScenario.LatestReviewer);
        last.Items[^1].TargetId.ShouldBe(instructionId);
        last.Items[^1].ChangesRequested.ShouldBeTrue();
        probe.AuditQueries.Count.ShouldBe(4);
        var pageQueries = probe.AuditQueries.Where(sql => sql.Contains("LIMIT", StringComparison.Ordinal)).ToArray();
        pageQueries.Length.ShouldBe(2);
        pageQueries.ShouldAllSatisfy(sql => sql.ShouldContain("\"Sequence\"", StringComparison.Ordinal));
    }
}
