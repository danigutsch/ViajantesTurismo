using PandaBear.Idempotency;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests the explicit ApprovePaymentInstructions action for one immutable document revision.</summary>
public sealed record ApprovePaymentInstructionsCommand(Guid DocumentId, UpdatePaymentInstructionsDto Input, string ActorId, IdempotencyKey IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "ApprovePaymentInstructionsCommand [redacted]";
}
