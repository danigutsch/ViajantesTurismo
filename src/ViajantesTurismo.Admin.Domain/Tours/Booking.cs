using JetBrains.Annotations;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.BuildingBlocks;
using ViajantesTurismo.Common.Results;
using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Represents a booking made by a customer for a tour.
/// </summary>
/// <remarks>
/// Part of the Tour aggregate. In production code, modify through <c>Tour</c> methods only (e.g., <c>Tour.ConfirmBooking()</c>, <c>Tour.UpdateBookingNotes()</c>).
/// </remarks>
public sealed class Booking : Entity<long>
{
    private readonly List<Payment> _payments = [];

    /// <summary>
    /// Initialises a new instance of the <see cref="Booking"/> class.
    /// </summary>
    /// <param name="tourId">The ID of the tour that was booked.</param>
    /// <param name="basePrice">The base price for a single room (not per person).</param>
    /// <param name="roomType">The room type for the booking.</param>
    /// <param name="roomAdditionalCost">The additional cost for a double room (0 for a single room).</param>
    /// <param name="principalCustomer">The principal customer's booking details.</param>
    /// <param name="companionCustomer">The companion customer's booking details, if any.</param>
    /// <param name="discount">The discount applied to this booking.</param>
    /// <param name="notes">Optional notes about the booking.</param>
    private Booking(
        int tourId,
        decimal basePrice,
        RoomType roomType,
        decimal roomAdditionalCost,
        BookingCustomer principalCustomer,
        BookingCustomer? companionCustomer,
        Discount discount,
        string? notes)
    {
        TourId = tourId;
        BasePrice = basePrice;
        RoomType = roomType;
        RoomAdditionalCost = roomAdditionalCost;
        PrincipalCustomer = principalCustomer;
        CompanionCustomer = companionCustomer;
        Discount = discount;
        Notes = notes;
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialisation.
    /// </summary>
    [UsedImplicitly]
#pragma warning disable CS8618
    private Booking()
#pragma warning restore CS8618
    {
    }

    /// <summary>
    /// The ID of the tour that was booked.
    /// </summary>
    public int TourId { get; private init; }

    /// <summary>
    /// The base price per person at the time of booking.
    /// </summary>
    public decimal BasePrice { get; private init; }

    /// <summary>
    /// The room type for the booking (shared by all participants).
    /// </summary>
    public RoomType RoomType { get; private set; }

    /// <summary>
    /// The additional cost for the room (e.g. single room supplement).
    /// </summary>
    public decimal RoomAdditionalCost { get; private set; }

    /// <summary>
    /// The principal customer's booking details including bike selection and price.
    /// </summary>
    public BookingCustomer PrincipalCustomer { get; private set; }

    /// <summary>
    /// The companion customer's booking details including bike selection and price, if any.
    /// </summary>
    public BookingCustomer? CompanionCustomer { get; private set; }

    /// <summary>
    /// The discount applied to this booking.
    /// </summary>
    public Discount Discount { get; private set; }

    /// <summary>
    /// The date when the booking was made.
    /// </summary>
    public DateTime BookingDate { get; private init; } = DateTime.UtcNow;

    /// <summary>
    /// The start date of the tour.
    /// </summary>
    public BookingStatus Status { get; private set; } = BookingStatus.Pending;

    /// <summary>
    /// The payment status of the booking.
    /// </summary>
    public PaymentStatus PaymentStatus { get; private set; } = PaymentStatus.Unpaid;

    /// <summary>
    /// The subtotal before discount (base price + room cost + bike prices).
    /// </summary>
    public decimal Subtotal => CalculateSubtotal(BasePrice, RoomAdditionalCost, PrincipalCustomer, CompanionCustomer);

    /// <summary>
    /// The total price of the booking (subtotal minus discount).
    /// </summary>
    public decimal TotalPrice => Subtotal - Discount.CalculateDiscountAmount(Subtotal);

    /// <summary>
    /// Additional notes for the booking.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// The payments recorded for this booking.
    /// </summary>
    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

    /// <summary>
    /// The total amount paid so far.
    /// </summary>
    public decimal AmountPaid => _payments.Sum(p => p.Amount);

    /// <summary>
    /// The remaining balance to be paid.
    /// </summary>
    public decimal RemainingBalance => TotalPrice - AmountPaid;

    private static decimal CalculateSubtotal(
        decimal basePrice,
        decimal roomAdditionalCost,
        BookingCustomer principalCustomer,
        BookingCustomer? companionCustomer)
    {
        var subtotal = basePrice + roomAdditionalCost + principalCustomer.BikePrice;

        if (companionCustomer is not null)
        {
            subtotal += companionCustomer.BikePrice;
        }

        return subtotal;
    }

    /// <summary>
    /// Creates a new booking with validation and sanitisation.
    /// </summary>
    /// <param name="tourId">The ID of the tour that was booked.</param>
    /// <param name="basePrice">The base price for a single room (not per person).</param>
    /// <param name="roomType">The room type for the booking.</param>
    /// <param name="roomAdditionalCost">The additional cost for a double room (0 for a single room).</param>
    /// <param name="principalCustomer">The principal customer's booking details.</param>
    /// <param name="companionCustomer">The companion customer's booking details, if any.</param>
    /// <param name="discount">The discount applied to this booking.</param>
    /// <param name="notes">Optional notes about the booking.</param>
    /// <returns>A Result containing the booking if successful, or validation errors.</returns>
    public static Result<Booking> Create(
        int tourId,
        decimal basePrice,
        RoomType roomType,
        decimal roomAdditionalCost,
        BookingCustomer principalCustomer,
        BookingCustomer? companionCustomer,
        Discount discount,
        string? notes)
    {
        ArgumentNullException.ThrowIfNull(principalCustomer);
        ArgumentNullException.ThrowIfNull(discount);

        basePrice = NumericSanitizer.SanitizePrice(basePrice);
        roomAdditionalCost = NumericSanitizer.SanitizePrice(roomAdditionalCost);
        notes = StringSanitizer.SanitizeNotes(notes);

        var errors = new ValidationErrors();

        if (!Enum.IsDefined(roomType))
        {
            errors.Add(BookingErrors.InvalidRoomType(roomType));
        }

        if (basePrice <= 0)
        {
            errors.Add(BookingErrors.ZeroOrNegativeBasePrice(basePrice));
        }

        if (basePrice > ContractConstants.MaxPrice)
        {
            errors.Add(BookingErrors.BasePriceExceedsMaximum(basePrice, ContractConstants.MaxPrice));
        }

        if (roomAdditionalCost < 0)
        {
            errors.Add(BookingErrors.NegativeRoomCost(roomAdditionalCost));
        }

        if (roomAdditionalCost > ContractConstants.MaxPrice)
        {
            errors.Add(BookingErrors.RoomCostExceedsMaximum(roomAdditionalCost, ContractConstants.MaxPrice));
        }

        if (notes?.Length > ContractConstants.MaxBookingNotesLength)
        {
            errors.Add(BookingErrors.InvalidNotesLength(ContractConstants.MaxBookingNotesLength, notes.Length));
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<Booking>();
        }

        var subtotal = CalculateSubtotal(basePrice, roomAdditionalCost, principalCustomer, companionCustomer);

        if (discount.Type == DiscountType.Absolute && discount.Amount > subtotal)
        {
            return DiscountErrors.AbsoluteDiscountExceedsSubtotal(discount.Amount, subtotal).ConvertError<Booking>();
        }

        var finalPrice = subtotal - discount.CalculateDiscountAmount(subtotal);
        if (finalPrice <= 0)
        {
            return DiscountErrors.FinalPriceNotPositive(finalPrice).ConvertError<Booking>();
        }

        return new Booking(tourId, basePrice, roomType, roomAdditionalCost, principalCustomer, companionCustomer, discount, notes);
    }

    /// <summary>
    /// Updates the payment status of the booking.
    /// </summary>
    /// <param name="paymentStatus">The new payment status.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result UpdatePaymentStatus(PaymentStatus paymentStatus)
    {
        if (!Enum.IsDefined(paymentStatus))
        {
            return BookingErrors.InvalidPaymentStatus(paymentStatus);
        }

        PaymentStatus = paymentStatus;
        return Result.Ok();
    }

    /// <summary>
    /// Confirms the booking, setting its status to Confirmed. Is idempotent.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    public Result Confirm()
    {
        return Status switch
        {
            BookingStatus.Confirmed => Result.Ok(),
            BookingStatus.Cancelled or BookingStatus.Completed => BookingErrors.InvalidStatusTransition(Status, BookingStatus.Confirmed),
            BookingStatus.Pending => ConfirmInternal(),
            _ => throw new ArgumentOutOfRangeException(nameof(Status), Status, $"Invalid booking status: {Status}")
        };

        Result ConfirmInternal()
        {
            Status = BookingStatus.Confirmed;
            return Result.Ok();
        }
    }

    /// <summary>
    /// Cancels the booking, setting its status to Cancelled. Is idempotent.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    public Result Cancel()
    {
        return Status switch
        {
            BookingStatus.Cancelled => Result.Ok(),
            BookingStatus.Completed => BookingErrors.InvalidStatusTransition(Status, BookingStatus.Cancelled),
            BookingStatus.Pending or BookingStatus.Confirmed => CancelInternal(),
            _ => throw new ArgumentOutOfRangeException(nameof(Status), Status, $"Invalid booking status: {Status}")
        };

        Result CancelInternal()
        {
            Status = BookingStatus.Cancelled;
            return Result.Ok();
        }
    }

    /// <summary>
    /// Marks the booking as completed. Is idempotent.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    public Result Complete()
    {
        return Status switch
        {
            BookingStatus.Completed => Result.Ok(),
            BookingStatus.Cancelled => BookingErrors.InvalidStatusTransition(Status, BookingStatus.Completed),
            BookingStatus.Pending or BookingStatus.Confirmed => CompleteInternal(),
            _ => throw new ArgumentOutOfRangeException(nameof(Status), Status, $"Invalid booking status: {Status}")
        };

        Result CompleteInternal()
        {
            Status = BookingStatus.Completed;
            return Result.Ok();
        }
    }


    /// <summary>
    /// Updates the notes for the booking.
    /// </summary>
    /// <param name="notes">The updated notes.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result UpdateNotes(string? notes)
    {
        notes = StringSanitizer.SanitizeNotes(notes);

        if (notes?.Length > ContractConstants.MaxBookingNotesLength)
        {
            return BookingErrors.InvalidNotesLength(ContractConstants.MaxBookingNotesLength, notes.Length);
        }

        Notes = notes;
        return Result.Ok();
    }

    /// <summary>
    /// Updates the discount for the booking.
    /// </summary>
    /// <param name="discount">The new discount.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result UpdateDiscount(Discount discount)
    {
        ArgumentNullException.ThrowIfNull(discount);

        if (Status is BookingStatus.Cancelled or BookingStatus.Completed)
        {
            return BookingErrors.CannotModifyCancelledOrCompletedBooking(Id, Status);
        }

        if (discount.Type == DiscountType.Absolute && discount.Amount > Subtotal)
        {
            return DiscountErrors.AbsoluteDiscountExceedsSubtotal(discount.Amount, Subtotal);
        }

        var finalPrice = Subtotal - discount.CalculateDiscountAmount(Subtotal);
        if (finalPrice <= 0)
        {
            return DiscountErrors.FinalPriceNotPositive(finalPrice);
        }

        Discount = discount;
        return Result.Ok();
    }

    /// <summary>
    /// Updates the booking details (room type, bikes, companion) after creation.
    /// </summary>
    /// <param name="roomType">The new room type.</param>
    /// <param name="roomAdditionalCost">The new room additional cost.</param>
    /// <param name="principalCustomer">The updated principal customer details.</param>
    /// <param name="companionCustomer">The updated companion customer details (null to remove).</param>
    /// <param name="discount">The discount applied to this booking.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result UpdateDetails(
        RoomType roomType,
        decimal roomAdditionalCost,
        BookingCustomer principalCustomer,
        BookingCustomer? companionCustomer,
        Discount discount)
    {
        ArgumentNullException.ThrowIfNull(principalCustomer);
        ArgumentNullException.ThrowIfNull(discount);

        if (Status is BookingStatus.Cancelled or BookingStatus.Completed)
        {
            return BookingErrors.CannotModifyCancelledOrCompletedBooking(Id, Status);
        }

        roomAdditionalCost = NumericSanitizer.SanitizePrice(roomAdditionalCost);

        var errors = new ValidationErrors();

        if (!Enum.IsDefined(roomType))
        {
            errors.Add(BookingErrors.InvalidRoomType(roomType));
        }

        if (roomAdditionalCost < 0)
        {
            errors.Add(BookingErrors.NegativeRoomCost(roomAdditionalCost));
        }

        if (roomAdditionalCost > ContractConstants.MaxPrice)
        {
            errors.Add(BookingErrors.RoomCostExceedsMaximum(roomAdditionalCost, ContractConstants.MaxPrice));
        }

        if (errors.HasErrors)
        {
            return errors.ToResult();
        }

        var subtotal = CalculateSubtotal(BasePrice, roomAdditionalCost, principalCustomer, companionCustomer);

        if (discount.Type == DiscountType.Absolute && discount.Amount > subtotal)
        {
            return DiscountErrors.AbsoluteDiscountExceedsSubtotal(discount.Amount, subtotal);
        }

        var finalPrice = subtotal - discount.CalculateDiscountAmount(subtotal);
        if (finalPrice <= 0)
        {
            return DiscountErrors.FinalPriceNotPositive(finalPrice);
        }

        RoomType = roomType;
        RoomAdditionalCost = roomAdditionalCost;
        PrincipalCustomer = principalCustomer;
        CompanionCustomer = companionCustomer;

        return Result.Ok();
    }

    /// <summary>
    /// Records a payment for this booking with validation.
    /// </summary>
    /// <param name="amount">The payment amount.</param>
    /// <param name="paymentDate">The date the payment was made.</param>
    /// <param name="method">The payment method used.</param>
    /// <param name="timeProvider">The time provider for getting current time.</param>
    /// <param name="referenceNumber">Optional reference number for the payment.</param>
    /// <param name="notes">Optional notes about the payment.</param>
    /// <returns>A result containing the recorded Payment if successful, or validation errors.</returns>
    public Result<Payment> RecordPayment(
        decimal amount,
        DateTime paymentDate,
        PaymentMethod method,
        TimeProvider timeProvider,
        string? referenceNumber = null,
        string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (amount > RemainingBalance)
        {
            return PaymentErrors.ExceedsRemainingBalance(amount, RemainingBalance).ConvertError<Payment>();
        }

        var paymentResult = Payment.Create(Id, amount, paymentDate, method, timeProvider, referenceNumber, notes);
        if (paymentResult.IsFailure)
        {
            return paymentResult;
        }

        var payment = paymentResult.Value;
        _payments.Add(payment);

        UpdatePaymentStatusFromPayments();

        return payment;
    }

    /// <summary>
    /// Updates the payment status based on the total amount paid.
    /// </summary>
    private void UpdatePaymentStatusFromPayments()
    {
        var amountPaid = AmountPaid;
        var totalPrice = TotalPrice;

        PaymentStatus = amountPaid switch
        {
            0 => PaymentStatus.Unpaid,
            _ when amountPaid >= totalPrice => PaymentStatus.Paid,
            _ => PaymentStatus.PartiallyPaid
        };
    }
}
