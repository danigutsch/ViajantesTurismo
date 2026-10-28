using PandaBear.Testing;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Payments;

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
            "Recipient", "DE89370400440532013000", "COBADEFFXXX").Value, "owner", scenario.Clock.GetUtcNow()).Value;
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
    public async Task Booking_price_without_approved_terms_does_not_create_debt_or_an_access_audit()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        await using var seed = scenario.Open();
        await seed.Migrate(ct);
        var tour = EntityBuilders.BuildTour(new TourOptions(Pricing: new TourPricingOptions(Currency: Currency.Euro)));
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
        history.ObligationCurrency.ShouldBe(CurrencyDto.Euro);
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
