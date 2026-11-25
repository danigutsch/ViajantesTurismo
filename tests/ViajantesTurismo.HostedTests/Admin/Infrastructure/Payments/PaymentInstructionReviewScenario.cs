using SharedKernel.BuildingBlocks;
using SharedKernel.Idempotency;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Payments;

/// <summary>Seeds a real instruction revision with a long review history and a known latest decision.</summary>
internal static class PaymentInstructionReviewScenario
{
    /// <summary>Identifies the newest review by insertion sequence.</summary>
    internal const string LatestReviewer = "latest-reviewer";

    /// <summary>Creates a protected instruction and adds reviews with a distinct last decision.</summary>
    internal static async Task<(Guid BookingId, Guid InstructionId)> Seed(PostgreSqlPaymentScenario scenario, int reviewCount, CancellationToken ct)
    {
        var now = scenario.Clock.GetUtcNow();
        var tour = EntityBuilders.BuildTour(new TourOptions(Pricing: new TourPricingOptions(Currency: Currency.Eur)));
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        var installment = PaymentInstallment.Create(100m, new DateOnly(2030, 1, 10)).Value;
        var schedule = tour.DraftBookingPaymentSchedule(booking.Id, 0, 100m, [installment], "owner", now).Value;
        var history = booking.FinancialHistory.ShouldNotBeNull();
        tour.ApproveBookingPaymentSchedule(booking.Id, history.Version, schedule,
            FinancialApproval.Create("owner", now, FinancialReason.InitialObligation, true).Value).IsSuccess.ShouldBeTrue();
        var recipient = PaymentRecipientDetails.CreateIban("Synthetic recipient", "DE89370400440532013000", "COBADEFFXXX", Currency.Eur).Value;
        var account = PaymentAccount.Create("Verified recipient", recipient, "owner", now).Value;
        account.Verify(account.Version, "owner", now, PaymentAccountVerificationMethod.BankStatement, now, true, true).IsSuccess.ShouldBeTrue();
        var request = new GeneratePaymentInstructionsDto
        {
            ExpectedVersion = history.Version,
            AccountId = account.Id,
            ExpectedAccountVersion = account.Version,
            InstallmentIds = [installment.Id],
        };
        await using var session = scenario.Open();
        await session.Migrate(ct);
        await session.Seed(tour, ct);
        await session.SeedAccount(account, ct);
        var result = await session.Send(new GeneratePaymentInstructionsCommand(booking.Id, null, request, "owner", IdempotencyKey.From("review-load-instruction")), ct);
        result.IsSuccess.ShouldBeTrue();

        var earlierReviews = Enumerable.Range(1, reviewCount - 1).Select(index =>
            new PaymentInstructionAuditMetadata(result.Value, 1, $"reviewer-{index:D4}", now,
                PaymentInstructionAuditOperation.ReviewAccepted, FinancialReason.ReviewAccepted, false));
        await session.SeedInstructionAudits(booking.Id, earlierReviews, ct);
        await session.SeedInstructionAudits(booking.Id,
            [new PaymentInstructionAuditMetadata(result.Value, 1, LatestReviewer, now.AddMinutes(-1),
                PaymentInstructionAuditOperation.ReviewChangesRequested, FinancialReason.ReviewChangesRequested, false)], ct);
        return (booking.Id, result.Value);
    }
}
