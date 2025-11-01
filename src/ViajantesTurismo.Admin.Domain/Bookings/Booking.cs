using JetBrains.Annotations;
using ViajantesTurismo.Common;

namespace ViajantesTurismo.Admin.Domain.Bookings;

/// <summary>
/// Represents a booking made by a customer for a tour.
/// </summary>
public sealed class Booking : Entity<long>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Booking"/> class.
    /// </summary>
    /// <param name="tourId">The ID of the tour that was booked.</param>
    /// <param name="customerId">The ID of the customer who made the booking.</param>
    /// <param name="companionId">The ID of the companion, if any.</param>
    public Booking(int tourId, int customerId, int? companionId)
    {
        TourId = tourId;
        CustomerId = customerId;
        CompanionId = companionId;
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
    /// The currency of the booking.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Updates the payment status of the booking.
    /// </summary>
    /// <param name="paymentStatus">The new payment status.</param>
    internal void UpdatePaymentStatus(PaymentStatus paymentStatus)
    {
        PaymentStatus = paymentStatus;
    }

    /// <summary>
    /// Confirms the booking, setting its status to Confirmed.
    /// </summary>
    internal void Confirm()
    {
        if (Status == BookingStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot confirm a cancelled booking.");
        }

        Status = BookingStatus.Confirmed;
    }

    /// <summary>
    /// Cancels the booking, setting its status to Cancelled.
    /// </summary>
    internal void Cancel()
    {
        if (Status == BookingStatus.Completed)
        {
            throw new InvalidOperationException("Cannot cancel a completed booking.");
        }

        Status = BookingStatus.Cancelled;
    }

    /// <summary>
    /// Marks the booking as completed.
    /// </summary>
    internal void Complete()
    {
        if (Status == BookingStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot complete a cancelled booking.");
        }

        Status = BookingStatus.Completed;
    }

    /// <summary>
    /// Updates the total price of the booking.
    /// </summary>
    /// <param name="newPrice">The new total price.</param>
    internal void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0)
        {
            throw new ArgumentException("Price must be greater than zero.", nameof(newPrice));
        }

        TotalPrice = newPrice;
    }

    /// <summary>
    /// Updates the notes for the booking.
    /// </summary>
    /// <param name="notes">The updated notes.</param>
    internal void UpdateNotes(string? notes)
    {
        Notes = notes;
    }
}
