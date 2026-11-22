using PandaBear.Idempotency;
using PandaBear.Mediator;
using PandaBear.Results;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests an explicit ReviewPaymentAccount account operation.</summary>
public sealed record ReviewPaymentAccountCommand(Guid AccountId, ReviewPaymentAccountDto Input, string ActorId, IdempotencyKey IdempotencyKey) : ICommand<Result<Guid>>, IPaymentCommand
{
    /// <summary>Gets the durable idempotency scope for this operation.</summary>
    internal string OperationScope => "payment-account:ReviewPaymentAccount:" + AccountId.ToString("N");

    /// <inheritdoc />
    public IReadOnlyList<string> TransactionLocks => PaymentCommandLocks.ForOperation(OperationScope, IdempotencyKey, PaymentCommandLocks.Account(AccountId));

    /// <inheritdoc />
    public override string ToString() => "ReviewPaymentAccountCommand [redacted]";
}
