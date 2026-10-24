using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Defines the raw accommodation preferences of a customer.
/// </summary>
/// <param name="RoomType">The preferred room type.</param>
/// <param name="BedType">The preferred bed type.</param>
/// <param name="CompanionId">The optional companion customer identifier.</param>
public sealed record AccommodationPreferencesDefinition(RoomType RoomType, BedType BedType, Guid? CompanionId);
