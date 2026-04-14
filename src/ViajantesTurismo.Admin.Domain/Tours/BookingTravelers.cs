using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Defines the travelers participating in a booking.
/// </summary>
/// <param name="PrincipalCustomerId">The principal customer identifier.</param>
/// <param name="PrincipalBikeType">The principal customer's bike type.</param>
/// <param name="CompanionCustomerId">The companion customer identifier when present.</param>
/// <param name="CompanionBikeType">The companion customer's bike type when present.</param>
public sealed record BookingTravelers(
    Guid PrincipalCustomerId,
    BikeType PrincipalBikeType,
    Guid? CompanionCustomerId = null,
    BikeType? CompanionBikeType = null);
