namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// Represents types of hotel room occupancy.
/// </summary>
public enum RoomTypeDto
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
