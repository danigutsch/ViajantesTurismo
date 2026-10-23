using PandaBear.Results;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Application.Mappings;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes CorrectBookingPaymentRefund through the owning Tour and an atomic financial transaction.</summary>
public sealed class CorrectBookingPaymentRefundCommandHandler(BookingPaymentCommandExecutor executor, TimeProvider clock)
{
    /// <summary>Validates and applies the explicit financial operation.</summary>
    public Task<Result<Guid>> Handle(CorrectBookingPaymentRefundCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        var input = command.Input;
        if (!Enum.IsDefined(input.Reason))
        {
            return Task.FromResult(FinancialErrors.Invalid("reason").ConvertError<Guid>());
        }
        var fingerprint = PaymentCommandFingerprint.Create(command.BookingId, command.RefundId, command.ActorId, input, PaymentFinancialContractJsonContext.Default.ApproveFinancialRecordDto);
        return executor.Execute(command.BookingId, "CorrectBookingPaymentRefund", command.IdempotencyKey, fingerprint, tour =>
        {
            var now = clock.GetUtcNow();
            var approval = FinancialApproval.Create(command.ActorId, now, FinancialMappings.Map(input.Reason), input.Confirmed);
            if (approval.IsFailure)
            {
                return Task.FromResult(approval.ConvertError<FinancialApproval, Guid>());
            }
            return Task.FromResult(tour.CorrectBookingPaymentRefund(command.BookingId, input.ExpectedVersion, command.RefundId, approval.Value));
        }, ct);
    }
}
