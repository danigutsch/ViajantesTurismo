namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>
/// DTO for retrieving booking information.
/// </summary>
public sealed record GetBookingDto
{
    /// <summary>Gets the protected financial concurrency version once the EUR workflow has been adopted.</summary>
    public long? FinancialVersion
    {
        get; init;
    }
    /// <summary>Gets the approved EUR obligation independently of the booking-price snapshot.</summary>
    public decimal? ApprovedObligationEuro
    {
        get; init;
    }

    /// <summary>The booking ID.</summary>
    public required Guid Id
    {
        get; init;
    }

    /// <summary>The tour ID.</summary>
    public required Guid TourId
    {
        get; init;
    }

    /// <summary>The tour identifier (e.g., "CUBA2024").</summary>
    public required string TourIdentifier
    {
        get; init;
    }

    /// <summary>The tour name.</summary>
    public required string TourName
    {
        get; init;
    }

    /// <summary>The customer ID.</summary>
    public required Guid CustomerId
    {
        get; init;
    }

    /// <summary>The customer's full name.</summary>
    public required string CustomerName
    {
        get; init;
    }

    /// <summary>The companion ID, if any.</summary>
    public Guid? CompanionId
    {
        get; init;
    }

    /// <summary>The companion's full name, if any.</summary>
    public string? CompanionName
    {
        get; init;
    }

    /// <summary>The room type selected for the booking.</summary>
    public required RoomTypeDto RoomType
    {
        get; init;
    }

    /// <summary>The bike type selected for the principal customer.</summary>
    public required BikeTypeDto PrincipalBikeType
    {
        get; init;
    }

    /// <summary>The bike type selected for the companion, if any.</summary>
    public BikeTypeDto? CompanionBikeType
    {
        get; init;
    }

    /// <summary>The date when the booking was made.</summary>
    public required DateTime BookingDate
    {
        get; init;
    }

    /// <summary>The booking status.</summary>
    public required BookingStatusDto Status
    {
        get; init;
    }

    /// <summary>The payment status.</summary>
    public required PaymentStatusDto PaymentStatus
    {
        get; init;
    }

    /// <summary>The total price of the booking.</summary>
    public required decimal TotalPrice
    {
        get; init;
    }

    /// <summary>The discount type applied to this booking.</summary>
    public required DiscountTypeDto DiscountType
    {
        get; init;
    }

    /// <summary>The discount amount applied to this booking.</summary>
    public required decimal DiscountAmount
    {
        get; init;
    }

    /// <summary>The reason for the discount, if any.</summary>
    public string? DiscountReason
    {
        get; init;
    }

    /// <summary>The currency of the booking price; approved payment obligations are in EUR.</summary>
    public required CurrencyDto Currency
    {
        get; init;
    }

    /// <summary>Notes about the booking.</summary>
    public string? Notes
    {
        get; init;
    }

    /// <summary>The total amount paid so far.</summary>
    public required decimal AmountPaid
    {
        get; init;
    }

    /// <summary>The remaining balance to be paid.</summary>
    public required decimal RemainingBalance
    {
        get; init;
    }
}
