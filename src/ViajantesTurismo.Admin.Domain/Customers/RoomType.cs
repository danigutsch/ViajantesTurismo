namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Represents types of hotel room occupancy.
/// </summary>
public enum RoomType
{
    /// <summary>
    /// Double occupancy room (shared room, base price).
    /// </summary>
    DoubleOccupancy = 0,

    /// <summary>
    /// Single occupancy room (solo traveler, supplement applies).
    /// </summary>
    SingleOccupancy = 1
}
