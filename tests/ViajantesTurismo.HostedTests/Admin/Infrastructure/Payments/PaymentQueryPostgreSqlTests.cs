using PandaBear.BuildingBlocks;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Payments;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
[TestScope(TestScope.Integration)]
public sealed class PaymentQueryPostgreSqlTests(PostgreSqlTestServerFixture fixture) : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task Account_summaries_mask_identifiers_and_sensitive_reads_persist_an_audit()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        await using var seed = scenario.Open();
        await seed.Migrate(ct);
        var account = PaymentAccount.Create("EUR", PaymentRecipientDetails.CreateIban(
            "Recipient", "DE89370400440532013000", "COBADEFFXXX", Currency.Eur).Value, "owner", scenario.Clock.GetUtcNow()).Value;
        await seed.SeedAccount(account, ct);
        var originalAudits = await seed.AuditCount(ct);

        // Act
        await using var reader = scenario.Open();
        var accounts = await reader.Accounts.List(ct);
        var summary = accounts.ShouldHaveSingleItem();
        var sensitiveAccount = await reader.Accounts.GetSensitiveDetails(account.Id, "admin", ct);
        var details = sensitiveAccount.ShouldNotBeNull();

        // Assert
        summary.Id.ShouldBe(account.Id);
        summary.MaskedIdentifier.ShouldNotContain("DE89370400440532013000", StringComparison.Ordinal);
        details.Recipient.Iban.ShouldBe("DE89370400440532013000");
        details.Recipient.Bic.ShouldBe("COBADEFFXXX");
        details.Account.ShouldBe(summary);
        await using var restarted = scenario.Open();
        var persistedAudits = await restarted.AuditCount(ct);
        persistedAudits.ShouldBe(originalAudits + 1);
    }

    [Fact]
    public async Task Account_pages_filter_verified_rows_in_stable_order_and_preserve_masking_without_audits()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        await using var seed = scenario.Open();
        await seed.Migrate(ct);
        const string identifier = "DE89370400440532013000";
        var recipient = PaymentRecipientDetails.CreateIban("Recipient", identifier, "COBADEFFXXX", Currency.Eur).Value;
        var now = scenario.Clock.GetUtcNow();
        var draft = PaymentAccount.Create("Draft recipient", recipient, "owner", now).Value;
        await seed.SeedAccount(draft, ct);
        var ids = new List<Guid>();
        for (var index = 0; index <= ContractConstants.DefaultPaymentAccountPageSize; index++)
        {
            var label = FormattableString.Invariant($"Verified recipient {index}");
            var account = PaymentAccount.Create(label, recipient, "owner", now).Value;
            account.Verify(account.Version, "owner", now, PaymentAccountVerificationMethod.BankStatement, now, true, true).IsSuccess.ShouldBeTrue();
            await seed.SeedAccount(account, ct);
            ids.Add(account.Id);
        }

        var initialAudits = await seed.AuditCount(ct);
        await using var reader = scenario.Open();

        // Act
        var first = await reader.Accounts.ListPage(1, ContractConstants.DefaultPaymentAccountPageSize, verifiedOnly: true, ct);
        var last = await reader.Accounts.ListPage(999, ContractConstants.DefaultPaymentAccountPageSize, verifiedOnly: true, ct);
        var all = await reader.Accounts.ListPage(1, ContractConstants.MaxPaymentAccountPageSize, verifiedOnly: false, ct);
        var finalAudits = await reader.AuditCount(ct);

        // Assert
        first.Items.Count.ShouldBe(ContractConstants.DefaultPaymentAccountPageSize);
        first.Total.ShouldBe(ids.Count);
        last.Page.ShouldBe(2);
        last.Items.ShouldHaveSingleItem();
        first.Items.Concat(last.Items).Select(account => account.Id).ShouldBe(ids.Order());
        first.Items.Concat(last.Items).All(account =>
            account.Status == PaymentAccountStatusDto.Verified && !account.MaskedIdentifier.Contains(identifier, StringComparison.Ordinal)).ShouldBeTrue();
        all.Total.ShouldBe(ids.Count + 1);
        all.Items.ShouldContain(account => account.Id == draft.Id);
        finalAudits.ShouldBe(initialAudits);
    }

    [Fact]
    public async Task Booking_price_without_approved_terms_does_not_create_debt_or_an_access_audit()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        await using var seed = scenario.Open();
        await seed.Migrate(ct);
        var tour = EntityBuilders.BuildTour(new TourOptions(Pricing: new TourPricingOptions(Currency: Currency.Eur)));
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        await seed.Seed(tour, ct);
        var originalAudits = await seed.AuditCount(ct);

        // Act
        await using var reader = scenario.Open();
        var queriedHistory = await reader.Financials.GetByBookingId(booking.Id, ct);
        var history = queriedHistory.ShouldNotBeNull();

        // Assert
        history.BookingId.ShouldBe(booking.Id);
        history.Version.ShouldBe(0);
        history.ObligationCurrency.ShouldBe("EUR");
        history.TermsStale.ShouldBeTrue();
        history.ApprovedObligationEuro.ShouldBe(0m);
        history.OutstandingEuro.ShouldBe(0m);
        history.UnallocatedEuroCredit.ShouldBe(0m);
        history.Overdue.ShouldBeFalse();
        history.Schedules.ShouldBeEmpty();
        history.Receipts.ShouldBeEmpty();
        var audits = await reader.AuditCount(ct);
        audits.ShouldBe(originalAudits);
    }

    [Fact]
    public async Task Sensitive_financial_reads_reveal_receipt_reference_and_persist_an_access_audit()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        var (bookingId, _, _) = await scenario.SeedCustomerCredit(ct);
        await using var reader = scenario.Open();
        var originalAudits = await reader.AuditCount(ct);

        // Act
        var maskedHistory = await reader.Financials.GetByBookingId(bookingId, ct);
        var summary = maskedHistory.ShouldNotBeNull();
        var sensitiveHistory = await reader.Financials.GetSensitiveDetails(bookingId, "admin", ct);
        var details = sensitiveHistory.ShouldNotBeNull();

        // Assert
        summary.Receipts.ShouldHaveSingleItem().Reference.ShouldNotBe("receipt");
        details.Receipts.ShouldHaveSingleItem().Reference.ShouldBe("receipt");
        details.OutstandingEuro.ShouldBe(summary.OutstandingEuro);
        details.UnallocatedEuroCredit.ShouldBe(120m);
        await using var restarted = scenario.Open();
        var persistedAudits = await restarted.AuditCount(ct);
        persistedAudits.ShouldBe(originalAudits + 1);
    }

    [Fact]
    public async Task Financial_history_maps_every_kind_of_correction_into_reversals()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        var bookingId = await scenario.SeedFinancialCorrections(ct);
        await using var reader = scenario.Open();

        // Act
        var history = await reader.Financials.GetByBookingId(bookingId, ct);

        // Assert
        var reversals = history.ShouldNotBeNull().Reversals;
        string[] operations = [.. reversals.Select(reversal => reversal.Operation).Order(StringComparer.Ordinal)];
        operations.ShouldBe(["AllocationReleased", "ApprovalWithdrawn", "ReceiptReversed", "RefundRecordCorrected"]);
        reversals.Select(reversal => reversal.ApprovedBy).Distinct().ShouldHaveSingleItem().ShouldBe("owner");
        reversals.ShouldHaveSingleItem(reversal => reversal.Operation == "AllocationReleased").EuroAmount.ShouldBe(5m);
    }
}
