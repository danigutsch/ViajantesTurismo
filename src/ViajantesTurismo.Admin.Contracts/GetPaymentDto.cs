namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// DTO for retrieving payment information.
/// </summary>
public sealed class GetPaymentDto
{
    /// <summary>The payment ID.</summary>
    public required long Id { get; init; }

    /// <summary>The booking ID this payment is for.</summary>
    public required long BookingId { get; init; }

    /// <summary>The payment amount.</summary>
    public required decimal Amount { get; init; }

    /// <summary>The date the payment was made.</summary>
    public required DateTime PaymentDate { get; init; }

    /// <summary>The payment method used.</summary>
    public required PaymentMethodDto Method { get; init; }

    /// <summary>Optional reference number for the payment.</summary>
    public string? ReferenceNumber { get; init; }

    /// <summary>Optional notes about the payment.</summary>
    public string? Notes { get; init; }

    /// <summary>The timestamp when this payment was recorded (UTC).</summary>
    public required DateTime RecordedAt { get; init; }
}
