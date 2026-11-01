using PandaBear.Idempotency;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests an explicit VerifyPaymentAccount account operation.</summary>
public sealed record VerifyPaymentAccountCommand(Guid AccountId, VerifyPaymentAccountDto Input, string ActorId, IdempotencyKey IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "VerifyPaymentAccountCommand [redacted]";
}
