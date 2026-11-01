using PandaBear.Idempotency;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests the explicit FinalizePaymentInstructions action for one immutable document revision.</summary>
public sealed record FinalizePaymentInstructionsCommand(Guid DocumentId, UpdatePaymentInstructionsDto Input, string ActorId, IdempotencyKey IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "FinalizePaymentInstructionsCommand [redacted]";
}
