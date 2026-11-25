using SharedKernel.Idempotency;
using SharedKernel.Mediator;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests an explicit VerifyPaymentAccount account operation.</summary>
public sealed record VerifyPaymentAccountCommand(Guid AccountId, VerifyPaymentAccountDto Input, string ActorId, IdempotencyKey IdempotencyKey) : ICommand<Result<Guid>>, IPaymentCommand
{
    /// <summary>Gets the durable idempotency scope for this operation.</summary>
    internal string OperationScope => "payment-account:VerifyPaymentAccount:" + AccountId.ToString("N");

    /// <inheritdoc />
    public IReadOnlyList<string> TransactionLocks => PaymentCommandLocks.ForOperation(OperationScope, IdempotencyKey, PaymentCommandLocks.Account(AccountId));

    /// <inheritdoc />
    public override string ToString() => "VerifyPaymentAccountCommand [redacted]";
}
