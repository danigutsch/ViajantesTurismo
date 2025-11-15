namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// DTO for retrieving booking information.
/// </summary>
public sealed record GetBookingDto
{
    /// <summary>The booking ID.</summary>
    public required Guid Id { get; init; }

    /// <summary>The tour ID.</summary>
    public required Guid TourId { get; init; }

    /// <summary>The tour identifier (e.g., "CUBA2024").</summary>
    public required string TourIdentifier { get; init; }

    /// <summary>The tour name.</summary>
    public required string TourName { get; init; }

    /// <summary>The customer ID.</summary>
    public required Guid CustomerId { get; init; }

    /// <summary>The customer's full name.</summary>
    public required string CustomerName { get; init; }

    /// <summary>The companion ID, if any.</summary>
    public Guid? CompanionId { get; init; }

    /// <summary>The companion's full name, if any.</summary>
    public string? CompanionName { get; init; }

    /// <summary>The date when the booking was made.</summary>
    public required DateTime BookingDate { get; init; }

    /// <summary>The booking status.</summary>
    public required BookingStatusDto Status { get; init; }

    /// <summary>The payment status.</summary>
    public required PaymentStatusDto PaymentStatus { get; init; }

    /// <summary>The total price of the booking.</summary>
    public required decimal TotalPrice { get; init; }

    /// <summary>The discount type applied to this booking.</summary>
    public required DiscountTypeDto DiscountType { get; init; }

    /// <summary>The discount amount applied to this booking.</summary>
    public required decimal DiscountAmount { get; init; }

    /// <summary>The reason for the discount, if any.</summary>
    public string? DiscountReason { get; init; }

    /// <summary>Notes about the booking.</summary>
    public string? Notes { get; init; }

    /// <summary>The payments recorded for this booking.</summary>
    public required IReadOnlyCollection<GetPaymentDto> Payments { get; init; }

    /// <summary>The total amount paid so far.</summary>
    public required decimal AmountPaid { get; init; }

    /// <summary>The remaining balance to be paid.</summary>
    public required decimal RemainingBalance { get; init; }
}
