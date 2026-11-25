using SharedKernel.Idempotency;
using SharedKernel.Mediator;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests ReleaseBookingPaymentAllocations with authenticated actor metadata and stable operation ownership.</summary>
public sealed record ReleaseBookingPaymentAllocationsCommand(Guid BookingId, AllocatePaymentCreditDto Input, string ActorId, IdempotencyKey IdempotencyKey) : ICommand<Result<Guid>>, IPaymentCommand
{
    /// <summary>Gets the durable idempotency scope for this operation.</summary>
    internal string OperationScope => "booking-payment:ReleaseBookingPaymentAllocations:" + BookingId.ToString("N");

    /// <inheritdoc />
    public IReadOnlyList<string> TransactionLocks => PaymentCommandLocks.ForOperation(OperationScope, IdempotencyKey, PaymentCommandLocks.Booking(BookingId));

    /// <inheritdoc />
    public override string ToString() => "ReleaseBookingPaymentAllocationsCommand [redacted]";
}
