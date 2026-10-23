using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests DraftBookingReceiptReconciliation with authenticated actor metadata and stable operation ownership.</summary>
public sealed record DraftBookingReceiptReconciliationCommand(Guid BookingId, Guid ReceiptId, CreateReceiptReconciliationDto Input, string ActorId, string? IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "DraftBookingReceiptReconciliationCommand [redacted]";
}
