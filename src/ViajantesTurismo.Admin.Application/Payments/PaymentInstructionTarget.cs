namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Identifies an immutable instruction revision and the payment resources it depends on.</summary>
public sealed record PaymentInstructionTarget(Guid DocumentId, Guid BookingId, Guid AccountId, int Revision);
