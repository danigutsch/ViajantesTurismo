using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.AdminApi.Contracts;
using ViajantesTurismo.Common.BuildingBlocks;
using ViajantesTurismo.Common.Results;
using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Represents a booking made by a customer for a tour.
/// </summary>
/// <remarks>
/// Part of the Tour aggregate. In production code, modify through <c>Tour</c> methods only (e.g., <c>Tour.ConfirmBooking()</c>, <c>Tour.UpdateBookingPrice()</c>).
/// </remarks>
public sealed class Booking : Entity<long>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Booking"/> class.
    /// </summary>
    /// <param name="tourId">The ID of the tour that was booked.</param>
    /// <param name="basePrice">The base price for a single room (not per person).</param>
    /// <param name="roomType">The room type for the booking.</param>
    /// <param name="roomAdditionalCost">The additional cost for double room (0 for single room).</param>
    /// <param name="principalCustomer">The principal customer's booking details.</param>
    /// <param name="companionCustomer">The companion customer's booking details, if any.</param>
    /// <param name="notes">Optional notes about the booking.</param>
    private Booking(
        int tourId,
        decimal basePrice,
        RoomType roomType,
        decimal roomAdditionalCost,
        BookingCustomer principalCustomer,
        BookingCustomer? companionCustomer,
        string? notes)
    {
        TourId = tourId;
        BasePrice = basePrice;
        RoomType = roomType;
        RoomAdditionalCost = roomAdditionalCost;
        PrincipalCustomer = principalCustomer;
        CompanionCustomer = companionCustomer;
        Notes = notes;
        TotalPrice = CalculateTotalPrice(basePrice, roomAdditionalCost, principalCustomer, companionCustomer);
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
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
    public RoomType RoomType { get; private init; }

    /// <summary>
    /// The additional cost for the room (e.g., single room supplement).
    /// </summary>
    public decimal RoomAdditionalCost { get; private init; }

    /// <summary>
    /// The principal customer's booking details including bike selection and price.
    /// </summary>
    public BookingCustomer PrincipalCustomer { get; private init; }

    /// <summary>
    /// The companion customer's booking details including bike selection and price, if any.
    /// </summary>
    public BookingCustomer? CompanionCustomer { get; private init; }

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
    /// The total price of the booking.
    /// </summary>
    public decimal TotalPrice { get; private set; }

    /// <summary>
    /// Additional notes for the booking.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Calculates the total price for a booking.
    /// Base price is for a single room (not per person).
    /// RoomAdditionalCost is added for double rooms.
    /// Bike prices are added for principal and companion customers.
    /// </summary>
    private static decimal CalculateTotalPrice(
        decimal basePrice,
        decimal roomAdditionalCost,
        BookingCustomer principalCustomer,
        BookingCustomer? companionCustomer)
    {
        var totalPrice = basePrice + roomAdditionalCost + principalCustomer.BikePrice;

        if (companionCustomer is not null)
        {
            totalPrice += companionCustomer.BikePrice;
        }

        return totalPrice;
    }

    /// <summary>
    /// Creates a new booking with validation and sanitization.
    /// </summary>
    /// <param name="tourId">The ID of the tour that was booked.</param>
    /// <param name="basePrice">The base price for a single room (not per person).</param>
    /// <param name="roomType">The room type for the booking.</param>
    /// <param name="roomAdditionalCost">The additional cost for double room (0 for single room).</param>
    /// <param name="principalCustomer">The principal customer's booking details.</param>
    /// <param name="companionCustomer">The companion customer's booking details, if any.</param>
    /// <param name="notes">Optional notes about the booking.</param>
    /// <returns>A Result containing the booking if successful, or validation errors.</returns>
    public static Result<Booking> Create(
        int tourId,
        decimal basePrice,
        RoomType roomType,
        decimal roomAdditionalCost,
        BookingCustomer principalCustomer,
        BookingCustomer? companionCustomer,
        string? notes)
    {
        basePrice = NumericSanitizer.SanitizePrice(basePrice);
        roomAdditionalCost = NumericSanitizer.SanitizePrice(roomAdditionalCost);
        notes = StringSanitizer.SanitizeNotes(notes);

        var errors = new ValidationErrors();

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

        return new Booking(tourId, basePrice, roomType, roomAdditionalCost, principalCustomer, companionCustomer, notes);
    }

    /// <summary>
    /// Updates the payment status of the booking.
    /// </summary>
    /// <param name="paymentStatus">The new payment status.</param>
    public void UpdatePaymentStatus(PaymentStatus paymentStatus)
    {
        PaymentStatus = paymentStatus;
    }

    /// <summary>
    /// Confirms the booking, setting its status to Confirmed. Is idempotent.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    public Result Confirm()
    {
        switch (Status)
        {
            case BookingStatus.Confirmed:
                return Result.Ok();
            case BookingStatus.Cancelled or BookingStatus.Completed:
                return BookingErrors.InvalidStatusTransition(Status, BookingStatus.Confirmed);
            case BookingStatus.Pending:
            default:
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
        switch (Status)
        {
            case BookingStatus.Cancelled:
                return Result.Ok();
            case BookingStatus.Completed:
                return BookingErrors.InvalidStatusTransition(Status, BookingStatus.Cancelled);
            case BookingStatus.Pending or BookingStatus.Confirmed:
            default:
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
        switch (Status)
        {
            case BookingStatus.Completed:
                return Result.Ok();
            case BookingStatus.Cancelled:
                return BookingErrors.InvalidStatusTransition(Status, BookingStatus.Completed);
            case BookingStatus.Pending or BookingStatus.Confirmed:
            default:
                Status = BookingStatus.Completed;
                return Result.Ok();
        }
    }

    /// <summary>
    /// Updates the total price of the booking.
    /// </summary>
    /// <param name="newPrice">The new total price.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result UpdatePrice(decimal newPrice)
    {
        newPrice = NumericSanitizer.SanitizePrice(newPrice);

        if (newPrice <= 0)
        {
            return BookingErrors.ZeroOrNegativeTotalPrice(newPrice);
        }

        if (newPrice > ContractConstants.MaxPrice)
        {
            return BookingErrors.TotalPriceExceedsMaximum(newPrice, ContractConstants.MaxPrice);
        }

        TotalPrice = newPrice;
        return Result.Ok();
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
}
