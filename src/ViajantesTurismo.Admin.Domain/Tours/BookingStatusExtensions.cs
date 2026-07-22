namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>Provides booking-status predicates for business workflows.</summary>
public static class BookingStatusExtensions
{
    /// <summary>Indicates whether a booking is eligible for customer-facing document generation.</summary>
    public static bool IsAccepted(this BookingStatus status) =>
        status is BookingStatus.Confirmed or BookingStatus.Completed;
}
