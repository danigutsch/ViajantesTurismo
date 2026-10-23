using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests RecordBookingReceipt with authenticated actor metadata and stable operation ownership.</summary>
public sealed record RecordBookingReceiptCommand(Guid BookingId, CreateBookingReceiptDto Input, string ActorId, string? IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "RecordBookingReceiptCommand [redacted]";
}
