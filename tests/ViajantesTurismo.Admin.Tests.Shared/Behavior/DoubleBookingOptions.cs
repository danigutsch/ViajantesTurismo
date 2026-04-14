using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Tests.Shared.Behavior;

/// <summary>
/// Options for creating a double-customer booking in tests.
/// </summary>
/// <param name="PrincipalCustomerId">The principal customer identifier override.</param>
/// <param name="CompanionCustomerId">The companion customer identifier override.</param>
/// <param name="PrincipalBikeType">The principal bike type override.</param>
/// <param name="CompanionBikeType">The companion bike type override.</param>
/// <param name="RoomType">The room type override.</param>
/// <param name="Discount">The discount override.</param>
/// <param name="SpecialRequests">The booking notes override.</param>
public sealed record DoubleBookingOptions(
    Guid? PrincipalCustomerId = null,
    Guid? CompanionCustomerId = null,
    BikeType PrincipalBikeType = BikeType.Regular,
    BikeType CompanionBikeType = BikeType.Regular,
    RoomType RoomType = RoomType.DoubleOccupancy,
    BookingDiscountDefinition? Discount = null,
    string? SpecialRequests = null);
