using PandaBear.Results;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Application.Mappings;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes DraftBookingPaymentRefund through the owning Tour and an atomic financial transaction.</summary>
public sealed class DraftBookingPaymentRefundCommandHandler(BookingPaymentCommandExecutor executor, TimeProvider clock)
{
    /// <summary>Validates and applies the explicit financial operation.</summary>
    public Task<Result<Guid>> Handle(DraftBookingPaymentRefundCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        var input = command.Input;
        var allocations = FinancialMappings.Allocations(input.ReversedAllocations);
        if (allocations.IsFailure)
        {
            return Task.FromResult(allocations.ConvertError<IReadOnlyList<InstallmentAllocation>, Guid>());
        }
        input = input with
        {
            ReversedAllocations = [.. input.ReversedAllocations.OrderBy(item => item.TargetId)]
        };
        var fingerprint = PaymentCommandFingerprint.Create(command.BookingId, command.ReceiptId, command.ActorId, input, PaymentFinancialContractJsonContext.Default.CreatePaymentRefundDto);
        return executor.Execute(command.BookingId, "DraftBookingPaymentRefund", command.IdempotencyKey, fingerprint, tour =>
        {
            var now = clock.GetUtcNow();
            return Task.FromResult(tour.DraftBookingPaymentRefund(command.BookingId, input.ExpectedVersion, command.ReceiptId, input.OriginalCashBasis, input.EuroCreditReversed, input.EuroCashPayable, input.RefundQuoteId, allocations.Value, input.Reference, input.PerformedAt, command.ActorId, now, input.ActualSettlement));
        }, ct);
    }
}
