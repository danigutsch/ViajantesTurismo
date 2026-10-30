using PostgreSqlTestDatabase = PandaBear.Npgsql.Testing.PostgreSqlTestDatabaseLease;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.DataProtection;
using ViajantesTurismo.Admin.Infrastructure.Payments;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Payments;

/// <summary>Arranges an isolated PostgreSQL database with ephemeral payment protection and a controllable clock for payment tests.</summary>
internal sealed class PostgreSqlPaymentScenario : IAsyncDisposable
{
    private readonly PostgreSqlTestDatabase _database;
    private readonly PaymentProtection _protection = new(new EphemeralDataProtectionProvider());
    /// <summary>Controllable clock starting at 2030-01-05 12:00 UTC.</summary>
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2030, 1, 5, 12, 0, 0, TimeSpan.Zero));
    private PostgreSqlPaymentScenario(PostgreSqlTestDatabase database) => _database = database;

    /// <summary>Creates a scenario over a database issued by <paramref name="fixture" />.</summary>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The scenario owns and disposes its fixture-issued test database.")]
    public static async Task<PostgreSqlPaymentScenario> Create(PostgreSqlTestServerFixture fixture, CancellationToken ct) => new(await fixture.CreateDatabase(ct));
    /// <summary>Opens a payment test session against the scenario database.</summary>
    public PaymentTestSession Open() => PaymentTestSession.Create(_database.ConnectionString, _protection, Clock);
    /// <summary>Seeds a tour with an approved schedule and a 120 euro receipt against a 100 euro installment, leaving 20 euro of customer credit, and returns the booking, receipt and installment ids.</summary>
    public async Task<(Guid BookingId, Guid ReceiptId, Guid InstallmentId)> SeedCustomerCredit(CancellationToken ct)
    {
        var now = Clock.GetUtcNow();
        var tour = ViajantesTurismo.Admin.Testing.Behavior.EntityBuilders.BuildTour(new ViajantesTurismo.Admin.Testing.Behavior.TourOptions(
            Pricing: new ViajantesTurismo.Admin.Testing.Behavior.TourPricingOptions(Currency: ViajantesTurismo.Admin.Domain.Tours.Currency.Euro)));
        var booking = ViajantesTurismo.Admin.Testing.Behavior.BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        var installment = ViajantesTurismo.Admin.Domain.Payments.PaymentInstallment.Create(100m, new DateOnly(2030, 1, 10)).Value;
        var schedule = tour.DraftBookingPaymentSchedule(booking.Id, 0, 100m, [installment], "owner", now).Value;
        tour.ApproveBookingPaymentSchedule(booking.Id, booking.FinancialHistory?.Version ?? 0, schedule,
            ViajantesTurismo.Admin.Domain.Payments.FinancialApproval.Create("owner", now, ViajantesTurismo.Admin.Domain.Payments.FinancialReason.InitialObligation, true).Value).IsSuccess.ShouldBeTrue();
        var receipt = tour.RecordBookingReceipt(booking.Id, booking.FinancialHistory?.Version ?? 0,
            ViajantesTurismo.Admin.Domain.Payments.ReceiptSettlement.Create(ViajantesTurismo.Admin.Domain.Tours.Currency.Euro, 120m, 0m).Value, new DateOnly(2030, 1, 5), now,
            ViajantesTurismo.Admin.Domain.Tours.PaymentMethod.BankTransfer, "receipt", [ViajantesTurismo.Admin.Domain.Payments.InstallmentAllocation.Create(installment.Id, 100m).Value], "owner", now, new DateOnly(2030, 1, 5)).Value;
        var reconciliation = tour.DraftBookingReceiptReconciliation(booking.Id, booking.FinancialHistory?.Version ?? 0, receipt, 120m, null, [], "owner", now).Value;
        tour.ApproveBookingReceiptReconciliation(booking.Id, booking.FinancialHistory?.Version ?? 0, reconciliation,
            ViajantesTurismo.Admin.Domain.Payments.FinancialApproval.Create("owner", now, ViajantesTurismo.Admin.Domain.Payments.FinancialReason.ExcessReceipt, true).Value).IsSuccess.ShouldBeTrue();
        await using var session = Open();
        await session.Migrate(ct);
        await session.Seed(tour, ct);
        return (booking.Id, receipt, installment.Id);
    }
    /// <summary>Seeds a booking whose history holds one receipt reversal, refund correction, approval withdrawal and allocation release, and returns the booking id.</summary>
    public async Task<Guid> SeedFinancialCorrections(CancellationToken ct)
    {
        var now = Clock.GetUtcNow();
        var tour = ViajantesTurismo.Admin.Testing.Behavior.EntityBuilders.BuildTour(new ViajantesTurismo.Admin.Testing.Behavior.TourOptions(
            Pricing: new ViajantesTurismo.Admin.Testing.Behavior.TourPricingOptions(Currency: ViajantesTurismo.Admin.Domain.Tours.Currency.Euro)));
        var booking = ViajantesTurismo.Admin.Testing.Behavior.BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        var installment = ViajantesTurismo.Admin.Domain.Payments.PaymentInstallment.Create(100m, new DateOnly(2030, 1, 10)).Value;
        var schedule = tour.DraftBookingPaymentSchedule(booking.Id, 0, 100m, [installment], "owner", now).Value;
        tour.ApproveBookingPaymentSchedule(booking.Id, booking.FinancialHistory?.Version ?? 0, schedule,
            ViajantesTurismo.Admin.Domain.Payments.FinancialApproval.Create("owner", now, ViajantesTurismo.Admin.Domain.Payments.FinancialReason.InitialObligation, true).Value).IsSuccess.ShouldBeTrue();
        var history = booking.FinancialHistory.ShouldNotBeNull();
        var receipt = RecordReceipt(tour, booking, installment, 120m, now);
        var reconciliation = history.DraftReconciliation(history.Version, receipt, 120m, null,
            [ViajantesTurismo.Admin.Domain.Payments.InstallmentAllocation.Create(installment.Id, 60m).Value], "owner", now).Value;
        history.ApproveReconciliation(history.Version, reconciliation,
            ViajantesTurismo.Admin.Domain.Payments.FinancialApproval.Create("owner", now, ViajantesTurismo.Admin.Domain.Payments.FinancialReason.ExcessReceipt, true).Value).IsSuccess.ShouldBeTrue();
        var quote = history.DraftQuote(history.Version, ViajantesTurismo.Admin.Domain.Payments.AgreedFxQuote.Create(5m, "bank", "agreement", now, now.AddHours(1), now).Value, false, "owner", now).Value;
        history.ApproveQuote(history.Version, quote,
            ViajantesTurismo.Admin.Domain.Payments.FinancialApproval.Create("owner", now, ViajantesTurismo.Admin.Domain.Payments.FinancialReason.QuoteAgreed, true).Value).IsSuccess.ShouldBeTrue();
        var allocation = history.ExportState().Allocations.Single();
        var refund = tour.DraftBookingPaymentRefund(booking.Id, history.Version, receipt, 20m, 20m, 20m, null,
            [ViajantesTurismo.Admin.Domain.Payments.InstallmentAllocation.Create(allocation.Id, 10m).Value], "refund", now, "owner", now).Value;
        history.ApproveRefund(history.Version, refund,
            ViajantesTurismo.Admin.Domain.Payments.FinancialApproval.Create("owner", now, ViajantesTurismo.Admin.Domain.Payments.FinancialReason.RefundPerformed, true).Value).IsSuccess.ShouldBeTrue();
        history.CorrectRefund(history.Version, refund,
            ViajantesTurismo.Admin.Domain.Payments.FinancialApproval.Create("owner", now, ViajantesTurismo.Admin.Domain.Payments.FinancialReason.RefundRecordError, true).Value).IsSuccess.ShouldBeTrue();
        history.ReleaseAllocations(history.Version, [ViajantesTurismo.Admin.Domain.Payments.InstallmentAllocation.Create(allocation.Id, 5m).Value],
            ViajantesTurismo.Admin.Domain.Payments.FinancialApproval.Create("owner", now, ViajantesTurismo.Admin.Domain.Payments.FinancialReason.AllocationSelected, true).Value).IsSuccess.ShouldBeTrue();
        history.WithdrawApproval(history.Version, quote,
            ViajantesTurismo.Admin.Domain.Payments.FinancialApproval.Create("owner", now, ViajantesTurismo.Admin.Domain.Payments.FinancialReason.ApprovalWithdrawn, true).Value).IsSuccess.ShouldBeTrue();
        var erroneous = RecordReceipt(tour, booking, installment, 10m, now);
        history.ReverseReceipt(history.Version, erroneous,
            ViajantesTurismo.Admin.Domain.Payments.FinancialApproval.Create("owner", now, ViajantesTurismo.Admin.Domain.Payments.FinancialReason.ReceiptError, true).Value).IsSuccess.ShouldBeTrue();
        await using var session = Open();
        await session.Migrate(ct);
        await session.Seed(tour, ct);
        return booking.Id;
    }
    private static Guid RecordReceipt(ViajantesTurismo.Admin.Domain.Tours.Tour tour, ViajantesTurismo.Admin.Domain.Tours.Booking booking, ViajantesTurismo.Admin.Domain.Payments.PaymentInstallment installment, decimal gross, DateTimeOffset now) =>
        tour.RecordBookingReceipt(booking.Id, booking.FinancialHistory?.Version ?? 0,
            ViajantesTurismo.Admin.Domain.Payments.ReceiptSettlement.Create(ViajantesTurismo.Admin.Domain.Tours.Currency.Euro, gross, 0m).Value, new DateOnly(2030, 1, 5), now,
            ViajantesTurismo.Admin.Domain.Tours.PaymentMethod.BankTransfer, "receipt", [ViajantesTurismo.Admin.Domain.Payments.InstallmentAllocation.Create(installment.Id, Math.Min(gross, installment.Amount.Amount)).Value], "owner", now, new DateOnly(2030, 1, 5)).Value;
    /// <summary>Disposes payment protection and the test database.</summary>
    public async ValueTask DisposeAsync()
    {
        _protection.Dispose();
        await _database.DisposeAsync();
    }
}
