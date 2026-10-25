using PostgreSqlTestDatabase = PandaBear.Npgsql.Testing.PostgreSqlTestDatabaseLease;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.DataProtection;
using ViajantesTurismo.Admin.Infrastructure.Payments;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Payments;

internal sealed class PostgreSqlPaymentScenario : IAsyncDisposable
{
    private readonly PostgreSqlTestDatabase _database;
    private readonly PaymentProtection _protection = new(new EphemeralDataProtectionProvider());
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2030, 1, 5, 12, 0, 0, TimeSpan.Zero));
    private PostgreSqlPaymentScenario(PostgreSqlTestDatabase database) => _database = database;

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The scenario owns and disposes its fixture-issued test database.")]
    public static async Task<PostgreSqlPaymentScenario> Create(PostgreSqlTestServerFixture fixture, CancellationToken ct) => new(await fixture.CreateDatabase(ct));
    public PaymentTestSession Open() => PaymentTestSession.Create(_database.ConnectionString, _protection, Clock);
    public async Task<(Guid BookingId, Guid ReceiptId, Guid InstallmentId)> SeedCustomerCredit(CancellationToken ct)
    {
        var now = Clock.GetUtcNow();
        var tour = Testing.Behavior.EntityBuilders.BuildTour(new Testing.Behavior.TourOptions(
            Pricing: new Testing.Behavior.TourPricingOptions(Currency: Domain.Tours.Currency.Euro)));
        var booking = Testing.Behavior.BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        var installment = Domain.Payments.PaymentInstallment.Create(100m, new DateOnly(2030, 1, 10)).Value;
        var schedule = tour.DraftBookingPaymentSchedule(booking.Id, 0, 100m, [installment], "owner", now).Value;
        tour.ApproveBookingPaymentSchedule(booking.Id, booking.FinancialHistory?.Version ?? 0, schedule,
            Domain.Payments.FinancialApproval.Create("owner", now, Domain.Payments.FinancialReason.InitialObligation, true).Value).IsSuccess.ShouldBeTrue();
        var receipt = tour.RecordBookingReceipt(booking.Id, booking.FinancialHistory?.Version ?? 0,
            Domain.Payments.ReceiptSettlement.Create(Domain.Tours.Currency.Euro, 120m, 0m).Value, new DateOnly(2030, 1, 5), now,
            Domain.Tours.PaymentMethod.BankTransfer, "receipt", [Domain.Payments.InstallmentAllocation.Create(installment.Id, 100m).Value], "owner", now, new DateOnly(2030, 1, 5)).Value;
        var reconciliation = tour.DraftBookingReceiptReconciliation(booking.Id, booking.FinancialHistory?.Version ?? 0, receipt, 120m, null, [], "owner", now).Value;
        tour.ApproveBookingReceiptReconciliation(booking.Id, booking.FinancialHistory?.Version ?? 0, reconciliation,
            Domain.Payments.FinancialApproval.Create("owner", now, Domain.Payments.FinancialReason.ExcessReceipt, true).Value).IsSuccess.ShouldBeTrue();
        await using var session = Open();
        await session.Migrate(ct);
        await session.Seed(tour, ct);
        return (booking.Id, receipt, installment.Id);
    }
    public async ValueTask DisposeAsync()
    {
        _protection.Dispose();
        await _database.DisposeAsync();
    }
}
