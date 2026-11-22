using PandaBear.Idempotency;
using PandaBear.Mediator;
using PandaBear.Results;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests an unverified recipient draft; actor identity comes from the authenticated principal.</summary>
public sealed record CreatePaymentAccountCommand(CreatePaymentAccountDto Input, string ActorId, IdempotencyKey IdempotencyKey) : ICommand<Result<Guid>>, IPaymentCommand
{
    /// <summary>The durable idempotency scope for a new payment account.</summary>
    internal const string OperationScope = "payment-account:create";

    /// <inheritdoc />
    public IReadOnlyList<string> TransactionLocks => PaymentCommandLocks.ForOperation(OperationScope, IdempotencyKey);

    /// <inheritdoc />
    public override string ToString() => "CreatePaymentAccountCommand [redacted]";
}
