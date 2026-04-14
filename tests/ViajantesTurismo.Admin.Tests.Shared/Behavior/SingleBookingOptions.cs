using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Tests.Shared.Behavior;

/// <summary>
/// Options for creating a single-customer booking in tests.
/// </summary>
/// <param name="CustomerId">The customer identifier override.</param>
/// <param name="BikeType">The bike type override.</param>
/// <param name="RoomType">The room type override.</param>
/// <param name="Discount">The discount override.</param>
/// <param name="SpecialRequests">The booking notes override.</param>
public sealed record SingleBookingOptions(
    Guid? CustomerId = null,
    BikeType BikeType = BikeType.Regular,
    RoomType RoomType = RoomType.DoubleOccupancy,
    BookingDiscountDefinition? Discount = null,
    string? SpecialRequests = null);
