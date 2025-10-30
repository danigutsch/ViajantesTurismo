namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Represents types of bicycles available for tours.
/// </summary>
public enum BikeType
{
    /// <summary>
    /// No bike required or selected.
    /// </summary>
    None = 0,

    /// <summary>
    /// Standard mechanical bicycle.
    /// </summary>
    Regular = 1,

    /// <summary>
    /// Electric bicycle (e-bike).
    /// </summary>
    EBike = 2
}
