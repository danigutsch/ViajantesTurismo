using Microsoft.AspNetCore.Components.Web;
using SharedKernel.Testing;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using ViajantesTurismo.Management.Web.Components.Pages.Payments;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Payments;

[Trait(TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[TestScope(TestScope.Component)]
public sealed class PaymentPagesTests : BunitContext
{
    private PaymentPageHttpScenario Handler => Services.GetRequiredService<PaymentPageHttpScenario>();
    private PaymentReviewTransportGate ReviewGate => Services.GetRequiredService<PaymentReviewTransportGate>();
    private PaymentProjectionResponseGate ProjectionGate => Services.GetRequiredService<PaymentProjectionResponseGate>();
    private readonly FakeBookingsApiClient _bookings = new();
    private readonly BunitAuthorizationContext _authorization;
    public PaymentPagesTests()
    {
        Services.AddSingleton<PaymentPageHttpScenario>();
        Services.AddSingleton(provider => new PaymentReviewTransportGate(provider.GetRequiredService<PaymentPageHttpScenario>().Transport));
        Services.AddSingleton(provider => new PaymentProjectionResponseGate(provider.GetRequiredService<PaymentReviewTransportGate>()));
        Services.AddSingleton(provider => new HttpClient(provider.GetRequiredService<PaymentProjectionResponseGate>(), disposeHandler: false) { BaseAddress = new Uri("https://payments.example.test") });
        Services.AddSingleton<IBookingsApiClient>(_bookings);
        Services.AddSingleton<IBookingPaymentsApiClient>(provider => new BookingPaymentsApiClient(provider.GetRequiredService<HttpClient>()));
        Services.AddSingleton<IPaymentAccountsApiClient>(provider => new PaymentAccountsApiClient(provider.GetRequiredService<HttpClient>()));
        Services.AddSingleton<IPaymentInstructionsApiClient>(provider => PaymentInstructionsApiClient.WithLatestReviewOnly(provider.GetRequiredService<HttpClient>()));
        Services.AddSingleton(TimeProvider.System);
        _authorization = AddAuthorization();
        _authorization.SetAuthorized("owner");
        _authorization.SetRoles("Admin");
    }
    [Fact]
    public void Financial_forms_have_labels_and_saving_a_draft_does_not_imply_approval()
    {
        // Arrange
        var booking = BuildBookingDto() with
        {
            Currency = "EUR"
        };
        _bookings.AddBooking(booking);

        // Act
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.Find("#schedule-obligation"));

        // Assert
        foreach (var input in page.FindAll("input[id],select[id]"))
        {
            page.FindAll($"label[for='{input.Id}']").Count.ShouldBe(1);
        }
        page.Markup.ShouldContain("Saving does not approve terms", StringComparison.Ordinal);
        Handler.Keys.Count.ShouldBe(0);
    }

    [Fact]
    public void Booking_finances_limits_receipt_cards_and_action_choices_for_retained_history()
    {
        // Arrange
        const int retainedReceipts = 803;
        const int firstPageSize = 25;
        var booking = BuildBookingDto() with
        {
            Currency = "EUR"
        };
        _bookings.AddBooking(booking);
        Handler.FinancialHistory = PaymentPageData.HistoryWithReceipts(booking.Id, retainedReceipts);

        // Act
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.Markup.ShouldContain("Receipts and customer credit", StringComparison.Ordinal));
        var receiptCards = page.FindAll("h3").Count(heading => heading.TextContent.StartsWith("Receipt ", StringComparison.Ordinal));
        var reconciliationChoices = page.FindAll("#reconcile-receipt option").Count - 1;
        var allocationChoices = page.FindAll("#allocation-receipt option").Count - 1;
        var refundChoices = page.FindAll("#refund-receipt option").Count - 1;
        var requests = Handler.Transport.Requests.Select(request => request.Message.RequestUri?.PathAndQuery).ToArray();
        Xunit.TestContext.Current.TestOutputHelper?.WriteLine(
            $"Financial reads {string.Join(", ", requests)}. Receipt cards {receiptCards}. Reconciliation choices {reconciliationChoices}. Allocation choices {allocationChoices}. Refund choices {refundChoices}.");

        // Assert
        receiptCards.ShouldBe(firstPageSize);
        reconciliationChoices.ShouldBeLessThanOrEqualTo(firstPageSize);
        allocationChoices.ShouldBeLessThanOrEqualTo(firstPageSize);
        refundChoices.ShouldBeLessThanOrEqualTo(firstPageSize);
        requests.ShouldBe([$"/api/v1/bookings/{booking.Id}/finances/projection"]);
    }

    [Fact]
    public void Receipt_cards_and_receipt_choices_page_independently_and_keep_a_selection()
    {
        // Arrange
        var booking = BuildBookingDto() with
        {
            Currency = "EUR"
        };
        _bookings.AddBooking(booking);
        Handler.FinancialHistory = PaymentPageData.HistoryWithReceipts(booking.Id, 53);
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.Find("nav[aria-label='Receipt history pages']"));
        var firstReceipt = page.Find("#allocation-receipt option:nth-child(2)").GetAttribute("value").ShouldNotBeNull();
        page.Find("#allocation-receipt").Change(firstReceipt);

        // Act
        page.Find("nav[aria-label='Receipt history pages'] button:last-child").Click();
        page.Find("nav[aria-label='Allocation receipt choice pages'] button:last-child").Click();

        // Assert
        page.Find("nav[aria-label='Receipt history pages']").TextContent.ShouldContain("Page 2 of 3", StringComparison.Ordinal);
        page.Find("nav[aria-label='Allocation receipt choice pages']").TextContent.ShouldContain("Page 2 of 3", StringComparison.Ordinal);
        page.FindAll("h3").ShouldNotContain(heading => heading.TextContent.Contains(firstReceipt, StringComparison.Ordinal));
        page.Find($"#allocation-receipt option[value='{firstReceipt}']").TextContent.ShouldContain("Selected", StringComparison.Ordinal);
        var requests = Handler.Transport.Requests.Select(request => request.Message.RequestUri?.PathAndQuery).ToArray();
        requests.ShouldBe([
            $"/api/v1/bookings/{booking.Id}/finances/projection",
            $"/api/v1/bookings/{booking.Id}/finances/projection?section=Receipts&page=2&pageSize=25",
            $"/api/v1/bookings/{booking.Id}/finances/projection?section=ReceiptChoices&page=2&pageSize=25"
        ]);
    }

    [Fact]
    public void Receipt_evidence_is_requested_for_the_displayed_page_and_cleared_when_browsing()
    {
        // Arrange
        const string privateReference = "private-test-bank-reference";
        var booking = BuildBookingDto() with
        {
            Currency = "EUR"
        };
        _bookings.AddBooking(booking);
        Handler.FinancialHistory = PaymentPageData.HistoryWithReceipts(booking.Id, 53);
        Handler.ReceiptEvidence = privateReference;
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.Find("nav[aria-label='Receipt history pages']"));
        page.Find("nav[aria-label='Receipt history pages'] button:last-child").Click();

        // Act
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "View sensitive evidence references (audited)").Click();
        var revealed = page.Markup;
        page.Find("nav[aria-label='Receipt history pages'] button:last-child").Click();

        // Assert
        revealed.ShouldContain(privateReference, StringComparison.Ordinal);
        page.Markup.ShouldNotContain(privateReference, StringComparison.Ordinal);
        var auditRead = Handler.Transport.Requests.ShouldHaveSingleItem(request =>
            request.Message.RequestUri?.AbsolutePath.EndsWith("/projection/details", StringComparison.Ordinal) == true);
        auditRead.Message.RequestUri.ShouldNotBeNull().Query.ShouldBe("?section=Receipts&page=2&pageSize=25");
        Handler.Keys.ShouldBeEmpty();
    }

    [Fact]
    public void A_new_projection_revision_discards_pending_confirmation_and_reloads_balances()
    {
        // Arrange
        var booking = BuildBookingDto() with
        {
            Currency = "EUR"
        };
        _bookings.AddBooking(booking);
        Handler.FinancialHistory = PaymentPageData.HistoryWithReceipts(booking.Id, 53);
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.Find("nav[aria-label='Receipt history pages']"));
        page.FindAll("button").First(button => button.TextContent == "Reverse erroneous receipt").Click();
        Handler.FinancialHistory = PaymentPageData.HistoryWithReceipts(booking.Id, 54);

        // Act
        page.Find("nav[aria-label='Receipt history pages'] button:last-child").Click();

        // Assert
        page.FindAll("#financial-confirm").ShouldBeEmpty();
        page.Markup.ShouldContain("Financial revision: 66", StringComparison.Ordinal);
        page.Find("[role=alert]").TextContent.ShouldContain("Review the current revision", StringComparison.Ordinal);
        Handler.Keys.ShouldBeEmpty();
    }

    [Fact]
    public void Operators_can_record_pending_receipts_but_do_not_see_admin_approval_or_sensitive_forms()
    {
        // Arrange
        var authorization = AddAuthorization();
        authorization.SetAuthorized("operator");
        authorization.SetRoles("Operator");
        var booking = BuildBookingDto() with
        {
            Currency = "EUR"
        };
        _bookings.AddBooking(booking);

        // Act
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.Find("#receipt-gross"));

        // Assert
        page.FindAll("#schedule-obligation").Count.ShouldBe(0);
        page.FindAll("#quote-rate").Count.ShouldBe(0);
        page.FindAll("#refund-actual").Count.ShouldBe(0);
        page.Markup.ShouldContain("Record pending receipt", StringComparison.Ordinal);
        page.Markup.ShouldNotContain("View sensitive evidence", StringComparison.Ordinal);
    }
    [Fact]
    public void Unknown_receipt_outcome_keeps_the_same_key_and_payload_for_retry()
    {
        // Arrange
        var booking = BuildBookingDto() with
        {
            Currency = "EUR"
        };
        _bookings.AddBooking(booking);
        Handler.FailNextSubmission = true;
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.Find("#receipt-gross"));
        page.Find("#receipt-gross").Change("100");
        page.Find("#receipt-date").Change("2030-01-05");
        page.Find("#receipt-reference").Change("bank-evidence");

        // Act
        page.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Record pending receipt").Click();
        page.WaitForAssertion(() => page.FindAll("button").Count(button => button.TextContent == "Retry same submission").ShouldBe(1));
        page.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Retry same submission").Click();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(2));
        Handler.Keys[0].ShouldBe(Handler.Keys[1]);
        Handler.Payloads[0].ShouldBe(Handler.Payloads[1]);
        page.Markup.ShouldNotContain("Private bank data", StringComparison.Ordinal);
    }
    [Fact]
    public void Recipient_entry_requires_labeled_details_and_explicit_independent_verification()
    {
        // Act
        var page = Render<Accounts>();
        page.WaitForAssertion(() => page.Find("#account-iban"));

        // Assert
        foreach (var input in page.FindAll("input[id],select[id]"))
        {
            page.FindAll($"label[for='{input.Id}']").Count.ShouldBe(1);
        }
        page.Markup.ShouldContain("Format validation does not prove ownership", StringComparison.Ordinal);
        page.Markup.ShouldContain("Save unverified draft", StringComparison.Ordinal);
        Handler.Keys.Count.ShouldBe(0);
    }

    [Fact]
    public void Booking_instructions_display_one_page_of_protected_revisions_at_a_time()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        Handler.Instructions = PaymentPageData.InstructionRevisions(bookingId, 26);

        // Act
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, bookingId));

        // Assert
        page.WaitForAssertion(() => page.FindAll("section h2").Count.ShouldBe(25));
        page.FindAll("section h2")[0].TextContent.ShouldContain("Revision 26", StringComparison.Ordinal);
        page.Find("button[aria-label='Next instruction revisions page']").Click();
        page.WaitForAssertion(() => page.FindAll("section h2").ShouldHaveSingleItem().TextContent.ShouldContain("Revision 1", StringComparison.Ordinal));
        page.Find("button[aria-label='Previous instruction revisions page']").Click();
        page.WaitForAssertion(() => page.FindAll("section h2").Count.ShouldBe(25));
        var historyReads = Handler.Transport.Requests.Count(request => request.Message.RequestUri?.AbsolutePath.EndsWith("/finances", StringComparison.Ordinal) == true);
        historyReads.ShouldBe(0);
        var choiceReads = Handler.Transport.Requests.Count(request => request.Message.RequestUri?.AbsolutePath.EndsWith("/finances/projection", StringComparison.Ordinal) == true);
        choiceReads.ShouldBe(2);
    }

    [Fact]
    public void Instruction_generation_renders_only_the_first_bounded_eligible_choice_pages()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        Handler.FinancialHistory = PaymentPageData.InstructionChoiceHistory(bookingId, eligibleCount: 803);

        // Act
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, bookingId));
        page.WaitForAssertion(() => page.Find("#instruction-quote"));

        // Assert
        var terms = page.FindAll("#instruction-quote option");
        terms.Count.ShouldBe(26);
        var installments = page.FindAll("fieldset").ShouldHaveSingleItem(fieldset =>
            fieldset.QuerySelector("legend")?.TextContent == "Outstanding installments");
        installments.QuerySelectorAll("input[type=checkbox]").Length.ShouldBe(25);
        page.Find("nav[aria-label='Approved quote pages']").TextContent.ShouldContain("803 records", StringComparison.Ordinal);
        page.Find("nav[aria-label='Outstanding installment pages']").TextContent.ShouldContain("803 records", StringComparison.Ordinal);
    }

    [Fact]
    public void Instruction_generation_keeps_selected_quote_and_installments_across_pages_of_one_revision()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        var quoteId = Guid.Parse("00000029-4444-4444-4444-444444444444");
        var firstInstallment = Guid.Parse("00000029-5555-5555-5555-555555555555");
        var secondInstallment = Guid.Parse("00000042-5555-5555-5555-555555555555");
        Handler.Accounts = PaymentPageData.VerifiedAccounts(1);
        Handler.FinancialHistory = PaymentPageData.InstructionChoiceHistory(bookingId, eligibleCount: 51);
        Handler.SubmissionStatus = HttpStatusCode.Created;
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, bookingId));
        page.WaitForAssertion(() => page.Find("#instruction-quote"));
        page.Find("#instruction-account").Change("00000001-1111-1111-1111-111111111111");
        page.Find("#instruction-quote").Change(quoteId.ToString("D"));
        page.Find("fieldset fieldset input[type=checkbox]").Change(true);

        // Act
        page.Find("nav[aria-label='Approved quote pages'] button:last-child").Click();
        page.WaitForAssertion(() => page.Find("#instruction-quote option[value='00000042-4444-4444-4444-444444444444']"));
        var retainedQuote = page.Find("#instruction-quote option[value='00000029-4444-4444-4444-444444444444']").TextContent;
        page.Find("nav[aria-label='Outstanding installment pages'] button:last-child").Click();
        page.WaitForAssertion(() => page.Find("nav[aria-label='Outstanding installment pages']").TextContent.ShouldContain("Page 2", StringComparison.Ordinal));
        page.Find("fieldset fieldset input[type=checkbox]").Change(true);
        page.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Save generated unapproved draft").Click();

        // Assert
        var payload = Handler.Payloads.ShouldHaveSingleItem();
        var input = JsonSerializer.Deserialize(payload, PaymentFinancialContractJsonContext.Default.GeneratePaymentInstructionsDto).ShouldNotBeNull();
        input.ExpectedVersion.ShouldBe(12);
        input.QuoteId.ShouldNotBeNull().ShouldBe(quoteId);
        input.InstallmentIds.ShouldBe([firstInstallment, secondInstallment]);
        retainedQuote.ShouldContain("BRL/EUR", StringComparison.Ordinal);
    }

    [Fact]
    public void Choice_navigation_after_financial_changes_clears_selections_and_uses_the_new_revision()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        Handler.Accounts = PaymentPageData.VerifiedAccounts(1);
        Handler.FinancialHistory = PaymentPageData.InstructionChoiceHistory(bookingId, eligibleCount: 51);
        Handler.SubmissionStatus = HttpStatusCode.Created;
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, bookingId));
        page.WaitForAssertion(() => page.Find("#instruction-quote"));
        page.Find("#instruction-account").Change("00000001-1111-1111-1111-111111111111");
        page.Find("#instruction-quote").Change("00000029-4444-4444-4444-444444444444");
        page.Find("fieldset fieldset input[type=checkbox]").Change(true);
        Handler.FinancialHistory = PaymentPageData.InstructionChoiceHistory(bookingId, eligibleCount: 51, version: 13);

        // Act
        page.Find("nav[aria-label='Approved quote pages'] button:last-child").Click();
        page.WaitForAssertion(() => page.Markup.ShouldContain("Select the quote and installments again", StringComparison.Ordinal));
        var firstPageLabel = page.Find("nav[aria-label='Outstanding installment pages']").TextContent;
        page.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Save generated unapproved draft").Click();

        // Assert
        firstPageLabel.ShouldContain("Page 1", StringComparison.Ordinal);
        var payload = Handler.Payloads.ShouldHaveSingleItem();
        var input = JsonSerializer.Deserialize(payload, PaymentFinancialContractJsonContext.Default.GeneratePaymentInstructionsDto).ShouldNotBeNull();
        input.ExpectedVersion.ShouldBe(13);
        input.QuoteId.ShouldBeNull();
        input.InstallmentIds.ShouldBeEmpty();
    }

    [Fact]
    public void Instruction_generation_requires_choice_pages_from_the_same_financial_revision()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        Handler.FinancialHistory = PaymentPageData.InstructionChoiceHistory(bookingId, eligibleCount: 51);
        Handler.InstructionInstallmentVersion = 13;

        // Act
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, bookingId));

        // Assert
        page.WaitForAssertion(() => page.Markup.ShouldContain("Financial terms changed while choices were loading", StringComparison.Ordinal));
        page.FindAll("#instruction-quote").ShouldBeEmpty();
        Handler.Payloads.ShouldBeEmpty();
    }

    [Fact]
    public void A_failed_choice_page_keeps_the_displayed_financial_revision()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        Handler.FinancialHistory = PaymentPageData.InstructionChoiceHistory(bookingId, eligibleCount: 51);
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, bookingId));
        page.WaitForAssertion(() => page.Find("nav[aria-label='Approved quote pages']"));
        Handler.ReadStatus = HttpStatusCode.ServiceUnavailable;

        // Act
        page.Find("nav[aria-label='Approved quote pages'] button:last-child").Click();

        // Assert
        page.WaitForAssertion(() => page.Markup.ShouldContain("Financial choices could not be loaded", StringComparison.Ordinal));
        page.Find("nav[aria-label='Approved quote pages']").TextContent.ShouldContain("Page 1", StringComparison.Ordinal);
        page.FindAll("#instruction-quote option").Count.ShouldBe(26);
    }

    [Theory]
    [InlineData(BookingFinancialSection.InstructionSettlementQuoteChoices)]
    [InlineData(BookingFinancialSection.InstructionOutstandingInstallmentChoices)]
    public void Generation_waits_for_both_choice_sections_and_recovers_on_refresh(BookingFinancialSection unavailable)
    {
        // Arrange
        const string quote = "#instruction-quote";
        var bookingId = Guid.CreateVersion7();
        Handler.FinancialHistory = PaymentPageData.InstructionChoiceHistory(bookingId, eligibleCount: 3);
        Handler.UnavailableChoiceSection = unavailable;
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, bookingId));
        page.WaitForAssertion(() => page.Markup.ShouldContain("Approve a EUR schedule before generating instructions", StringComparison.Ordinal));
        var quoteControlsBeforeRefresh = page.FindAll(quote).Count;
        Handler.UnavailableChoiceSection = null;

        // Act
        page.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Refresh").Click();

        // Assert
        quoteControlsBeforeRefresh.ShouldBe(0);
        page.WaitForAssertion(() => page.FindAll($"{quote} option").Count.ShouldBe(4));
        page.FindAll("[role=alert]").ShouldBeEmpty();
        Handler.Payloads.ShouldBeEmpty();
    }

    [Fact]
    public void Reloading_choices_after_paging_clears_old_terms_if_one_section_is_unavailable()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        Handler.FinancialHistory = PaymentPageData.InstructionChoiceHistory(bookingId, eligibleCount: 51);
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, bookingId));
        page.WaitForAssertion(() => page.Find("nav[aria-label='Approved quote pages']"));
        Handler.FinancialHistory = PaymentPageData.InstructionChoiceHistory(bookingId, eligibleCount: 51, version: 13);
        Handler.UnavailableChoiceSection = BookingFinancialSection.InstructionOutstandingInstallmentChoices;

        // Act
        page.Find("nav[aria-label='Approved quote pages'] button:last-child").Click();

        // Assert
        page.WaitForAssertion(() => page.Markup.ShouldContain("Approve a EUR schedule before generating instructions", StringComparison.Ordinal));
        page.FindAll("#instruction-quote").ShouldBeEmpty();
        page.Markup.ShouldNotContain("Save generated unapproved draft", StringComparison.Ordinal);
        Handler.Payloads.ShouldBeEmpty();
    }

    [Fact]
    public void Refresh_preserves_choices_at_the_same_revision_and_clears_them_when_terms_change()
    {
        // Arrange
        const string selectedId = "00000029-4444-4444-4444-444444444444";
        const string selectedOption = "#instruction-quote option[value='" + selectedId + "']";
        var bookingId = Guid.CreateVersion7();
        Handler.FinancialHistory = PaymentPageData.InstructionChoiceHistory(bookingId, eligibleCount: 51);
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, bookingId));
        page.WaitForAssertion(() => page.Find(selectedOption));
        page.Find("#instruction-quote").Change(selectedId);
        page.Find("fieldset fieldset input[type=checkbox]").Change(true);
        page.Find("nav[aria-label='Approved quote pages'] button:last-child").Click();
        page.WaitForAssertion(() => page.Find("nav[aria-label='Approved quote pages']").TextContent.ShouldContain("Page 2", StringComparison.Ordinal));

        // Act
        page.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Refresh").Click();
        page.WaitForAssertion(() => page.Find(selectedOption));
        var keptInstallments = page.Find("[role=status]").TextContent;
        Handler.FinancialHistory = PaymentPageData.InstructionChoiceHistory(bookingId, eligibleCount: 51, version: 13);
        page.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Refresh").Click();

        // Assert
        keptInstallments.ShouldContain("Installments selected: 1", StringComparison.Ordinal);
        page.WaitForAssertion(() => page.FindAll(selectedOption).ShouldBeEmpty());
        page.FindAll("[role=status]").Any(status => status.TextContent.Contains("Installments selected", StringComparison.Ordinal)).ShouldBeFalse();
        page.Find("nav[aria-label='Approved quote pages']").TextContent.ShouldContain("Page 2", StringComparison.Ordinal);
        Handler.Payloads.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    public async Task A_late_quote_response_cannot_replace_the_new_bookings_choices(bool browse, int requestedPage)
    {
        // Arrange
        var ct = Xunit.TestContext.Current.CancellationToken;
        var bookingId = Guid.CreateVersion7();
        var otherBookingId = Guid.CreateVersion7();
        Handler.FinancialHistory = PaymentPageData.InstructionChoiceHistory(bookingId, eligibleCount: 51);
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, bookingId));
        page.WaitForAssertion(() => page.Find("nav[aria-label='Approved quote pages']"));
        var action = browse
            ? page.Find("nav[aria-label='Approved quote pages'] button:last-child")
            : page.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Refresh");
        ProjectionGate.HoldNext("/finances/projection", $"section={BookingFinancialSection.InstructionSettlementQuoteChoices}&page={requestedPage}");

        // Act
        await ProjectionGate.CompleteAfter(() => action.ClickAsync(new MouseEventArgs()), () =>
        {
            Handler.FinancialHistory = PaymentPageData.InstructionChoiceHistory(otherBookingId, eligibleCount: 3);
            page.Render(parameters => parameters.Add(value => value.BookingId, otherBookingId));
            page.WaitForAssertion(() => page.FindAll("#instruction-quote option").Count.ShouldBe(4));
        }, ct);

        // Assert
        page.FindAll("#instruction-quote option").Count.ShouldBe(4);
        page.FindAll("nav[aria-label='Approved quote pages']").ShouldBeEmpty();
        page.FindAll("[role=alert]").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delayed_sensitive_evidence_cannot_return_after_switching_booking_or_losing_admin_access(bool loseAccess)
    {
        // Arrange
        const string auditedRead = "View sensitive evidence references (audited)";
        const string previousReference = "previous-booking-private-receipt-evidence";
        var ct = Xunit.TestContext.Current.CancellationToken;
        var booking = BuildBookingDto() with
        {
            Currency = "EUR"
        };
        var otherBooking = BuildBookingDto() with
        {
            Currency = "EUR"
        };
        _bookings.AddBooking(booking);
        _bookings.AddBooking(otherBooking);
        Handler.FinancialHistory = PaymentPageData.HistoryWithReceipts(booking.Id, 3);
        Handler.ReceiptEvidence = previousReference;
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == auditedRead));
        var evidenceButton = page.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == auditedRead);
        ProjectionGate.HoldNext("/finances/projection/details", "section=Receipts");

        // Act
        await ProjectionGate.CompleteAfter(() => evidenceButton.ClickAsync(new MouseEventArgs()), () =>
        {
            if (loseAccess)
            {
                _authorization.SetRoles("Operator");
                page.WaitForAssertion(() => PaymentPageAuthentication.ReviewButtons(page, auditedRead).ShouldBeEmpty());
            }
            else
            {
                Handler.FinancialHistory = PaymentPageData.HistoryWithReceipts(otherBooking.Id, 2);
                page.Render(parameters => parameters.Add(value => value.BookingId, otherBooking.Id));
                page.WaitForAssertion(() => page.FindAll("h3")
                    .Count(heading => heading.TextContent.StartsWith("Receipt ", StringComparison.Ordinal)).ShouldBe(2));
            }
        }, ct);

        // Assert
        page.Markup.ShouldNotContain(previousReference, StringComparison.Ordinal);
        var evidenceButtons = PaymentPageAuthentication.ReviewButtons(page, auditedRead);
        if (loseAccess)
        {
            evidenceButtons.ShouldBeEmpty();
        }
        else
        {
            evidenceButtons.ShouldHaveSingleItem().HasAttribute("disabled").ShouldBeFalse();
        }
        Handler.Payloads.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(26)]
    [InlineData(803)]
    public void Instruction_review_history_loads_on_demand_and_keeps_the_latest_decision_warning(int reviewCount)
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        Handler.Instructions = PaymentPageData.InstructionWithReviews(bookingId, reviewCount);
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, bookingId));
        page.WaitForAssertion(() => page.Markup.ShouldContain("Attention requested", StringComparison.Ordinal));
        var initialReads = Handler.Transport.Requests.ToArray();

        // Act
        page.Find("button[aria-controls^='reviews-']").Click();
        page.WaitForAssertion(() => page.FindAll("ul[aria-label='Independent reviews'] li").Count.ShouldBe(25));
        var firstPage = page.FindAll("ul[aria-label='Independent reviews'] li").Select(item => item.TextContent).ToArray();
        page.Find("button[aria-label='Next reviews page']").Click();
        page.WaitForAssertion(() => page.Find("ul[aria-label='Independent reviews']").TextContent.ShouldContain("reviewer-0026", StringComparison.Ordinal));
        var secondPage = page.FindAll("ul[aria-label='Independent reviews'] li").Select(item => item.TextContent).ToArray();
        page.Find("button[aria-label='Previous reviews page']").Click();
        page.WaitForAssertion(() => page.Find("ul[aria-label='Independent reviews']").TextContent.ShouldContain("reviewer-0001", StringComparison.Ordinal));
        page.Find("button[aria-controls^='reviews-']").Click();

        // Assert
        initialReads.ShouldNotContain(request => request.Message.RequestUri?.AbsolutePath.EndsWith("/reviews", StringComparison.Ordinal) == true);
        initialReads.ShouldContain(request => request.Message.RequestUri?.Query.Contains("latestReviewOnly=true", StringComparison.Ordinal) == true);
        firstPage[0].ShouldContain("reviewer-0001", StringComparison.Ordinal);
        secondPage.Length.ShouldBe(Math.Min(25, reviewCount - 25));
        secondPage[0].ShouldContain("reviewer-0026", StringComparison.Ordinal);
        page.FindAll("ul[aria-label='Independent reviews']").ShouldBeEmpty();
        page.Markup.ShouldContain("Attention requested", StringComparison.Ordinal);
        page.Markup.ShouldNotContain("reviewer-0803", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Closing_instruction_review_history_cancels_its_pending_request()
    {
        // Arrange
        var ct = Xunit.TestContext.Current.CancellationToken;
        var bookingId = Guid.CreateVersion7();
        Handler.Instructions = PaymentPageData.InstructionWithReviews(bookingId, 2);
        ReviewGate.HoldReviewRequest = true;
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, bookingId));
        page.WaitForAssertion(() => page.Find("button[aria-controls^='reviews-']"));

        // Act
        var open = page.Find("button[aria-controls^='reviews-']").ClickAsync(new());
        await ReviewGate.Started.WaitAsync(TimeSpan.FromSeconds(5), ct);
        page.WaitForAssertion(() => page.Markup.ShouldContain("Loading independent reviews", StringComparison.Ordinal));
        var beforeClose = page.Markup;
        await page.Find("button[aria-controls^='reviews-']").ClickAsync(new());
        await ReviewGate.Canceled.WaitAsync(TimeSpan.FromSeconds(5), ct);
        await open;

        // Assert
        beforeClose.ShouldContain("Loading independent reviews", StringComparison.Ordinal);
        page.Markup.ShouldNotContain("Loading independent reviews", StringComparison.Ordinal);
        page.Markup.ShouldNotContain("Instruction reviews could not be loaded", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, "No independent reviews.")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Instruction reviews could not be loaded.")]
    public void Instruction_review_history_has_distinct_empty_and_unavailable_states(HttpStatusCode status, string message)
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        Handler.Instructions = PaymentPageData.InstructionWithReviews(bookingId, 0);
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, bookingId));
        page.WaitForAssertion(() => page.Find("button[aria-controls^='reviews-']"));
        Handler.ReadStatus = status;

        // Act
        page.Find("button[aria-controls^='reviews-']").Click();

        // Assert
        page.WaitForAssertion(() => page.Markup.ShouldContain(message, StringComparison.Ordinal));
        page.FindAll("nav[aria-label='Independent review pages']").ShouldBeEmpty();
    }

    [Fact]
    public void Regeneration_from_an_older_page_returns_to_the_newest_revision()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        Handler.Accounts = PaymentPageData.VerifiedAccounts(1);
        Handler.Instructions = PaymentPageData.InstructionRevisions(bookingId, 26);
        Handler.SubmissionStatus = HttpStatusCode.Created;
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, bookingId));
        page.WaitForAssertion(() => page.Find("button[aria-label='Next instruction revisions page']"));
        page.Find("button[aria-label='Next instruction revisions page']").Click();
        page.WaitForAssertion(() => page.FindAll("section h2").ShouldHaveSingleItem().TextContent.ShouldContain("Revision 1", StringComparison.Ordinal));
        page.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Select for regeneration").Click();
        page.Find("#instruction-account").Change("00000001-1111-1111-1111-111111111111");
        Handler.Instructions = PaymentPageData.InstructionRevisions(bookingId, 27);

        // Act
        page.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Save generated unapproved draft").Click();

        // Assert
        page.WaitForAssertion(() => page.FindAll("section h2")[0].TextContent.ShouldContain("Revision 27", StringComparison.Ordinal));
        Handler.Paths.ShouldHaveSingleItem().ShouldEndWith("/regenerate", StringComparison.Ordinal);
        var command = Handler.Transport.Requests.Where(request => request.Method == HttpMethod.Post).ShouldHaveSingleItem();
        var commandUri = command.Message.RequestUri.ShouldNotBeNull();
        commandUri.Query.ShouldBe("?latestReviewOnly=true");
    }

    [Fact]
    public void Changing_bookings_resets_the_history_page_and_old_revision_selection()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        var otherBookingId = Guid.CreateVersion7();
        Handler.Instructions = PaymentPageData.InstructionRevisions(bookingId, 26);
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, bookingId));
        page.WaitForAssertion(() => page.Find("button[aria-label='Next instruction revisions page']"));
        page.Find("button[aria-label='Next instruction revisions page']").Click();
        page.WaitForAssertion(() => page.FindAll("section h2").ShouldHaveSingleItem().TextContent.ShouldContain("Revision 1", StringComparison.Ordinal));
        page.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Select for regeneration").Click();
        Handler.Instructions = PaymentPageData.InstructionRevisions(otherBookingId, 30);

        // Act
        page.Render(parameters => parameters.Add(value => value.BookingId, otherBookingId));

        // Assert
        page.WaitForAssertion(() => page.FindAll("section h2")[0].TextContent.ShouldContain("Revision 30", StringComparison.Ordinal));
        page.Markup.ShouldNotContain("Regenerate revision 1", StringComparison.Ordinal);
    }

    [Fact]
    public void Account_register_displays_only_the_first_page_when_the_collection_grows()
    {
        // Arrange
        Handler.Accounts = PaymentPageData.VerifiedAccounts(26);

        // Act
        var page = Render<Accounts>();

        // Assert
        page.WaitForAssertion(() => page.FindAll("tbody tr").Count.ShouldBe(25));
        page.Find("button[aria-label='Next payment accounts page']").Click();
        page.WaitForAssertion(() => page.FindAll("tbody tr").ShouldHaveSingleItem().TextContent.ShouldContain("Verified recipient 26", StringComparison.Ordinal));
        page.Find("button[aria-label='Previous payment accounts page']").Click();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Count.ShouldBe(25));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Instruction_recipient_picker_keeps_the_selected_version_while_browsing_verified_pages(bool returnToFirstPage)
    {
        // Arrange
        Handler.Accounts = PaymentPageData.VerifiedAccounts(26);
        Handler.SubmissionStatus = HttpStatusCode.Created;

        // Act
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, Guid.CreateVersion7()));

        // Assert
        page.WaitForAssertion(() => page.FindAll("#instruction-account option").Count.ShouldBe(26));
        const string selectedId = "00000001-1111-1111-1111-111111111111";
        page.Find("#instruction-account").Change(selectedId);
        page.Find("button[aria-label='Next verified recipients page']").Click();
        page.WaitForAssertion(() => page.FindAll("#instruction-account option").Count.ShouldBe(3));
        page.Find($"#instruction-account option[value='{selectedId}']").TextContent.ShouldContain("Verified recipient 1", StringComparison.Ordinal);
        if (returnToFirstPage)
        {
            page.Find("button[aria-label='Previous verified recipients page']").Click();
            page.WaitForAssertion(() => page.FindAll("#instruction-account option").Count.ShouldBe(26));
        }

        page.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Save generated unapproved draft").Click();
        page.WaitForAssertion(() => Handler.Payloads.ShouldHaveSingleItem());
        using var payload = JsonDocument.Parse(Handler.Payloads.ShouldHaveSingleItem());
        payload.RootElement.GetProperty("accountId").GetString().ShouldBe(selectedId);
        payload.RootElement.GetProperty("expectedAccountVersion").GetInt64().ShouldBe(7);
        var exactAccountReads = Handler.Transport.Requests.Count(request => request.Message.RequestUri?.AbsolutePath == $"/api/v1/payment-accounts/{selectedId}");
        exactAccountReads.ShouldBe(returnToFirstPage ? 0 : 1);
    }

    [Fact]
    public void Instruction_refresh_discards_a_recipient_that_was_retired_while_the_form_was_open()
    {
        // Arrange
        const string selectedId = "00000001-1111-1111-1111-111111111111";
        Handler.Accounts = PaymentPageData.VerifiedAccounts(26);
        Handler.SubmissionStatus = HttpStatusCode.Conflict;
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, Guid.CreateVersion7()));
        page.WaitForAssertion(() => page.Find("#instruction-account"));
        page.Find("#instruction-account").Change(selectedId);
        page.Find("button[aria-label='Next verified recipients page']").Click();
        var accounts = JsonNode.Parse(Handler.Accounts).ShouldNotBeNull().AsArray();
        accounts[0].ShouldNotBeNull().AsObject()["status"] = (int)PaymentAccountStatusDto.Inactive;
        Handler.Accounts = accounts.ToJsonString();

        // Act
        page.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Save generated unapproved draft").Click();

        // Assert
        page.WaitForAssertion(() => page.Markup.ShouldContain("Instruction or source terms changed", StringComparison.Ordinal));
        page.FindAll($"#instruction-account option[value='{selectedId}']").ShouldBeEmpty();
        page.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Save generated unapproved draft").Click();
        page.WaitForAssertion(() => page.Markup.ShouldContain("Select a verified recipient", StringComparison.Ordinal));
        Handler.Payloads.ShouldHaveSingleItem();
    }

    [Fact]
    public void Instruction_generation_selects_sources_without_editable_financial_text()
    {
        // Act
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, Guid.CreateVersion7()));
        page.WaitForAssertion(() => page.Find("#instruction-account"));

        // Assert
        page.FindAll("textarea,input[type=number]").Count.ShouldBe(0);
        page.Find("label[for='instruction-account']").TextContent.ShouldContain("Verified recipient", StringComparison.Ordinal);
        page.Markup.ShouldContain("Generated financial fields cannot be edited", StringComparison.Ordinal);
        Handler.Keys.Count.ShouldBe(0);
    }

    [Theory]
    [InlineData("Verify", "verify", 200)]
    [InlineData("Withdraw verification", "withdraw-verification", 200)]
    [InlineData("Retire", "retire", 200)]
    [InlineData("Independent review (optional)", "review", 200)]
    [InlineData("Verify", "verify", 409)]
    [InlineData("Retire", "retire", 400)]
    public void Account_actions_require_confirmation_and_submit_the_displayed_revision(string button, string operation, int status)
    {
        // Arrange
        Handler.Accounts = PaymentPageData.Accounts;
        Handler.SubmissionStatus = (HttpStatusCode)status;
        var page = Render<Accounts>();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Count.ShouldBe(3));
        page.FindAll("button").First(item => item.TextContent == button).Click();

        // Act
        var section = page.Find("section[aria-labelledby='account-confirm-title']");
        for (var index = 0; index < page.FindAll("section[aria-labelledby='account-confirm-title'] input[type=checkbox]").Count; index++)
        {
            page.FindAll("section[aria-labelledby='account-confirm-title'] input[type=checkbox]")[index].Change(true);
        }
        page.FindAll("section[aria-labelledby='account-confirm-title'] button").ShouldHaveSingleItem(item => item.TextContent.StartsWith("Confirm ", StringComparison.Ordinal)).Click();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(1));
        Handler.Paths[0].ShouldEndWith("/" + operation, StringComparison.Ordinal);
        using var payload = JsonDocument.Parse(Handler.Payloads[0]);
        payload.RootElement.GetProperty("expectedVersion").GetInt64().ShouldBe(7);
        if (operation != "review")
        {
            payload.RootElement.GetProperty("confirmed").GetBoolean().ShouldBeTrue();
        }
        if (operation == "verify")
        {
            payload.RootElement.GetProperty("independentCheckAttested").GetBoolean().ShouldBeTrue();
        }
        if (status == 409)
        {
            page.WaitForAssertion(() => page.Markup.ShouldContain("The account changed", StringComparison.Ordinal));
        }
    }

    [Theory]
    [InlineData("Iban", false)]
    [InlineData("Pix", false)]
    [InlineData("BrazilianBank", false)]
    [InlineData("Iban", true)]
    [InlineData("Pix", true)]
    [InlineData("BrazilianBank", true)]
    public void Recipient_drafts_submit_only_selected_rail_fields_and_edits_use_a_revision_precondition(string kind, bool edit)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(kind);
        Handler.Accounts = PaymentPageData.Accounts;
        var page = Render<Accounts>();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Count.ShouldBe(3));
        if (edit)
        {
            page.FindAll("button").First(item => item.TextContent == "Edit recipient").Click();
            page.WaitForAssertion(() => page.Markup.ShouldContain("New unverified recipient revision", StringComparison.Ordinal));
        }
        page.Find("#account-label").Change("Checked account");
        page.Find("#recipient-name").Change("Checked recipient");
        page.Find("#account-kind").Change(kind);
        if (kind == "Iban")
        {
            page.Find("#account-iban").Change("DE89370400440532013000");
            page.Find("#account-bic").Change("COBADEFFXXX");
        }
        else if (kind == "Pix")
        {
            page.Find("#pix-key").Change("recipient@example.test");
        }
        else
        {
            page.Find("#bank-ispb").Change("00000000");
            page.Find("#bank-branch").Change("0001");
            page.Find("#bank-number").Change("123456");
            page.Find("#holder-tax").Change("52998224725");
        }

        // Act
        page.Find("form").Submit();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(1));
        using var payload = JsonDocument.Parse(Handler.Payloads[0]);
        var recipient = payload.RootElement.GetProperty("recipient");
        recipient.GetProperty("settlementCurrency").GetString().ShouldBe(kind == "Iban" ? "EUR" : "BRL");
        if (edit)
        {
            payload.RootElement.GetProperty("expectedVersion").GetInt64().ShouldBe(7);
        }
        payload.RootElement.TryGetProperty("confirmed", out _).ShouldBeFalse();
        if (kind != "Iban")
        {
            Handler.Payloads[0].ShouldNotContain("DE89370400440532013000", StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("Approve terms", "schedule")]
    [InlineData("Approve quote", "quote")]
    [InlineData("Approve reconciliation", "reconciliation")]
    [InlineData("Approve refund record", "refund")]
    [InlineData("Correct erroneous refund record", "correct")]
    [InlineData("Reverse erroneous receipt", "reverse")]
    [InlineData("Withdraw approval", "withdraw")]
    [InlineData("Optional independent review", "review")]
    [InlineData("Review allocation", "allocate")]
    [InlineData("Review release", "release")]
    public void Financial_actions_submit_only_after_confirming_the_current_summary(string button, string operation)
    {
        // Arrange
        var booking = BuildBookingDto() with
        {
            Currency = "EUR"
        };
        _bookings.AddBooking(booking);
        Handler.FinancialHistory = PaymentPageData.History;
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.FindAll("button").Any(item => item.TextContent == button).ShouldBeTrue());
        page.FindAll("button").First(item => item.TextContent == button).Click();
        var section = page.Find("section[aria-labelledby='financial-confirm']");
        Handler.Keys.Count.ShouldBe(0);
        section.QuerySelector("button").ShouldNotBeNull().HasAttribute("disabled").ShouldBeTrue();

        // Act
        for (var index = 0; index < page.FindAll("section[aria-labelledby='financial-confirm'] input[type=checkbox]").Count; index++)
        {
            page.FindAll("section[aria-labelledby='financial-confirm'] input[type=checkbox]")[index].Change(true);
        }
        page.FindAll("section[aria-labelledby='financial-confirm'] button").ShouldHaveSingleItem(item => item.TextContent.StartsWith("Confirm ", StringComparison.Ordinal)).Click();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(1));
        using var payload = JsonDocument.Parse(Handler.Payloads[0]);
        payload.RootElement.GetProperty("expectedVersion").GetInt64().ShouldBe(12);
        if (operation == "review")
        {
            payload.RootElement.GetProperty("changesRequested").GetBoolean().ShouldBeTrue();
        }
        else
        {
            payload.RootElement.GetProperty("confirmed").GetBoolean().ShouldBeTrue();
        }
        page.WaitForAssertion(() => page.FindAll("#financial-confirm").Count.ShouldBe(0));
    }

    [Theory]
    [InlineData("Save unapproved schedule", "/schedules")]
    [InlineData("Save unapproved quote", "/quotes")]
    [InlineData("Save unapproved reconciliation", "/reconciliations")]
    [InlineData("Save unapproved refund record", "/refunds")]
    public void Financial_draft_forms_preserve_calendar_dates_and_do_not_send_approval(string button, string suffix)
    {
        // Arrange
        var booking = BuildBookingDto() with
        {
            Currency = "EUR"
        };
        _bookings.AddBooking(booking);
        Handler.FinancialHistory = PaymentPageData.History;
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.Find("#schedule-obligation"));
        page.Find("#schedule-obligation").Change("100");
        page.Find("#quote-agreed").Change("2030-01-05T12:00");
        page.Find("#quote-expiry").Change("2030-01-06T12:00");
        page.Find("#refund-time").Change("2030-01-05T12:00");
        page.FindAll("label").ShouldHaveSingleItem(item => item.TextContent.Contains("I checked evidence", StringComparison.Ordinal)).QuerySelector("input").ShouldNotBeNull().Change(true);

        // Act
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == button).Click();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(1));
        Handler.Paths[0].ShouldEndWith(suffix, StringComparison.Ordinal);
        using var payload = JsonDocument.Parse(Handler.Payloads[0]);
        payload.RootElement.GetProperty("expectedVersion").GetInt64().ShouldBe(12);
        payload.RootElement.TryGetProperty("confirmed", out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Approve summary", "approve", 0)]
    [InlineData("Finalize printable HTML", "finalize", 3)]
    [InlineData("Withdraw approval", "withdraw", 3)]
    [InlineData("Void revision", "void", 4)]
    [InlineData("Independent review (optional)", "review", 4)]
    public void Instruction_actions_pin_the_displayed_document_revision_and_explicit_confirmation(string button, string operation, int documentStatus)
    {
        // Arrange
        Handler.Accounts = PaymentPageData.Accounts;
        Handler.FinancialHistory = PaymentPageData.History;
        var instruction = JsonNode.Parse(PaymentPageData.Instruction).ShouldNotBeNull();
        instruction["document"].ShouldNotBeNull()["status"] = documentStatus;
        Handler.Instructions = "[" + instruction.ToJsonString() + "]";
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, Guid.CreateVersion7()));
        page.WaitForAssertion(() => page.FindAll("button").Any(item => item.TextContent == button).ShouldBeTrue());
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == button).Click();
        var section = page.Find("section[aria-labelledby='instruction-confirm']");
        Handler.Keys.Count.ShouldBe(0);

        // Act
        for (var index = 0; index < page.FindAll("section[aria-labelledby='instruction-confirm'] input[type=checkbox]").Count; index++)
        {
            page.FindAll("section[aria-labelledby='instruction-confirm'] input[type=checkbox]")[index].Change(true);
        }
        page.FindAll("section[aria-labelledby='instruction-confirm'] button").ShouldHaveSingleItem(item => item.TextContent.StartsWith("Confirm ", StringComparison.Ordinal)).Click();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(1));
        using var payload = JsonDocument.Parse(Handler.Payloads[0]);
        payload.RootElement.GetProperty("expectedDocumentVersion").GetInt64().ShouldBe(9);
        if (operation == "review")
        {
            payload.RootElement.GetProperty("changesRequested").GetBoolean().ShouldBeTrue();
        }
        else
        {
            payload.RootElement.GetProperty("confirmed").GetBoolean().ShouldBeTrue();
        }
        page.WaitForAssertion(() => page.FindAll("#instruction-confirm").Count.ShouldBe(0));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Generating_instructions_pins_account_financial_and_replacement_revisions(bool regenerate)
    {
        // Arrange
        Handler.Accounts = PaymentPageData.Accounts;
        Handler.FinancialHistory = PaymentPageData.History;
        Handler.Instructions = "[" + PaymentPageData.Instruction + "]";
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, Guid.CreateVersion7()));
        page.WaitForAssertion(() => page.Find("#instruction-account"));
        page.Find("#instruction-account").Change("00000001-1111-1111-1111-111111111111");
        var installment = page.FindAll("fieldset").ShouldHaveSingleItem(item => item.QuerySelector("legend")?.TextContent == "Outstanding installments");
        installment.QuerySelector("input").ShouldNotBeNull().Change(true);
        if (regenerate)
        {
            page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Select for regeneration").Click();
        }

        // Act
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Save generated unapproved draft").Click();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(1));
        using var payload = JsonDocument.Parse(Handler.Payloads[0]);
        payload.RootElement.GetProperty("expectedVersion").GetInt64().ShouldBe(12);
        payload.RootElement.GetProperty("expectedAccountVersion").GetInt64().ShouldBe(7);
        payload.RootElement.GetProperty("installmentIds").GetArrayLength().ShouldBe(1);
        if (regenerate)
        {
            payload.RootElement.GetProperty("expectedDocumentVersion").GetInt64().ShouldBe(9);
        }
        payload.RootElement.TryGetProperty("confirmed", out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.BadRequest)]
    public void Financial_rejection_keeps_approval_explicit_and_refreshes_a_conflicting_revision(HttpStatusCode status)
    {
        // Arrange
        var booking = BuildBookingDto() with
        {
            Currency = "EUR"
        };
        _bookings.AddBooking(booking);
        Handler.FinancialHistory = PaymentPageData.History;
        Handler.SubmissionStatus = status;
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.Find("#schedule-obligation"));

        // Act
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Save unapproved schedule").Click();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(1));
        page.WaitForAssertion(() => page.FindAll("[role=alert]").Count.ShouldBeGreaterThan(0));
        Handler.Payloads[0].ShouldNotContain("confirmed", StringComparison.Ordinal);
        if (status == HttpStatusCode.Conflict)
        {
            page.Markup.ShouldContain("Financial history changed", StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.BadRequest)]
    public void Instruction_rejection_retains_the_explicit_confirmation_boundary(HttpStatusCode status)
    {
        // Arrange
        Handler.Accounts = PaymentPageData.Accounts;
        Handler.FinancialHistory = PaymentPageData.History;
        Handler.Instructions = "[" + PaymentPageData.Instruction + "]";
        Handler.SubmissionStatus = status;
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, Guid.CreateVersion7()));
        page.WaitForAssertion(() => page.FindAll("button").Any(item => item.TextContent == "Approve summary").ShouldBeTrue());
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Approve summary").Click();
        page.Find("section[aria-labelledby='instruction-confirm'] input[type=checkbox]").Change(true);

        // Act
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Confirm approve").Click();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(1));
        page.WaitForAssertion(() => page.FindAll("[role=alert]").Count.ShouldBeGreaterThan(0));
        if (status == HttpStatusCode.Conflict)
        {
            page.Markup.ShouldContain("Instruction or source terms changed", StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Failed_payment_reads_show_accessible_errors_and_never_expose_transport_details()
    {
        // Arrange
        var booking = BuildBookingDto() with
        {
            Currency = "EUR",
            FinancialVersion = 4
        };
        _bookings.AddBooking(booking);
        Handler.ReadStatus = HttpStatusCode.Forbidden;

        // Act
        var accounts = Render<Accounts>();
        var finances = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        var instructions = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, booking.Id));

        // Assert
        accounts.WaitForAssertion(() => accounts.FindAll("[role=alert]").Count.ShouldBeGreaterThan(0));
        finances.WaitForAssertion(() => finances.FindAll("[role=alert]").Count.ShouldBeGreaterThan(0));
        instructions.WaitForAssertion(() => instructions.FindAll("[role=alert]").Count.ShouldBeGreaterThan(0));
        accounts.Markup.ShouldNotContain("Private bank data", StringComparison.Ordinal);
        Handler.Keys.Count.ShouldBe(0);
    }

    [Fact]
    public void Invalid_frozen_instruction_reference_does_not_submit_a_reconciliation()
    {
        // Arrange
        var booking = BuildBookingDto() with
        {
            Currency = "EUR"
        };
        _bookings.AddBooking(booking);
        Handler.FinancialHistory = PaymentPageData.History;
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.Find("#reconcile-instruction"));
        page.Find("#reconcile-instruction").Change("invalid-document-id");

        // Act
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Save unapproved reconciliation").Click();

        // Assert
        page.Find("[role=alert]").TextContent.ShouldContain("valid instruction ID", StringComparison.Ordinal);
        Handler.Keys.Count.ShouldBe(0);
    }

    [Fact]
    public void Unknown_instruction_approval_retries_the_same_immutable_payload_and_key()
    {
        // Arrange
        Handler.Accounts = PaymentPageData.Accounts;
        Handler.FinancialHistory = PaymentPageData.History;
        Handler.Instructions = "[" + PaymentPageData.Instruction + "]";
        Handler.FailNextSubmission = true;
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, Guid.CreateVersion7()));
        page.WaitForAssertion(() => page.FindAll("button").Any(item => item.TextContent == "Approve summary").ShouldBeTrue());
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Approve summary").Click();
        page.Find("section[aria-labelledby='instruction-confirm'] input[type=checkbox]").Change(true);
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Confirm approve").Click();
        page.WaitForAssertion(() => page.FindAll("button").Any(item => item.TextContent == "Retry same submission").ShouldBeTrue());

        // Act
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Retry same submission").Click();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(2));
        Handler.Keys[0].ShouldBe(Handler.Keys[1]);
        Handler.Payloads[0].ShouldBe(Handler.Payloads[1]);
        page.WaitForAssertion(() => page.FindAll("#instruction-confirm").Count.ShouldBe(0));
        page.Markup.ShouldNotContain("Private bank data", StringComparison.Ordinal);
    }

    [Fact]
    public void Account_revision_conflicts_and_unknown_outcomes_do_not_invent_a_second_submission()
    {
        // Arrange
        Handler.Accounts = PaymentPageData.Accounts;
        Handler.FailNextSubmission = true;
        var page = Render<Accounts>();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Count.ShouldBe(3));
        page.FindAll("button").First(item => item.TextContent == "Retire").Click();
        page.Find("section[aria-labelledby='account-confirm-title'] input[type=checkbox]").Change(true);
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Confirm retire").Click();
        page.WaitForAssertion(() => page.FindAll("button").Any(item => item.TextContent == "Retry same submission").ShouldBeTrue());

        // Act
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Retry same submission").Click();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(2));
        Handler.Keys[0].ShouldBe(Handler.Keys[1]);
        Handler.Payloads[0].ShouldBe(Handler.Payloads[1]);
        page.WaitForAssertion(() => page.FindAll("#account-confirm-title").Count.ShouldBe(0));
    }

    [Fact]
    public void Reconciliation_can_select_frozen_instruction_evidence_without_silently_approving_credit()
    {
        // Arrange
        var booking = BuildBookingDto() with
        {
            Currency = "EUR"
        };
        _bookings.AddBooking(booking);
        Handler.FinancialHistory = PaymentPageData.History;
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.Find("#reconcile-instruction"));
        page.Find("#reconcile-instruction").Change("00000022-1111-1111-1111-111111111111");
        page.Find("#reconcile-eur").Change("50");

        // Act
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "View sensitive evidence references (audited)").Click();
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Save unapproved reconciliation").Click();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(1));
        using var payload = JsonDocument.Parse(Handler.Payloads[0]);
        payload.RootElement.GetProperty("instructionId").GetGuid().ShouldBe(Guid.Parse("00000022-1111-1111-1111-111111111111"));
        payload.RootElement.GetProperty("euroCredit").GetDecimal().ShouldBe(50m);
        payload.RootElement.TryGetProperty("confirmed", out _).ShouldBeFalse();
    }

    [Fact]
    public void Failed_recipient_detail_read_shows_fixed_copy_and_keeps_the_edit_form_closed()
    {
        // Arrange
        Handler.Accounts = PaymentPageData.Accounts;
        var page = Render<Accounts>();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Count.ShouldBe(3));
        Handler.ReadStatus = HttpStatusCode.Forbidden;

        // Act
        page.FindAll("button").First(item => item.TextContent == "Edit recipient").Click();

        // Assert
        page.WaitForAssertion(() => page.Find("[role=alert]").TextContent.ShouldContain("Recipient details could not be loaded.", StringComparison.Ordinal));
        page.Markup.ShouldNotContain("New unverified recipient revision", StringComparison.Ordinal);
        Handler.Keys.Count.ShouldBe(0);
    }

    [Fact]
    public void Failed_sensitive_evidence_read_shows_fixed_copy_and_keeps_the_loaded_history()
    {
        // Arrange
        var booking = BuildBookingDto() with
        {
            Currency = "EUR",
            FinancialVersion = 4
        };
        _bookings.AddBooking(booking);
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.Find("#schedule-obligation"));
        Handler.ReadStatus = HttpStatusCode.Forbidden;

        // Act
        page.FindAll("button").First(item => item.TextContent.Contains("View sensitive evidence references", StringComparison.Ordinal)).Click();

        // Assert
        page.WaitForAssertion(() => page.Find("[role=alert]").TextContent.ShouldContain("Sensitive evidence could not be loaded.", StringComparison.Ordinal));
        page.Find("#schedule-obligation").ShouldNotBeNull();
        Handler.Keys.Count.ShouldBe(0);
    }
}
