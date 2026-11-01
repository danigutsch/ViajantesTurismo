using PandaBear.Idempotency;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests ApproveBookingPaymentSchedule with authenticated actor metadata and stable operation ownership.</summary>
public sealed record ApproveBookingPaymentScheduleCommand(Guid BookingId, Guid ScheduleId, ApproveFinancialRecordDto Input, string ActorId, IdempotencyKey IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "ApproveBookingPaymentScheduleCommand [redacted]";
}
