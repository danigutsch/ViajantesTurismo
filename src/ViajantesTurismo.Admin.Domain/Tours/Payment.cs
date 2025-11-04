using JetBrains.Annotations;
using ViajantesTurismo.Common.BuildingBlocks;
using ViajantesTurismo.Common.Results;
using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Represents a payment recorded for a booking.
/// Payments are immutable once created.
/// </summary>
public sealed class Payment : Entity<long>
{
    /// <summary>
    /// Initialises a new instance of the <see cref="Payment"/> class.
    /// </summary>
    /// <param name="bookingId">The ID of the booking this payment is for.</param>
    /// <param name="amount">The payment amount.</param>
    /// <param name="paymentDate">The date the payment was made.</param>
    /// <param name="method">The payment method used.</param>
    /// <param name="referenceNumber">Optional reference number for the payment.</param>
    /// <param name="notes">Optional notes about the payment.</param>
    /// <param name="recordedAt">The timestamp when this payment was recorded (UTC).</param>
    private Payment(
        long bookingId,
        decimal amount,
        DateTime paymentDate,
        PaymentMethod method,
        string? referenceNumber,
        string? notes,
        DateTime recordedAt)
    {
        BookingId = bookingId;
        Amount = amount;
        PaymentDate = paymentDate;
        Method = method;
        ReferenceNumber = referenceNumber;
        Notes = notes;
        RecordedAt = recordedAt;
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialisation.
    /// </summary>
    [UsedImplicitly]
    private Payment()
    {
    }

    /// <summary>
    /// The ID of the booking this payment is for.
    /// </summary>
    public long BookingId { get; private init; }

    /// <summary>
    /// The payment amount.
    /// </summary>
    public decimal Amount { get; private init; }

    /// <summary>
    /// The date the payment was made.
    /// </summary>
    public DateTime PaymentDate { get; private init; }

    /// <summary>
    /// The payment method used.
    /// </summary>
    public PaymentMethod Method { get; private init; }

    /// <summary>
    /// Optional reference number for the payment (e.g. transaction ID, check number).
    /// </summary>
    public string? ReferenceNumber { get; private init; }

    /// <summary>
    /// Optional notes about the payment.
    /// </summary>
    public string? Notes { get; private init; }

    /// <summary>
    /// The timestamp when this payment was recorded (UTC).
    /// </summary>
    public DateTime RecordedAt { get; private init; }

    /// <summary>
    /// Creates a new instance of <see cref="Payment"/> with validation.
    /// </summary>
    /// <param name="bookingId">The ID of the booking this payment is for.</param>
    /// <param name="amount">The payment amount.</param>
    /// <param name="paymentDate">The date the payment was made.</param>
    /// <param name="method">The payment method used.</param>
    /// <param name="timeProvider">The time provider for getting current time.</param>
    /// <param name="referenceNumber">Optional reference number for the payment.</param>
    /// <param name="notes">Optional notes about the payment.</param>
    /// <returns>A Result containing the Payment if successful, or validation errors.</returns>
    public static Result<Payment> Create(
        long bookingId,
        decimal amount,
        DateTime paymentDate,
        PaymentMethod method,
        TimeProvider timeProvider,
        string? referenceNumber,
        string? notes)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        amount = NumericSanitizer.SanitizePrice(amount);
        referenceNumber = StringSanitizer.Sanitize(referenceNumber);
        notes = StringSanitizer.SanitizeNotes(notes);

        var errors = new ValidationErrors();

        if (amount <= 0)
        {
            errors.Add(PaymentErrors.InvalidAmount(amount));
        }

        if (!Enum.IsDefined(method))
        {
            errors.Add(PaymentErrors.InvalidPaymentMethod(method));
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (paymentDate > now)
        {
            errors.Add(PaymentErrors.FuturePaymentDate(paymentDate));
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<Payment>();
        }

        return new Payment(bookingId, amount, paymentDate, method, referenceNumber, notes, now);
    }
}
