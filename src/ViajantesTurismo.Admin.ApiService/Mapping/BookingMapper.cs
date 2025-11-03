using ViajantesTurismo.Admin.Domain.Bookings;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Admin.ApiService.Mapping;

/// <summary>
/// Maps Booking-related DTOs to domain objects.
/// </summary>
internal static class BookingMapper
{
    /// <summary>
    /// Maps a <see cref="BikeTypeDto"/> to a <see cref="BikeType"/>.
    /// </summary>
    public static BikeType MapToBikeType(BikeTypeDto bikeTypeDto)
    {
        return bikeTypeDto switch
        {
            BikeTypeDto.None => BikeType.None,
            BikeTypeDto.Regular => BikeType.Regular,
            BikeTypeDto.EBike => BikeType.EBike,
            _ => throw new ArgumentOutOfRangeException(nameof(bikeTypeDto), bikeTypeDto, "Invalid bike type value.")
        };
    }

    /// <summary>
    /// Maps a <see cref="BikeType"/> to a <see cref="BikeTypeDto"/>.
    /// </summary>
    public static BikeTypeDto MapToBikeTypeDto(BikeType bikeType)
    {
        return bikeType switch
        {
            BikeType.None => BikeTypeDto.None,
            BikeType.Regular => BikeTypeDto.Regular,
            BikeType.EBike => BikeTypeDto.EBike,
            _ => throw new ArgumentOutOfRangeException(nameof(bikeType), bikeType, "Invalid bike type value.")
        };
    }

    /// <summary>
    /// Maps a <see cref="RoomTypeDto"/> to a <see cref="RoomType"/>.
    /// </summary>
    public static RoomType MapToRoomType(RoomTypeDto roomTypeDto)
    {
        return roomTypeDto switch
        {
            RoomTypeDto.SingleRoom => RoomType.SingleRoom,
            RoomTypeDto.DoubleRoom => RoomType.DoubleRoom,
            _ => throw new ArgumentOutOfRangeException(nameof(roomTypeDto), roomTypeDto, "Invalid room type value.")
        };
    }

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