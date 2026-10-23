using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests ReverseBookingReceipt with authenticated actor metadata and stable operation ownership.</summary>
public sealed record ReverseBookingReceiptCommand(Guid BookingId, Guid ReceiptId, ApproveFinancialRecordDto Input, string ActorId, string? IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "ReverseBookingReceiptCommand [redacted]";
}
