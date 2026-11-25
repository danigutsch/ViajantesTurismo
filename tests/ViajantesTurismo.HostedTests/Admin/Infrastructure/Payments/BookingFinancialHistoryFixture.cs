using SharedKernel.BuildingBlocks;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Payments;

/// <summary>Creates realistic longitudinal booking financial data in the existing PostgreSQL scenario.</summary>
internal static class BookingFinancialHistoryFixture
{
    /// <summary>Persists a booking with 53 protected receipt references and a second booking with one receipt.</summary>
    public static async Task<(Guid BookingId, Guid OtherBookingId, Guid OtherReceiptId, IReadOnlyList<(Guid Id, string Reference)> Receipts)> Seed(
        PostgreSqlPaymentScenario scenario, CancellationToken ct)
    {
        const int receiptCount = 53;
        var now = scenario.Clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var tour = EntityBuilders.BuildTour(new TourOptions(Pricing: new TourPricingOptions(Currency: Currency.Eur)));
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        var other = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        var installments = Enumerable.Range(1, 6)
            .Select(day => PaymentInstallment.Create(5m, today.AddDays(day)).Value)
            .Append(PaymentInstallment.Create(booking.TotalPrice - 30m, today.AddDays(7)).Value)
            .ToArray();
        var termsAt = now.AddHours(-2);
        var schedule = tour.DraftBookingPaymentSchedule(booking.Id, 0, booking.TotalPrice, installments, "owner", termsAt).Value;
        var history = booking.FinancialHistory.ShouldNotBeNull();
        tour.ApproveBookingPaymentSchedule(booking.Id, history.Version, schedule,
            FinancialApproval.Create("owner", termsAt, FinancialReason.InitialObligation, true).Value).IsSuccess.ShouldBeTrue();

        var settlement = ReceiptSettlement.Create(Currency.Eur, 5m, 0m).Value;
        var proposed = InstallmentAllocation.Create(installments[0].Id, 5m).Value;
        var receipts = new List<(Guid Id, string Reference)>(receiptCount);
        for (var index = 1; index <= receiptCount; index++)
        {
            var recordedAt = now.AddMinutes(index - receiptCount);
            var reference = FormattableString.Invariant($"synthetic-bank-receipt-{index:D4}");
            var result = tour.RecordBookingReceipt(booking.Id, history.Version, settlement, today, recordedAt,
                PaymentMethod.BankTransfer, reference, [proposed], "owner", recordedAt, today);
            result.IsSuccess.ShouldBeTrue();
            receipts.Add((result.Value, reference));
        }

        var otherReceipt = tour.RecordBookingReceipt(other.Id, 0, settlement, today, now,
            PaymentMethod.BankTransfer, "synthetic-other-booking-receipt", [], "owner", now, today).Value;
        await using var session = scenario.Open();
        await session.Migrate(ct);
        await session.Seed(tour, ct);
        return (booking.Id, other.Id, otherReceipt, receipts);
    }
}
