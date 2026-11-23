using PandaBear.Testing;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Payments;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
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
}
