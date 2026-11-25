using SharedKernel.Mediator;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Application.Mappings;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes ReverseBookingReceipt through the owning Tour and an atomic financial transaction.</summary>
public sealed class ReverseBookingReceiptCommandHandler(BookingPaymentOperations executor, TimeProvider clock) : ICommandHandler<ReverseBookingReceiptCommand, Result<Guid>>
{
    /// <summary>Validates and applies the explicit financial operation.</summary>
    public ValueTask<Result<Guid>> Handle(ReverseBookingReceiptCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        var input = command.Input;
        if (!Enum.IsDefined(input.Reason))
        {
            return ValueTask.FromResult(FinancialErrors.Invalid("reason").ConvertError<Guid>());
        }
        var fingerprint = PaymentCommandFingerprint.Create(command.BookingId, command.ReceiptId, command.ActorId, input, PaymentFinancialContractJsonContext.Default.ApproveFinancialRecordDto);
        return executor.Execute(command.BookingId, command.OperationScope, command.IdempotencyKey, fingerprint, tour =>
        {
            var now = clock.GetUtcNow();
            var approval = FinancialApproval.Create(command.ActorId, now, FinancialMappings.Map(input.Reason), input.Confirmed);
            if (approval.IsFailure)
            {
                return Task.FromResult(approval.ConvertError<FinancialApproval, Guid>());
            }
            return Task.FromResult(tour.ReverseBookingReceipt(command.BookingId, input.ExpectedVersion, command.ReceiptId, approval.Value));
        }, ct);
    }
}
