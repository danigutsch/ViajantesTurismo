using PandaBear.BuildingBlocks;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Payments;

/// <summary>Persists approved instruction choices alongside paid installments and ineligible quotes.</summary>
internal static class PaymentInstructionChoiceScenario
{
    /// <summary>Seeds a booking with enough actual choice records to exercise both page boundaries.</summary>
    internal static async Task<(Guid BookingId, long Version, Guid FirstQuote, Guid LastQuote, Guid WithdrawnQuote, Guid PaidInstallment, Guid FirstInstallment, Guid LastInstallment)> Seed(
        PostgreSqlPaymentScenario scenario, int eligibleCount, CancellationToken ct)
    {
        var now = scenario.Clock.GetUtcNow();
        var tour = EntityBuilders.BuildTour(new TourOptions(Pricing: new TourPricingOptions(Currency: Currency.Eur)));
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        var dueDate = new DateOnly(2030, 1, 10);
        var settled = PaymentInstallment.Create(100m - eligibleCount * 0.1m, dueDate).Value;
        PaymentInstallment[] outstanding = [.. Enumerable.Range(0, eligibleCount).Select(_ => PaymentInstallment.Create(0.1m, dueDate).Value)];
        var schedule = tour.DraftBookingPaymentSchedule(booking.Id, 0, 100m, [settled, .. outstanding], "owner", now).Value;
        var history = booking.FinancialHistory.ShouldNotBeNull();
        tour.ApproveBookingPaymentSchedule(booking.Id, history.Version, schedule,
            FinancialApproval.Create("owner", now, FinancialReason.InitialObligation, true).Value).IsSuccess.ShouldBeTrue();
        var amount = settled.Amount.Amount;
        var selection = InstallmentAllocation.Create(settled.Id, amount).Value;
        var receipt = tour.RecordBookingReceipt(booking.Id, history.Version, ReceiptSettlement.Create(Currency.Eur, amount, 0m).Value,
            DateOnly.FromDateTime(now.UtcDateTime), now, PaymentMethod.BankTransfer, "settled-installment", [selection], "owner", now,
            DateOnly.FromDateTime(now.UtcDateTime)).Value;
        var reconciliation = history.DraftReconciliation(history.Version, receipt, amount, null, [selection], "owner", now).Value;
        history.ApproveReconciliation(history.Version, reconciliation,
            FinancialApproval.Create("owner", now, FinancialReason.LateReceipt, true).Value).IsSuccess.ShouldBeTrue();

        var terms = AgreedFxQuote.Create(5m, "synthetic source", "synthetic reference", now, now.AddDays(1), now).Value;
        var approval = FinancialApproval.Create("owner", now, FinancialReason.QuoteAgreed, true).Value;
        var withdrawn = history.DraftQuote(history.Version, terms, false, "owner", now).Value;
        history.ApproveQuote(history.Version, withdrawn, approval).IsSuccess.ShouldBeTrue();
        history.WithdrawApproval(history.Version, withdrawn,
            FinancialApproval.Create("owner", now, FinancialReason.ApprovalWithdrawn, true).Value).IsSuccess.ShouldBeTrue();
        var refund = history.DraftQuote(history.Version, terms, true, "owner", now).Value;
        history.ApproveQuote(history.Version, refund, approval).IsSuccess.ShouldBeTrue();
        for (var index = 0; index < 40; index++)
        {
            history.DraftQuote(history.Version, terms, false, "owner", now).IsSuccess.ShouldBeTrue();
        }

        var eligible = new List<Guid>(eligibleCount);
        for (var index = 0; index < eligibleCount; index++)
        {
            var quote = history.DraftQuote(history.Version, terms, false, "owner", now).Value;
            history.ApproveQuote(history.Version, quote, approval).IsSuccess.ShouldBeTrue();
            eligible.Add(quote);
        }

        await using var session = scenario.Open();
        await session.Migrate(ct);
        await session.Seed(tour, ct);
        return (booking.Id, history.Version, eligible[0], eligible[^1], withdrawn, settled.Id, outstanding[0].Id, outstanding[^1].Id);
    }
}
