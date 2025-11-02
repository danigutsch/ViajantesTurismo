using ViajantesTurismo.Admin.Domain.Bookings;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Admin.ApiService.Mapping;

/// <summary>
/// Maps Booking-related DTOs to domain objects.
/// </summary>
internal static class BookingMapper
{
    /// <summary>
    /// Maps a <see cref="BookingStatusDto"/> to a <see cref="BookingStatus"/>.
    /// </summary>
    public static BookingStatus MapToBookingStatus(BookingStatusDto bookingStatusDto)
    {
        return bookingStatusDto switch
        {
            BookingStatusDto.Pending => BookingStatus.Pending,
            BookingStatusDto.Confirmed => BookingStatus.Confirmed,
            BookingStatusDto.Cancelled => BookingStatus.Cancelled,
            BookingStatusDto.Completed => BookingStatus.Completed,
            _ => throw new ArgumentOutOfRangeException(nameof(bookingStatusDto), bookingStatusDto, "Invalid booking status value.")
        };
    }

    /// <summary>
    /// Maps a <see cref="PaymentStatusDto"/> to a <see cref="PaymentStatus"/>.
    /// </summary>
    public static PaymentStatus MapToPaymentStatus(PaymentStatusDto paymentStatusDto)
    {
        return paymentStatusDto switch
        {
            PaymentStatusDto.Unpaid => PaymentStatus.Unpaid,
            PaymentStatusDto.PartiallyPaid => PaymentStatus.PartiallyPaid,
            PaymentStatusDto.Paid => PaymentStatus.Paid,
            PaymentStatusDto.Refunded => PaymentStatus.Refunded,
            _ => throw new ArgumentOutOfRangeException(nameof(paymentStatusDto), paymentStatusDto, "Invalid payment status value.")
        };
    }
}
