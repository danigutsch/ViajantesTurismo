using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Admin.Application.Mappings;

/// <summary>
/// Maps Booking-related DTOs to domain objects.
/// </summary>
public static class BookingMapper
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

    /// <summary>
    /// Maps a <see cref="DiscountTypeDto"/> to a <see cref="DiscountType"/>.
    /// </summary>
    public static DiscountType MapToDiscountType(DiscountTypeDto discountTypeDto)
    {
        return discountTypeDto switch
        {
            DiscountTypeDto.None => DiscountType.None,
            DiscountTypeDto.Percentage => DiscountType.Percentage,
            DiscountTypeDto.Absolute => DiscountType.Absolute,
            _ => throw new ArgumentOutOfRangeException(nameof(discountTypeDto), discountTypeDto, "Invalid discount type value.")
        };
    }

    /// <summary>
    /// Maps a <see cref="DiscountType"/> to a <see cref="DiscountTypeDto"/>.
    /// </summary>
    public static DiscountTypeDto MapToDiscountTypeDto(DiscountType discountType)
    {
        return discountType switch
        {
            DiscountType.None => DiscountTypeDto.None,
            DiscountType.Percentage => DiscountTypeDto.Percentage,
            DiscountType.Absolute => DiscountTypeDto.Absolute,
            _ => throw new ArgumentOutOfRangeException(nameof(discountType), discountType, "Invalid discount type value.")
        };
    }

    /// <summary>
    /// Maps a <see cref="PaymentMethodDto"/> to a <see cref="PaymentMethod"/>.
    /// </summary>
    public static PaymentMethod MapToPaymentMethod(PaymentMethodDto paymentMethodDto)
    {
        return paymentMethodDto switch
        {
            PaymentMethodDto.CreditCard => PaymentMethod.CreditCard,
            PaymentMethodDto.BankTransfer => PaymentMethod.BankTransfer,
            PaymentMethodDto.Cash => PaymentMethod.Cash,
            PaymentMethodDto.Check => PaymentMethod.Check,
            PaymentMethodDto.PayPal => PaymentMethod.PayPal,
            PaymentMethodDto.Other => PaymentMethod.Other,
            _ => throw new ArgumentOutOfRangeException(nameof(paymentMethodDto), paymentMethodDto, "Invalid payment method value.")
        };
    }

    /// <summary>
    /// Maps a <see cref="PaymentMethod"/> to a <see cref="PaymentMethodDto"/>.
    /// </summary>
    public static PaymentMethodDto MapToPaymentMethodDto(PaymentMethod paymentMethod)
    {
        return paymentMethod switch
        {
            PaymentMethod.CreditCard => PaymentMethodDto.CreditCard,
            PaymentMethod.BankTransfer => PaymentMethodDto.BankTransfer,
            PaymentMethod.Cash => PaymentMethodDto.Cash,
            PaymentMethod.Check => PaymentMethodDto.Check,
            PaymentMethod.PayPal => PaymentMethodDto.PayPal,
            PaymentMethod.Other => PaymentMethodDto.Other,
            _ => throw new ArgumentOutOfRangeException(nameof(paymentMethod), paymentMethod, "Invalid payment method value.")
        };
    }

    /// <summary>
    /// Maps a <see cref="Payment"/> to a <see cref="GetPaymentDto"/>.
    /// </summary>
    public static GetPaymentDto MapToPaymentDto(Payment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);

        return new GetPaymentDto
        {
            Id = payment.Id,
            BookingId = payment.BookingId,
            Amount = payment.Amount,
            PaymentDate = payment.PaymentDate,
            Method = MapToPaymentMethodDto(payment.Method),
            ReferenceNumber = payment.ReferenceNumber,
            Notes = payment.Notes,
            RecordedAt = payment.RecordedAt
        };
    }
}
