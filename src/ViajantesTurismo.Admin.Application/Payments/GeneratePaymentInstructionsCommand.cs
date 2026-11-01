using PandaBear.Idempotency;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests a new immutable payment instruction revision with required source-version preconditions.</summary>
public sealed record GeneratePaymentInstructionsCommand(Guid BookingId, Guid? ReplacesDocumentId, GeneratePaymentInstructionsDto Input, string ActorId, IdempotencyKey IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "GeneratePaymentInstructionsCommand [redacted]";
}
