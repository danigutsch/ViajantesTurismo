using PandaBear.Idempotency;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests an explicit UpdatePaymentAccount account operation.</summary>
public sealed record UpdatePaymentAccountCommand(Guid AccountId, UpdatePaymentAccountDto Input, string ActorId, IdempotencyKey IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "UpdatePaymentAccountCommand [redacted]";
}
