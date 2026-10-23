using PandaBear.Results;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Application.Mappings;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes AllocateBookingPayment through the owning Tour and an atomic financial transaction.</summary>
public sealed class AllocateBookingPaymentCommandHandler(BookingPaymentCommandExecutor executor, TimeProvider clock)
{
    /// <summary>Validates and applies the explicit financial operation.</summary>
    public Task<Result<Guid>> Handle(AllocateBookingPaymentCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        var input = command.Input;
        if (!Enum.IsDefined(input.Reason))
        {
            return Task.FromResult(FinancialErrors.Invalid("reason").ConvertError<Guid>());
        }
        var allocations = FinancialMappings.Allocations(input.Allocations);
        if (allocations.IsFailure)
        {
            return Task.FromResult(allocations.ConvertError<IReadOnlyList<InstallmentAllocation>, Guid>());
        }
        input = input with
        {
            Allocations = input.Allocations.OrderBy(item => item.TargetId).ToArray()
        };
        var fingerprint = PaymentCommandFingerprint.Create(command.BookingId, command.ReceiptId, command.ActorId, input, PaymentFinancialContractJsonContext.Default.AllocatePaymentCreditDto);
        return executor.Execute(command.BookingId, "AllocateBookingPayment", command.IdempotencyKey, fingerprint, tour =>
        {
            var now = clock.GetUtcNow();
            var approval = FinancialApproval.Create(command.ActorId, now, FinancialMappings.Map(input.Reason), input.Confirmed);
            if (approval.IsFailure)
            {
                return Task.FromResult(approval.ConvertError<FinancialApproval, Guid>());
            }
            return Task.FromResult(tour.AllocateBookingPayment(command.BookingId, input.ExpectedVersion, command.ReceiptId, allocations.Value, approval.Value));
        }, ct);
    }
}
