using SharedKernel.Idempotency;
using SharedKernel.Mediator;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests a new immutable payment instruction revision with required source-version preconditions.</summary>
public sealed record GeneratePaymentInstructionsCommand(Guid BookingId, Guid? ReplacesDocumentId, GeneratePaymentInstructionsDto Input, string ActorId, IdempotencyKey IdempotencyKey) : ICommand<Result<Guid>>, IPaymentCommand
{
    /// <summary>Gets the durable idempotency scope for this operation.</summary>
    internal string OperationScope => "payment-instructions:generate:" + BookingId.ToString("N");

    /// <inheritdoc />
    public IReadOnlyList<string> TransactionLocks => PaymentCommandLocks.ForOperation(OperationScope, IdempotencyKey, PaymentCommandLocks.Booking(BookingId), PaymentCommandLocks.Account(Input.AccountId));

    /// <inheritdoc />
    public override string ToString() => "GeneratePaymentInstructionsCommand [redacted]";
}
