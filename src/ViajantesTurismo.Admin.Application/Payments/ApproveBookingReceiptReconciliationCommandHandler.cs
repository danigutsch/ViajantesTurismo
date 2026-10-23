using PandaBear.Results;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Application.Mappings;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes ApproveBookingReceiptReconciliation through the owning Tour and an atomic financial transaction.</summary>
public sealed class ApproveBookingReceiptReconciliationCommandHandler(BookingPaymentCommandExecutor executor, TimeProvider clock, IPaymentInstructionStore instructions, PaymentBusinessCalendar calendar)
{
    /// <summary>Validates and applies the explicit financial operation.</summary>
    public Task<Result<Guid>> Handle(ApproveBookingReceiptReconciliationCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        var input = command.Input;
        if (!Enum.IsDefined(input.Reason))
        {
            return Task.FromResult(FinancialErrors.Invalid("reason").ConvertError<Guid>());
        }
        var fingerprint = PaymentCommandFingerprint.Create(command.BookingId, command.ReconciliationId, command.ActorId, input, PaymentFinancialContractJsonContext.Default.ApproveFinancialRecordDto);
        return executor.Execute(command.BookingId, "ApproveBookingReceiptReconciliation", command.IdempotencyKey, fingerprint, async tour =>
        {
            var now = clock.GetUtcNow();
            var approval = FinancialApproval.Create(command.ActorId, now, FinancialMappings.Map(input.Reason), input.Confirmed);
            if (approval.IsFailure)
            {
                return approval.ConvertError<FinancialApproval, Guid>();
            }

            var booking = tour.Bookings.Single(item => item.Id == command.BookingId);
            var documentId = booking.FinancialHistory?.GetReconciliationInstructionId(command.ReconciliationId);
            var snapshot = documentId is { } id ? await instructions.GetSnapshot(id, ct) : null;
            if (documentId is not null && (snapshot is null || snapshot.Terms.BookingId != command.BookingId))
            {
                return FinancialErrors.Invalid("instruction").ConvertError<Guid>();
            }
            if (approval.Value.Reason == FinancialReason.ExactReceipt && (snapshot?.FinalizedAt is not { } finalizedAt
                || booking.FinancialHistory?.GetReconciliationSettlementInstant(command.ReconciliationId) is not { } settledAt || settledAt < finalizedAt))
            {
                return FinancialErrors.Invalid("frozenReceiptEvidence").ConvertError<Guid>();
            }
            var settlementInstant = booking.FinancialHistory?.GetReconciliationSettlementInstant(command.ReconciliationId);
            return tour.ApproveBookingReceiptReconciliation(command.BookingId, input.ExpectedVersion, command.ReconciliationId, approval.Value, snapshot?.Terms, settlementInstant is { } instant ? calendar.At(instant) : null);
        }, ct);
    }
}
