using PandaBear.Idempotency;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests ReviewBookingFinancialRecord with authenticated actor metadata and stable operation ownership.</summary>
public sealed record ReviewBookingFinancialRecordCommand(Guid BookingId, Guid TargetId, ReviewFinancialRecordDto Input, string ActorId, IdempotencyKey IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "ReviewBookingFinancialRecordCommand [redacted]";
}
