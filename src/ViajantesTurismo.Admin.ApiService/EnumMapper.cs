using System.ComponentModel;
using ViajantesTurismo.Admin.Domain.Bookings;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.AdminApi.Contracts;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.ApiService;

/// <summary>
/// Maps DTO enums to domain enums.
/// </summary>
internal static class EnumMapper
{
    /// <summary>
    /// Maps a <see cref="CurrencyDto"/> to a <see cref="Currency"/>.
    /// </summary>
    /// <param name="currencyDto">The currency DTO.</param>
    /// <returns>The corresponding domain currency.</returns>
    /// <exception cref="InvalidEnumArgumentException">Thrown when the currency DTO value is not recognized.</exception>
    public static Currency MapToCurrency(CurrencyDto currencyDto)
    {
        return currencyDto switch
        {
            CurrencyDto.Real => Currency.Real,
            CurrencyDto.Euro => Currency.Euro,
            CurrencyDto.UsDollar => Currency.UsDollar,
            _ => throw new InvalidEnumArgumentException(nameof(currencyDto), (int)currencyDto, typeof(CurrencyDto))
        };
    }

    /// <summary>
    /// Maps a <see cref="BikeTypeDto"/> to a <see cref="BikeType"/>.
    /// </summary>
    /// <param name="bikeTypeDto">The bike type DTO.</param>
    /// <returns>The corresponding domain bike type.</returns>
    /// <exception cref="InvalidEnumArgumentException">Thrown when the bike type DTO value is not recognized.</exception>
    public static BikeType MapToBikeType(BikeTypeDto bikeTypeDto)
    {
        return bikeTypeDto switch
        {
            BikeTypeDto.None => BikeType.None,
            BikeTypeDto.Regular => BikeType.Regular,
            BikeTypeDto.EBike => BikeType.EBike,
            _ => throw new InvalidEnumArgumentException(nameof(bikeTypeDto), (int)bikeTypeDto, typeof(BikeTypeDto))
        };
    }

    /// <summary>
    /// Maps a <see cref="RoomTypeDto"/> to a <see cref="RoomType"/>.
    /// </summary>
    /// <param name="roomTypeDto">The room type DTO.</param>
    /// <returns>The corresponding domain room type.</returns>
    /// <exception cref="InvalidEnumArgumentException">Thrown when the room type DTO value is not recognized.</exception>
    public static RoomType MapToRoomType(RoomTypeDto roomTypeDto)
    {
        return roomTypeDto switch
        {
            RoomTypeDto.SingleRoom => RoomType.SingleRoom,
            RoomTypeDto.DoubleRoom => RoomType.DoubleRoom,
            _ => throw new InvalidEnumArgumentException(nameof(roomTypeDto), (int)roomTypeDto, typeof(RoomTypeDto))
        };
    }

    /// <summary>
    /// Maps a <see cref="BedTypeDto"/> to a <see cref="BedType"/>.
    /// </summary>
    /// <param name="bedTypeDto">The bed type DTO.</param>
    /// <returns>The corresponding domain bed type.</returns>
    /// <exception cref="InvalidEnumArgumentException">Thrown when the bed type DTO value is not recognized.</exception>
    public static BedType MapToBedType(BedTypeDto bedTypeDto)
    {
        return bedTypeDto switch
        {
            BedTypeDto.SingleBed => BedType.SingleBed,
            BedTypeDto.DoubleBed => BedType.DoubleBed,
            _ => throw new InvalidEnumArgumentException(nameof(bedTypeDto), (int)bedTypeDto, typeof(BedTypeDto))
        };
    }

    /// <summary>
    /// Maps a <see cref="BookingStatusDto"/> to a <see cref="BookingStatus"/>.
    /// </summary>
    /// <param name="bookingStatusDto">The booking status DTO.</param>
    /// <returns>The corresponding domain booking status.</returns>
    /// <exception cref="InvalidEnumArgumentException">Thrown when the booking status DTO value is not recognized.</exception>
    public static BookingStatus MapToBookingStatus(BookingStatusDto bookingStatusDto)
    {
        return bookingStatusDto switch
        {
            BookingStatusDto.Pending => BookingStatus.Pending,
            BookingStatusDto.Confirmed => BookingStatus.Confirmed,
            BookingStatusDto.Cancelled => BookingStatus.Cancelled,
            BookingStatusDto.Completed => BookingStatus.Completed,
            _ => throw new InvalidEnumArgumentException(nameof(bookingStatusDto), (int)bookingStatusDto, typeof(BookingStatusDto))
        };
    }

    /// <summary>
    /// Maps a <see cref="PaymentStatusDto"/> to a <see cref="PaymentStatus"/>.
    /// </summary>
    /// <param name="paymentStatusDto">The payment status DTO.</param>
    /// <returns>The corresponding domain payment status.</returns>
    /// <exception cref="InvalidEnumArgumentException">Thrown when the payment status DTO value is not recognized.</exception>
    public static PaymentStatus MapToPaymentStatus(PaymentStatusDto paymentStatusDto)
    {
        return paymentStatusDto switch
        {
            PaymentStatusDto.Unpaid => PaymentStatus.Unpaid,
            PaymentStatusDto.PartiallyPaid => PaymentStatus.PartiallyPaid,
            PaymentStatusDto.Paid => PaymentStatus.Paid,
            PaymentStatusDto.Refunded => PaymentStatus.Refunded,
            _ => throw new InvalidEnumArgumentException(nameof(paymentStatusDto), (int)paymentStatusDto, typeof(PaymentStatusDto))
        };
    }
}
