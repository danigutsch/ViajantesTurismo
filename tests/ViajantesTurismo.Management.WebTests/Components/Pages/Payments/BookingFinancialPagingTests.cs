using SharedKernel.Testing;
using System.Net;
using System.Text.Json;
using ViajantesTurismo.Management.Web.Components.Pages.Payments;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Payments;

[Trait(TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[TestScope(TestScope.Component)]
public sealed class BookingFinancialPagingTests : BunitContext
{
    private BookingFinancialPagingScenario Scenario => Services.GetRequiredService<BookingFinancialPagingScenario>();
    private readonly Guid _bookingId;
    public BookingFinancialPagingTests()
    {
        var booking = BuildBookingDto() with
        {
            Currency = "EUR"
        };
        var bookings = new FakeBookingsApiClient();
        bookings.AddBooking(booking);
        _bookingId = booking.Id;
        Services.AddSingleton<IBookingsApiClient>(bookings);
        Services.AddSingleton<BookingFinancialPagingScenario>();
        Services.AddSingleton(services => new HttpClient(services.GetRequiredService<BookingFinancialPagingScenario>().Transport, disposeHandler: false) { BaseAddress = new Uri("https://payments.example.test") });
        Services.AddSingleton<IBookingPaymentsApiClient>(services => new BookingPaymentsApiClient(services.GetRequiredService<HttpClient>()));
        Services.AddSingleton(TimeProvider.System);
        var authorization = AddAuthorization();
        authorization.SetAuthorized("owner");
        authorization.SetRoles("Admin");
    }

    [Theory]
    [InlineData("Schedule history pages", BookingFinancialSection.Schedules)]
    [InlineData("Quote history pages", BookingFinancialSection.Quotes)]
    [InlineData("Receipt history pages", BookingFinancialSection.Receipts)]
    [InlineData("Reconciliation history pages", BookingFinancialSection.Reconciliations)]
    [InlineData("Allocation history pages", BookingFinancialSection.Allocations)]
    [InlineData("Refund record pages", BookingFinancialSection.Refunds)]
    [InlineData("Financial reversal pages", BookingFinancialSection.Reversals)]
    [InlineData("Financial review history pages", BookingFinancialSection.Reviews)]
    [InlineData("Reconciliation receipt choice pages", BookingFinancialSection.ReceiptChoices)]
    [InlineData("Allocation receipt choice pages", BookingFinancialSection.ReceiptChoices)]
    [InlineData("Refund receipt choice pages", BookingFinancialSection.ReceiptChoices)]
    [InlineData("Settlement quote choice pages", BookingFinancialSection.SettlementQuoteChoices)]
    [InlineData("Refund quote choice pages", BookingFinancialSection.RefundQuoteChoices)]
    [InlineData("Reconciliation installment choice pages", BookingFinancialSection.ApprovedInstallmentChoices)]
    [InlineData("Allocation installment choice pages", BookingFinancialSection.ApprovedInstallmentChoices)]
    [InlineData("Proposed installment choice pages", BookingFinancialSection.ProposedInstallmentChoices)]
    [InlineData("Release allocation choice pages", BookingFinancialSection.ActiveAllocationChoices)]
    [InlineData("Refund allocation choice pages", BookingFinancialSection.ActiveAllocationChoices)]
    public void Each_financial_list_requests_only_its_own_window_and_can_return_to_the_first_page(string label, BookingFinancialSection section)
    {
        // Arrange
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, _bookingId));
        var pager = $"nav[aria-label='{label}']";
        page.WaitForAssertion(() => page.Find(pager));
        var originalSummary = page.FindAll("p").First(item => item.TextContent.StartsWith("Approved obligation", StringComparison.Ordinal)).TextContent;

        // Act
        page.Find($"{pager} button:last-child").Click();
        var secondPage = page.Find(pager).TextContent;
        page.Find($"{pager} button:first-child").Click();

        // Assert
        secondPage.ShouldContain("Page 2 of 2", StringComparison.Ordinal);
        page.Find(pager).TextContent.ShouldContain("Page 1 of 2", StringComparison.Ordinal);
        page.Markup.ShouldContain(originalSummary, StringComparison.Ordinal);
        var requests = Scenario.Transport.Requests.Select(request => request.Message.RequestUri?.Query).ToArray();
        requests.ShouldBe([string.Empty, $"?section={section}&page=2&pageSize=1", $"?section={section}&page=1&pageSize=1"]);
    }

    [Theory]
    [InlineData("Schedule 1 installment pages", BookingFinancialSection.ScheduleInstallments, "00000006-1111-1111-1111-111111111111", 1)]
    [InlineData("Reconciliation 00000013-1111-1111-1111-111111111111 selection pages", BookingFinancialSection.ReconciliationSelections, "00000013-1111-1111-1111-111111111111", 5)]
    [InlineData("Refund 00000014-1111-1111-1111-111111111111 reversed allocation pages", BookingFinancialSection.RefundSelections, "00000014-1111-1111-1111-111111111111", 1)]
    [InlineData("Reversal 00000017-1111-1111-1111-111111111111 linked allocation pages", BookingFinancialSection.ReversalLinks, "00000017-1111-1111-1111-111111111111", 1)]
    public void Nested_financial_paging_uses_the_visible_parent_and_replaces_its_details(string label, BookingFinancialSection section, string parentId, int pageSize)
    {
        // Arrange
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, _bookingId));
        var pager = $"nav[aria-label='{label}']";
        page.WaitForAssertion(() => page.Find(pager));

        // Act
        page.Find($"{pager} button:last-child").Click();

        // Assert
        page.Find(pager).TextContent.ShouldContain("Page 2 of 2", StringComparison.Ordinal);
        page.Markup.ShouldContain("c5082377-3be8-44d9-9ada-664b30788c02", StringComparison.Ordinal);
        var request = Scenario.Transport.Requests[^1].Message.RequestUri.ShouldNotBeNull();
        request.Query.ShouldBe($"?section={section}&page=2&pageSize={pageSize}&parentId={parentId}");
    }

    [Fact]
    public void Reconciliation_confirmation_covers_the_whole_record_when_allocations_span_pages()
    {
        // Arrange
        const string reconciliationId = "00000013-1111-1111-1111-111111111111";
        const string confirmationSelector = "section[aria-labelledby='financial-confirm']";
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, _bookingId));
        var pager = $"nav[aria-label='Reconciliation {reconciliationId} selection pages']";
        page.WaitForAssertion(() => page.Find(pager).TextContent.ShouldContain("Page 1 of 2", StringComparison.Ordinal));

        // Act
        page.Find($"{pager} button:last-child").Click();
        page.WaitForAssertion(() => page.Find(pager).TextContent.ShouldContain("Page 2 of 2", StringComparison.Ordinal));
        page.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Approve reconciliation").Click();
        var confirmation = page.Find(confirmationSelector).TextContent;
        page.Find($"{confirmationSelector} input[type=checkbox]").Change(true);
        page.FindAll($"{confirmationSelector} button").ShouldHaveSingleItem(button => button.TextContent == "Confirm reconciliation").Click();

        // Assert
        confirmation.ShouldContain("Approve the full reconciliation", StringComparison.Ordinal);
        confirmation.ShouldContain("The total number of allocations across all pages is 6", StringComparison.Ordinal);
        confirmation.ShouldContain("I confirm the full reconciliation and the displayed summary", StringComparison.Ordinal);
        var submission = Scenario.Transport.Requests.ShouldHaveSingleItem(request => request.Method == HttpMethod.Post);
        submission.Message.RequestUri.ShouldNotBeNull().AbsolutePath.ShouldBe($"/api/v1/bookings/{_bookingId}/finances/reconciliations/{reconciliationId}/approve");
        using var payload = JsonDocument.Parse(submission.Body.ShouldNotBeNull());
        payload.RootElement.GetProperty("expectedVersion").GetInt64().ShouldBe(12);
        payload.RootElement.GetProperty("confirmed").GetBoolean().ShouldBeTrue();
        payload.RootElement.TryGetProperty("allocations", out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData("View quote evidence references (audited)", BookingFinancialSection.Quotes)]
    [InlineData("View refund evidence references (audited)", BookingFinancialSection.Refunds)]
    public void Each_sensitive_evidence_button_reveals_only_its_selected_section(string label, BookingFinancialSection section)
    {
        // Arrange
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, _bookingId));
        page.WaitForAssertion(() => page.Find("nav[aria-label='Receipt history pages']"));

        // Act
        page.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == label).Click();

        // Assert
        page.Markup.ShouldContain("private-page-evidence", StringComparison.Ordinal);
        var request = Scenario.Transport.Requests[^1].Message.RequestUri.ShouldNotBeNull();
        request.AbsolutePath.ShouldEndWith("/projection/details", StringComparison.Ordinal);
        request.Query.ShouldBe($"?section={section}&page=1&pageSize=1");
    }

    [Fact]
    public void A_failed_list_page_keeps_the_visible_snapshot_and_allows_retry()
    {
        // Arrange
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, _bookingId));
        const string pager = "nav[aria-label='Receipt history pages']";
        page.WaitForAssertion(() => page.Find(pager));
        Scenario.NextReadStatus = HttpStatusCode.Forbidden;

        // Act
        page.Find($"{pager} button:last-child").Click();
        var failedPage = page.Find(pager).TextContent;
        var error = page.Find("[role=alert]").TextContent;
        page.Find($"{pager} button:last-child").Click();

        // Assert
        failedPage.ShouldContain("Page 1 of 2", StringComparison.Ordinal);
        error.ShouldContain("Financial records could not be loaded", StringComparison.Ordinal);
        page.Find(pager).TextContent.ShouldContain("Page 2 of 2", StringComparison.Ordinal);
        page.FindAll("[role=alert]").ShouldBeEmpty();
    }
}
