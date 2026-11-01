using PandaBear.Idempotency;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests an explicit WithdrawPaymentAccountVerification account operation.</summary>
public sealed record WithdrawPaymentAccountVerificationCommand(Guid AccountId, WithdrawPaymentAccountVerificationDto Input, string ActorId, IdempotencyKey IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "WithdrawPaymentAccountVerificationCommand [redacted]";
}
