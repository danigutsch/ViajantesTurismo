using JetBrains.Annotations;
using ViajantesTurismo.AdminApi.Contracts;
using ViajantesTurismo.Common;
using ViajantesTurismo.Common.BuildingBlocks;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Domain.Bookings;

/// <summary>
/// Represents a booking made by a customer for a tour.
/// </summary>
/// <remarks>
/// Part of the Tour aggregate. Modify through <c>Tour</c> methods only (e.g., <c>Tour.ConfirmBooking()</c>, <c>Tour.UpdateBookingPrice()</c>).
/// Methods are <c>internal</c> to enforce aggregate boundary.
/// </remarks>
public sealed class Booking : Entity<long>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Booking"/> class.
    /// </summary>
    /// <param name="tourId">The ID of the tour that was booked.</param>
    /// <param name="customerId">The ID of the customer who made the booking.</param>
    /// <param name="companionId">The ID of the companion, if any.</param>
    /// <param name="totalPrice">The total price of the booking.</param>
    /// <param name="notes">Optional notes about the booking.</param>
    private Booking(int tourId, int customerId, int? companionId, decimal totalPrice, string? notes)
    {
        TourId = tourId;
        CustomerId = customerId;
        CompanionId = companionId;
        TotalPrice = totalPrice;
        Notes = notes;
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
    [UsedImplicitly]
    private Booking()
    {
    }

    /// <summary>
    /// The ID of the tour that was booked.
    /// </summary>
    public int TourId { get; private init; }

    /// <summary>
    /// The ID of the customer who made the booking.
    /// </summary>
    public int CustomerId { get; private init; }

    /// <summary>
    /// The ID of the companion, if any.
    /// </summary>
    public int? CompanionId { get; private init; }

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
    /// Creates a new booking with validation and sanitization.
    /// </summary>
    /// <param name="tourId">The ID of the tour that was booked.</param>
    /// <param name="customerId">The ID of the customer who made the booking.</param>
    /// <param name="companionId">The ID of the companion, if any.</param>
    /// <param name="totalPrice">The total price of the booking.</param>
    /// <param name="notes">Optional notes about the booking.</param>
    /// <returns>A Result containing the booking if successful, or validation errors.</returns>
    internal static Result<Booking> Create(int tourId, int customerId, int? companionId, decimal totalPrice, string? notes)
    {
        totalPrice = NumericSanitizer.SanitizePrice(totalPrice);
        notes = StringSanitizer.SanitizeNotes(notes);

        var errors = new ValidationErrors();

        if (totalPrice <= ContractConstants.MinPrice)
        {
            errors.Add(BookingErrors.ZeroOrNegativePrice(totalPrice));
        }

        if (totalPrice > ContractConstants.MaxPrice)
        {
            errors.Add(BookingErrors.PriceExceedsMaximum(totalPrice, ContractConstants.MaxPrice));
        }

        if (notes?.Length > ContractConstants.MaxBookingNotesLength)
        {
            errors.Add(BookingErrors.InvalidNotesLength(ContractConstants.MaxBookingNotesLength, notes.Length));
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<Booking>();
        }

        return new Booking(tourId, customerId, companionId, totalPrice, notes);
    }

    /// <summary>
    /// Updates the payment status of the booking.
    /// </summary>
    /// <param name="paymentStatus">The new payment status.</param>
    internal void UpdatePaymentStatus(PaymentStatus paymentStatus)
    {
        PaymentStatus = paymentStatus;
    }

    /// <summary>
    /// Confirms the booking, setting its status to Confirmed. Is idempotent.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    internal Result Confirm()
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
    internal Result Cancel()
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
    internal Result Complete()
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
    internal Result UpdatePrice(decimal newPrice)
    {
        newPrice = NumericSanitizer.SanitizePrice(newPrice);

        if (newPrice <= ContractConstants.MinPrice)
        {
            return BookingErrors.ZeroOrNegativePrice(newPrice);
        }

        if (newPrice > ContractConstants.MaxPrice)
        {
            return BookingErrors.PriceExceedsMaximum(newPrice, ContractConstants.MaxPrice);
        }

        TotalPrice = newPrice;
        return Result.Ok();
    }

    /// <summary>
    /// Updates the notes for the booking.
    /// </summary>
    /// <param name="notes">The updated notes.</param>
    /// <returns>A result indicating success or failure.</returns>
    internal Result UpdateNotes(string? notes)
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
