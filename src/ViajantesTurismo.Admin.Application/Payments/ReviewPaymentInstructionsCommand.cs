using PandaBear.Idempotency;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests the explicit ReviewPaymentInstructions action for one immutable document revision.</summary>
public sealed record ReviewPaymentInstructionsCommand(Guid DocumentId, ReviewPaymentInstructionsDto Input, string ActorId, IdempotencyKey IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "ReviewPaymentInstructionsCommand [redacted]";
}
