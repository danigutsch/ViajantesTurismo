using PandaBear.Results;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Application.Mappings;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes DraftBookingReceiptReconciliation through the owning Tour and an atomic financial transaction.</summary>
public sealed class DraftBookingReceiptReconciliationCommandHandler(BookingPaymentCommandExecutor executor, TimeProvider clock)
{
    /// <summary>Validates and applies the explicit financial operation.</summary>
    public Task<Result<Guid>> Handle(DraftBookingReceiptReconciliationCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        var input = command.Input;
        var allocations = FinancialMappings.Allocations(input.Allocations);
        if (allocations.IsFailure)
        {
            return Task.FromResult(allocations.ConvertError<IReadOnlyList<InstallmentAllocation>, Guid>());
        }
        input = input with
        {
            Allocations = input.Allocations.OrderBy(item => item.TargetId).ToArray()
        };
        var fingerprint = PaymentCommandFingerprint.Create(command.BookingId, command.ReceiptId, command.ActorId, input, PaymentFinancialContractJsonContext.Default.CreateReceiptReconciliationDto);
        return executor.Execute(command.BookingId, "DraftBookingReceiptReconciliation", command.IdempotencyKey, fingerprint, tour =>
        {
            var now = clock.GetUtcNow();
            return Task.FromResult(tour.DraftBookingReceiptReconciliation(command.BookingId, input.ExpectedVersion, command.ReceiptId, input.EuroCredit, input.QuoteId, allocations.Value, command.ActorId, now, input.InstructionId));
        }, ct);
    }
}
