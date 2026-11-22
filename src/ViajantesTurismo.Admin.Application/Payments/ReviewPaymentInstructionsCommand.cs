using PandaBear.Idempotency;
using PandaBear.Mediator;
using PandaBear.Results;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests the explicit ReviewPaymentInstructions action for one immutable document revision.</summary>
public sealed record ReviewPaymentInstructionsCommand(PaymentInstructionTarget Target, ReviewPaymentInstructionsDto Input, string ActorId, IdempotencyKey IdempotencyKey) : ICommand<Result<Guid>>, IPaymentCommand
{
    /// <summary>Gets the durable idempotency scope for this operation.</summary>
    internal string OperationScope => "payment-instruction:review:" + Target.DocumentId.ToString("N");

    /// <inheritdoc />
    public IReadOnlyList<string> TransactionLocks => PaymentCommandLocks.ForOperation(OperationScope, IdempotencyKey,
        PaymentCommandLocks.Booking(Target.BookingId), PaymentCommandLocks.Account(Target.AccountId));

    /// <inheritdoc />
    public override string ToString() => "ReviewPaymentInstructionsCommand [redacted]";
}
