using PandaBear.Results;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Application.Mappings;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes RecordBookingReceipt through the owning Tour and an atomic financial transaction.</summary>
public sealed class RecordBookingReceiptCommandHandler(BookingPaymentCommandExecutor executor, TimeProvider clock, PaymentBusinessCalendar calendar)
{
    /// <summary>Validates and applies the explicit financial operation.</summary>
    public Task<Result<Guid>> Handle(RecordBookingReceiptCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        var input = command.Input;
        var allocations = FinancialMappings.Allocations(input.ProposedAllocations);
        if (allocations.IsFailure)
        {
            return Task.FromResult(allocations.ConvertError<IReadOnlyList<InstallmentAllocation>, Guid>());
        }
        input = input with
        {
            ProposedAllocations = input.ProposedAllocations.OrderBy(item => item.TargetId).ToArray()
        };
        if (!Enum.IsDefined(input.SettlementCurrency) || !Enum.IsDefined(input.Method))
        {
            return Task.FromResult(FinancialErrors.Invalid("settlement").ConvertError<Guid>());
        }
        var settlement = ReceiptSettlement.Create(TourMapper.MapToCurrency(input.SettlementCurrency), input.Gross, input.Fees);
        if (settlement.IsFailure)
        {
            return Task.FromResult(settlement.ConvertError<ReceiptSettlement, Guid>());
        }
        var fingerprint = PaymentCommandFingerprint.Create(command.BookingId, null, command.ActorId, input, PaymentFinancialContractJsonContext.Default.CreateBookingReceiptDto);
        return executor.Execute(command.BookingId, "RecordBookingReceipt", command.IdempotencyKey, fingerprint, tour =>
        {
            var now = clock.GetUtcNow();
            return Task.FromResult(tour.RecordBookingReceipt(command.BookingId, input.ExpectedVersion, settlement.Value, input.PaymentDate, input.SettledAt, BookingMapper.MapToPaymentMethod(input.Method), input.Reference, allocations.Value, command.ActorId, now, calendar.At(now)));
        }, ct);
    }
}
