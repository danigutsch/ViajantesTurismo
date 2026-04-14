using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Defines the input required to add a booking through the tour aggregate.
/// </summary>
/// <param name="Travelers">The travelers participating in the booking.</param>
/// <param name="RoomType">The requested room type.</param>
/// <param name="Discount">The requested discount.</param>
/// <param name="Notes">Optional booking notes.</param>
public sealed record TourBookingRequest(
    BookingTravelers Travelers,
    RoomType RoomType,
    BookingDiscountDefinition Discount,
    string? Notes = null)
{
    /// <summary>
    /// Creates a booking request from flat values.
    /// </summary>
    /// <param name="principalCustomerId">The principal customer identifier.</param>
    /// <param name="principalBikeType">The principal customer's bike type.</param>
    /// <param name="roomType">The requested room type.</param>
    /// <param name="discountType">The requested discount type.</param>
    /// <param name="companionCustomerId">The companion customer identifier when present.</param>
    /// <param name="companionBikeType">The companion customer's bike type when present.</param>
    /// <param name="discountAmount">The requested discount amount.</param>
    /// <param name="discountReason">The optional discount reason.</param>
    /// <param name="notes">Optional booking notes.</param>
    public TourBookingRequest(
        Guid principalCustomerId,
        BikeType principalBikeType,
        RoomType roomType,
        DiscountType discountType,
        Guid? companionCustomerId = null,
        BikeType? companionBikeType = null,
        decimal discountAmount = 0m,
        string? discountReason = null,
        string? notes = null)
        : this(
            new BookingTravelers(principalCustomerId, principalBikeType, companionCustomerId, companionBikeType),
            roomType,
            new BookingDiscountDefinition(discountType, discountAmount, discountReason),
            notes)
    {
    }

    /// <summary>
    /// Creates a booking request for a single traveler.
    /// </summary>
    /// <param name="principalCustomerId">The principal customer identifier.</param>
    /// <param name="principalBikeType">The principal customer's bike type.</param>
    /// <param name="roomType">The requested room type.</param>
    /// <param name="discount">The requested discount.</param>
    /// <param name="notes">Optional booking notes.</param>
    /// <returns>A booking request for a single traveler.</returns>
    public static TourBookingRequest CreateSingle(
        Guid principalCustomerId,
        BikeType principalBikeType,
        RoomType roomType,
        BookingDiscountDefinition? discount = null,
        string? notes = null)
    {
        return new TourBookingRequest(
            new BookingTravelers(principalCustomerId, principalBikeType),
            roomType,
            discount ?? BookingDiscountDefinition.None,
            notes);
    }

    /// <summary>
    /// Creates a booking request for two travelers sharing a booking.
    /// </summary>
    /// <param name="principalCustomerId">The principal customer identifier.</param>
    /// <param name="principalBikeType">The principal customer's bike type.</param>
    /// <param name="companionCustomerId">The companion customer identifier.</param>
    /// <param name="companionBikeType">The companion customer's bike type.</param>
    /// <param name="roomType">The requested room type.</param>
    /// <param name="discount">The requested discount.</param>
    /// <param name="notes">Optional booking notes.</param>
    /// <returns>A booking request for two travelers.</returns>
    public static TourBookingRequest CreateDouble(
        Guid principalCustomerId,
        BikeType principalBikeType,
        Guid companionCustomerId,
        BikeType companionBikeType,
        RoomType roomType,
        BookingDiscountDefinition? discount = null,
        string? notes = null)
    {
        return new TourBookingRequest(
            new BookingTravelers(principalCustomerId, principalBikeType, companionCustomerId, companionBikeType),
            roomType,
            discount ?? BookingDiscountDefinition.None,
            notes);
    }
}
