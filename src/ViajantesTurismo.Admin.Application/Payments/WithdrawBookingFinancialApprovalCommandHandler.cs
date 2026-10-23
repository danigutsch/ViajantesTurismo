using PandaBear.Results;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Application.Mappings;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes WithdrawBookingFinancialApproval through the owning Tour and an atomic financial transaction.</summary>
public sealed class WithdrawBookingFinancialApprovalCommandHandler(BookingPaymentCommandExecutor executor, TimeProvider clock)
{
    /// <summary>Validates and applies the explicit financial operation.</summary>
    public Task<Result<Guid>> Handle(WithdrawBookingFinancialApprovalCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        var input = command.Input;
        if (!Enum.IsDefined(input.Reason))
        {
            return Task.FromResult(FinancialErrors.Invalid("reason").ConvertError<Guid>());
        }
        var fingerprint = PaymentCommandFingerprint.Create(command.BookingId, command.TargetId, command.ActorId, input, PaymentFinancialContractJsonContext.Default.ApproveFinancialRecordDto);
        return executor.Execute(command.BookingId, "WithdrawBookingFinancialApproval", command.IdempotencyKey, fingerprint, tour =>
        {
            var now = clock.GetUtcNow();
            var approval = FinancialApproval.Create(command.ActorId, now, FinancialMappings.Map(input.Reason), input.Confirmed);
            if (approval.IsFailure)
            {
                return Task.FromResult(approval.ConvertError<FinancialApproval, Guid>());
            }
            return Task.FromResult(tour.WithdrawBookingFinancialApproval(command.BookingId, input.ExpectedVersion, command.TargetId, approval.Value));
        }, ct);
    }
}
