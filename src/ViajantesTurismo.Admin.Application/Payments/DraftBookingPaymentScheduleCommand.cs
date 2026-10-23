using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests DraftBookingPaymentSchedule with authenticated actor metadata and stable operation ownership.</summary>
public sealed record DraftBookingPaymentScheduleCommand(Guid BookingId, CreatePaymentScheduleDto Input, string ActorId, string? IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "DraftBookingPaymentScheduleCommand [redacted]";
}
