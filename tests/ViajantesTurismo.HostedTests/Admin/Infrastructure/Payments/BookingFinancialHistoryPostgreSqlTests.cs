using SharedKernel.Testing;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Payments;

[Trait(TestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[Trait(TestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
[TestScope(TestScope.Integration)]
public sealed class BookingFinancialHistoryPostgreSqlTests(PostgreSqlTestServerFixture fixture) : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task Legacy_financial_reads_preserve_large_history_masks_and_sensitive_access_audits()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        var (bookingId, otherBookingId, otherReceiptId, receipts) = await BookingFinancialHistoryFixture.Seed(scenario, ct);
        await using var reader = scenario.Open();
        var initialAudits = await reader.AuditCount(ct);

        // Act
        var masked = await reader.Financials.GetByBookingId(bookingId, ct);
        var other = await reader.Financials.GetByBookingId(otherBookingId, ct);
        var afterMasked = await reader.AuditCount(ct);
        var sensitive = await reader.Financials.GetSensitiveDetails(bookingId, "finance-reader", ct);
        await using var restarted = scenario.Open();
        var afterSensitive = await restarted.AuditCount(ct);
        var sensitiveAudit = await restarted.ReadFinancialAccessAudit("finance-reader", ct);

        // Assert
        var summary = masked.ShouldNotBeNull();
        var details = sensitive.ShouldNotBeNull();
        summary.Receipts.Count.ShouldBe(53);
        summary.Receipts.Select(receipt => receipt.Id).ShouldBe(receipts.Select(receipt => receipt.Id));
        summary.Receipts.Select(receipt => receipt.Reference).ShouldBe(receipts.Select(receipt => "…" + receipt.Reference[^4..]));
        details.Receipts.Select(receipt => receipt.Reference).ShouldBe(receipts.Select(receipt => receipt.Reference));
        details.Version.ShouldBe(summary.Version);
        details.OutstandingEuro.ShouldBe(summary.OutstandingEuro);
        other.ShouldNotBeNull().Receipts.ShouldHaveSingleItem().Id.ShouldBe(otherReceiptId);
        afterMasked.ShouldBe(initialAudits);
        afterSensitive.ShouldBe(initialAudits + 1);
        sensitiveAudit.ShouldBe((bookingId, summary.Version, "finance-reader", "SensitiveAccess"));
    }

    [Fact]
    public async Task Receipt_windows_preserve_persisted_order_and_booking_totals_across_pages()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        var (bookingId, otherBookingId, otherReceiptId, receipts) = await BookingFinancialHistoryFixture.Seed(scenario, ct);
        await using var reader = scenario.Open();
        var initialAudits = await reader.AuditCount(ct);
        var legacy = await reader.Financials.GetByBookingId(bookingId, ct);
        var request = new BookingFinancialPageRequestDto
        {
            Section = BookingFinancialSection.Receipts,
            Page = 2,
            PageSize = 25
        };
        var oversizedPage = request with
        {
            Page = 999
        };
        var choicesRequest = request with
        {
            Section = BookingFinancialSection.ReceiptChoices
        };

        // Act
        var initial = await reader.Financials.GetPage(bookingId, new BookingFinancialPageRequestDto(), ct);
        var next = await reader.Financials.GetPage(bookingId, request, ct);
        var last = await reader.Financials.GetPage(bookingId, oversizedPage, ct);
        var choices = await reader.Financials.GetPage(bookingId, choicesRequest, ct);
        var other = await reader.Financials.GetPage(otherBookingId, request, ct);
        var missing = await reader.Financials.GetPage(Guid.CreateVersion7(), request, ct);
        var finalAudits = await reader.AuditCount(ct);

        // Assert
        var summary = legacy.ShouldNotBeNull();
        var pages = new[] { initial.ShouldNotBeNull(), next.ShouldNotBeNull(), last.ShouldNotBeNull() };
        pages.Select(page => page.History.Receipts.Count).ShouldBe([25, 25, 3]);
        var windows = pages.Select(page => page.Windows.ShouldHaveSingleItem(window => window.Section == BookingFinancialSection.Receipts));
        windows.Select(window => (window.Page, window.PageSize, window.TotalCount, window.PageCount))
            .ShouldBe([(1, 25, 53, 3), (2, 25, 53, 3), (3, 25, 53, 3)]);
        var visible = pages.SelectMany(page => page.History.Receipts).ToArray();
        visible.Select(receipt => receipt.Id).ShouldBe(receipts.Select(receipt => receipt.Id));
        visible.Select(receipt => receipt.Reference).ShouldBe(receipts.Select(receipt => "…" + receipt.Reference[^4..]));
        pages.All(page => page.History.Version == summary.Version
            && page.History.ApprovedObligationEuro == summary.ApprovedObligationEuro
            && page.History.OutstandingEuro == summary.OutstandingEuro
            && page.History.UnallocatedEuroCredit == summary.UnallocatedEuroCredit).ShouldBeTrue();
        var choicePage = choices.ShouldNotBeNull();
        choicePage.Windows.ShouldHaveSingleItem(window => window.Section == BookingFinancialSection.ReceiptChoices).Page.ShouldBe(2);
        var expectedChoices = receipts.Skip(25).Take(25).ToArray();
        choicePage.ReceiptChoices.Select(receipt => receipt.Id).ShouldBe(expectedChoices.Select(receipt => receipt.Id));
        choicePage.ReceiptChoices.Select(receipt => receipt.Reference).ShouldBe(expectedChoices.Select(receipt => "…" + receipt.Reference[^4..]));
        var otherPage = other.ShouldNotBeNull();
        otherPage.History.Receipts.ShouldHaveSingleItem().Id.ShouldBe(otherReceiptId);
        otherPage.Windows.ShouldHaveSingleItem(window => window.Section == BookingFinancialSection.Receipts).TotalCount.ShouldBe(1);
        missing.ShouldBeNull();
        finalAudits.ShouldBe(initialAudits);
    }

    [Fact]
    public async Task Sensitive_receipt_page_reveals_only_its_window_and_records_one_persisted_audit()
    {
        // Arrange
        const string actor = "scoped-finance-reader";
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        var (bookingId, _, _, receipts) = await BookingFinancialHistoryFixture.Seed(scenario, ct);
        await using var reader = scenario.Open();
        var initialAudits = await reader.AuditCount(ct);
        var request = new BookingFinancialPageRequestDto
        {
            Section = BookingFinancialSection.Receipts,
            Page = 2,
            PageSize = 25
        };

        // Act
        var masked = await reader.Financials.GetPage(bookingId, request, ct);
        var revealed = await reader.Financials.GetSensitivePage(bookingId, actor, request, ct);
        var missing = await reader.Financials.GetSensitivePage(Guid.CreateVersion7(), actor, request, ct);
        await using var restarted = scenario.Open();
        var finalAudits = await restarted.AuditCount(ct);
        var access = await restarted.ReadFinancialAccessAudit(actor, ct);

        // Assert
        var maskedHistory = masked.ShouldNotBeNull().History;
        var sensitiveHistory = revealed.ShouldNotBeNull().History;
        var expected = receipts.Skip(25).Take(25).ToArray();
        sensitiveHistory.Receipts.Select(receipt => receipt.Id).ShouldBe(expected.Select(receipt => receipt.Id));
        sensitiveHistory.Receipts.Select(receipt => receipt.Reference).ShouldBe(expected.Select(receipt => receipt.Reference));
        maskedHistory.Receipts.Select(receipt => receipt.Reference).ShouldBe(expected.Select(receipt => "…" + receipt.Reference[^4..]));
        sensitiveHistory.Version.ShouldBe(maskedHistory.Version);
        missing.ShouldBeNull();
        finalAudits.ShouldBe(initialAudits + 1);
        access.ShouldBe((bookingId, sensitiveHistory.Version, actor, "SensitiveAccess"));
    }

    [Fact]
    public async Task Schedule_installment_pages_stay_scoped_to_the_schedule_and_its_booking()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        var (bookingId, otherBookingId, _, _) = await BookingFinancialHistoryFixture.Seed(scenario, ct);
        await using var reader = scenario.Open();
        var legacy = await reader.Financials.GetByBookingId(bookingId, ct);
        var schedule = legacy.ShouldNotBeNull().Schedules.ShouldHaveSingleItem();
        var request = new BookingFinancialPageRequestDto
        {
            Section = BookingFinancialSection.ScheduleInstallments,
            ParentId = schedule.Id,
            Page = 2
        };

        // Act
        var initial = await reader.Financials.GetPage(bookingId, new BookingFinancialPageRequestDto(), ct);
        var next = await reader.Financials.GetPage(bookingId, request, ct);
        var crossing = await reader.Financials.GetPage(otherBookingId, request, ct);

        // Assert
        var preview = initial.ShouldNotBeNull();
        var later = next.ShouldNotBeNull();
        var window = preview.Windows.ShouldHaveSingleItem(item => item.Section == BookingFinancialSection.ScheduleInstallments);
        window.ShouldBe(new BookingFinancialPageWindowDto(BookingFinancialSection.ScheduleInstallments, schedule.Id, 1, 5, 7, 2));
        var nextWindow = later.Windows.ShouldHaveSingleItem(item => item.Section == BookingFinancialSection.ScheduleInstallments);
        nextWindow.ShouldBe(new BookingFinancialPageWindowDto(BookingFinancialSection.ScheduleInstallments, schedule.Id, 2, 5, 7, 2));
        var previewInstallments = preview.History.Schedules.ShouldHaveSingleItem().Installments;
        var lastInstallments = later.History.Schedules.ShouldHaveSingleItem().Installments;
        previewInstallments.Count.ShouldBe(5);
        lastInstallments.Count.ShouldBe(2);
        previewInstallments.Concat(lastInstallments).Select(installment => installment.Id)
            .ShouldBe(schedule.Installments.Select(installment => installment.Id));
        crossing.ShouldBeNull();
    }

    [Fact]
    public async Task Correction_pages_preserve_order_and_scope_links_to_the_reversed_receipt()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        var bookingId = await scenario.SeedFinancialCorrections(ct);
        await using var reader = scenario.Open();
        var retained = await reader.Financials.GetByBookingId(bookingId, ct);
        var history = retained.ShouldNotBeNull();
        var reversedReceipt = history.Receipts.ShouldHaveSingleItem(receipt => receipt.Reversed);
        var revokedAllocation = history.Allocations.ShouldHaveSingleItem(allocation => allocation.ReceiptId == reversedReceipt.Id);
        var request = new BookingFinancialPageRequestDto { Section = BookingFinancialSection.Reversals, PageSize = 2 };

        // Act
        var firstResponse = await reader.Financials.GetPage(bookingId, request, ct);
        var nextResponse = await reader.Financials.GetPage(bookingId, request with
        {
            Page = 2
        }, ct);
        var first = firstResponse.ShouldNotBeNull();
        var next = nextResponse.ShouldNotBeNull();
        var reversal = first.History.Reversals[0];
        var linkRequest = new BookingFinancialPageRequestDto
        {
            Section = BookingFinancialSection.ReversalLinks,
            ParentId = reversal.Id,
            PageSize = 1
        };
        var links = await reader.Financials.GetPage(bookingId, linkRequest, ct);
        var wrongParent = await reader.Financials.GetPage(bookingId, linkRequest with
        {
            ParentId = first.History.Reversals[1].Id
        }, ct);

        // Assert
        first.Windows.ShouldHaveSingleItem(window => window.Section == BookingFinancialSection.Reversals)
            .ShouldBe(new BookingFinancialPageWindowDto(BookingFinancialSection.Reversals, null, 1, 2, 4, 2));
        next.Windows.ShouldHaveSingleItem().ShouldBe(new BookingFinancialPageWindowDto(BookingFinancialSection.Reversals, null, 2, 2, 4, 2));
        first.History.Reversals.Concat(next.History.Reversals).Select(item => (item.Operation, item.Reason)).ShouldBe(
        [
            ("ReceiptReversed", FinancialReasonDto.ReceiptError),
            ("RefundRecordCorrected", FinancialReasonDto.RefundRecordError),
            ("ApprovalWithdrawn", FinancialReasonDto.ApprovalWithdrawn),
            ("AllocationReleased", FinancialReasonDto.AllocationSelected)
        ]);
        reversal.TargetId.ShouldBe(reversedReceipt.Id);
        first.History.Reversals[1].TargetId.ShouldBe(history.Refunds.ShouldHaveSingleItem().Id);
        next.History.Reversals[0].TargetId.ShouldBe(history.Quotes.ShouldHaveSingleItem().Id);
        next.History.Reversals[1].EuroAmount.ShouldBe(5m);
        var linkPage = links.ShouldNotBeNull();
        linkPage.Windows.ShouldHaveSingleItem().ShouldBe(new BookingFinancialPageWindowDto(BookingFinancialSection.ReversalLinks, reversal.Id, 1, 1, 1, 1));
        var matched = linkPage.History.Reversals.ShouldHaveSingleItem();
        matched.Id.ShouldBe(reversal.Id);
        matched.AllocationIds.ShouldHaveSingleItem().ShouldBe(revokedAllocation.Id);
        revokedAllocation.ActiveEuroCredit.ShouldBe(0m);
        wrongParent.ShouldBeNull();
    }

    [Fact]
    public async Task Selection_pages_preserve_reconciliation_refund_and_active_credit_boundaries()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        var bookingId = await scenario.SeedFinancialCorrections(ct);
        await using var reader = scenario.Open();
        var retained = await reader.Financials.GetByBookingId(bookingId, ct);
        var history = retained.ShouldNotBeNull();
        var original = history.Allocations.ShouldHaveSingleItem(allocation => allocation.OriginalEuroCredit == 60m);
        var reconciliation = history.Reconciliations.ShouldHaveSingleItem(record => record.ReceiptId == original.ReceiptId);
        var refund = history.Refunds.ShouldHaveSingleItem();
        var reconciliationRequest = new BookingFinancialPageRequestDto
        {
            Section = BookingFinancialSection.ReconciliationSelections,
            ParentId = reconciliation.Id,
            PageSize = 1
        };
        var refundRequest = new BookingFinancialPageRequestDto
        {
            Section = BookingFinancialSection.RefundSelections,
            ParentId = refund.Id,
            PageSize = 1
        };
        var activeRequest = new BookingFinancialPageRequestDto { Section = BookingFinancialSection.ActiveAllocationChoices, PageSize = 1 };

        // Act
        var reconciliationPage = await reader.Financials.GetPage(bookingId, reconciliationRequest, ct);
        var refundPage = await reader.Financials.GetPage(bookingId, refundRequest, ct);
        var activeChoices = await reader.Financials.GetPage(bookingId, activeRequest, ct);

        // Assert
        var reconciliationWindow = reconciliationPage.ShouldNotBeNull();
        reconciliationWindow.Windows.ShouldHaveSingleItem()
            .ShouldBe(new BookingFinancialPageWindowDto(BookingFinancialSection.ReconciliationSelections, reconciliation.Id, 1, 1, 1, 1));
        var selectedReconciliation = reconciliationWindow.History.Reconciliations.ShouldHaveSingleItem();
        selectedReconciliation.Id.ShouldBe(reconciliation.Id);
        selectedReconciliation.EuroCredit.ShouldBe(120m);
        var selectedInstallment = selectedReconciliation.Allocations.ShouldHaveSingleItem();
        (selectedInstallment.TargetId, selectedInstallment.EuroAmount).ShouldBe((original.InstallmentId, 60m));
        var refundWindow = refundPage.ShouldNotBeNull();
        refundWindow.Windows.ShouldHaveSingleItem()
            .ShouldBe(new BookingFinancialPageWindowDto(BookingFinancialSection.RefundSelections, refund.Id, 1, 1, 1, 1));
        var selectedRefund = refundWindow.History.Refunds.ShouldHaveSingleItem();
        selectedRefund.Id.ShouldBe(refund.Id);
        selectedRefund.Corrected.ShouldBeTrue();
        selectedRefund.Reference.ShouldBe("…fund");
        var selectedAllocation = selectedRefund.ReversedAllocations.ShouldHaveSingleItem();
        (selectedAllocation.TargetId, selectedAllocation.EuroAmount).ShouldBe((original.Id, 10m));
        var choices = activeChoices.ShouldNotBeNull();
        choices.Windows.ShouldHaveSingleItem()
            .ShouldBe(new BookingFinancialPageWindowDto(BookingFinancialSection.ActiveAllocationChoices, null, 1, 1, 1, 1));
        var active = choices.ActiveAllocationChoices.ShouldHaveSingleItem();
        (active.Id, active.OriginalEuroCredit, active.ActiveEuroCredit).ShouldBe((original.Id, 60m, 55m));
        choices.History.Version.ShouldBe(history.Version);
        choices.History.OutstandingEuro.ShouldBe(history.OutstandingEuro);
    }
}
