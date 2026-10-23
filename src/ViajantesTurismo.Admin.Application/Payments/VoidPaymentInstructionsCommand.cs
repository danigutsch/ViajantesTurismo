using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests the explicit VoidPaymentInstructions action for one immutable document revision.</summary>
public sealed record VoidPaymentInstructionsCommand(Guid DocumentId, UpdatePaymentInstructionsDto Input, string ActorId, string? IdempotencyKey);
