namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>The supported sort keys for the booking list.</summary>
public enum BookingPageSort
{
    /// <summary>Cancelled bookings first, newest bookings first within each group.</summary>
    Default,

    /// <summary>The booking date.</summary>
    BookingDate,

    /// <summary>The tour name.</summary>
    TourName,

    /// <summary>The principal customer's full name.</summary>
    CustomerName,

    /// <summary>The booking price after discount.</summary>
    TotalPrice,
}
