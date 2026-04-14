using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Represents the room selection and associated room cost for a booking.
/// </summary>
/// <param name="RoomType">The selected room type.</param>
/// <param name="AdditionalCost">The additional room cost for the selected room type.</param>
public readonly record struct BookingRoom(RoomType RoomType, decimal AdditionalCost);
