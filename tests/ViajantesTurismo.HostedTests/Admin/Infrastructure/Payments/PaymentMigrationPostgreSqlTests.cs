using Npgsql;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Payments;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
public sealed class PaymentMigrationPostgreSqlTests(PostgreSqlTestServerFixture fixture) : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task Unpublished_migrations_never_create_the_obsolete_payment_table()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        await using var session = scenario.Open();
        await session.Migrate(ct, "20261020200401_UseCivilDates");
        var paymentTableBeforeLifecycle = await session.HasPaymentTable(ct);
        paymentTableBeforeLifecycle.ShouldBeFalse();

        // Act
        await session.Migrate(ct);

        // Assert
        var paymentTableAfterLifecycle = await session.HasPaymentTable(ct);
        paymentTableAfterLifecycle.ShouldBeFalse();
    }

    [Fact]
    public async Task Receipt_calendar_date_and_method_survive_encrypted_persistence_and_restart()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        await using var session = scenario.Open();
        await session.Migrate(ct);
        var tour = EntityBuilders.BuildTour(new TourOptions(Pricing: new TourPricingOptions(Currency: Currency.Euro)));
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        var now = scenario.Clock.GetUtcNow();
        var date = DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1);
        var result = tour.RecordBookingReceipt(booking.Id, 0,
            Domain.Payments.ReceiptSettlement.Create(Currency.Euro, 123.45m, 1m).Value,
            date, now, PaymentMethod.Cash, "calendar-receipt", [], "operator", now, DateOnly.FromDateTime(now.UtcDateTime));
        result.IsSuccess.ShouldBeTrue();
        await session.Seed(tour, ct);

        // Act
        await using var restarted = scenario.Open();
        var persistedHistory = await restarted.Financials.GetByBookingId(booking.Id, ct);
        var history = persistedHistory.ShouldNotBeNull();

        // Assert
        var receipt = history.Receipts.ShouldHaveSingleItem();
        receipt.Id.ShouldBe(result.Value);
        receipt.PaymentDate.ShouldBe(date);
        receipt.Method.ShouldBe(Contracts.Application.PaymentMethodDto.Cash);
        receipt.CreatedBy.ShouldBe("operator");
        receipt.ApprovedEuroCredit.ShouldBe(0m);
        receipt.PendingReconciliation.ShouldBeTrue();
    }

    [Fact]
    public async Task Rollback_refuses_to_delete_protected_financial_records()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        await using var session = scenario.Open();
        await session.Migrate(ct);
        var account = PaymentAccount.Create("EUR", PaymentRecipientDetails.CreateIban("Recipient", "DE89370400440532013000", "COBADEFFXXX").Value, "owner", scenario.Clock.GetUtcNow()).Value;
        await session.SeedAccount(account, ct);

        // Act
        var rollback = () => session.Migrate(ct, "20261020200401_UseCivilDates");
        var rejected = await rollback.ShouldThrow<PostgresException>();

        // Assert
        rejected.SqlState.ShouldBe("P0001");
        var retainedAccountCount = await session.AccountCount(ct);
        retainedAccountCount.ShouldBe(1);
        var retained = await session.Accounts.GetById(account.Id, ct);
        retained.ShouldNotBeNull();
        retained.Version.ShouldBe(account.Version);
    }
}
