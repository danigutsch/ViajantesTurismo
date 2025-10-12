using ViajantesTurismo.Common;

namespace ViajantesTurismo.Admin.Domain;

/// <summary>
/// Represents physical characteristics and bike preferences.
/// </summary>
public sealed class PhysicalInfo
{
    /// <summary>Weight in kilograms.</summary>
    public required decimal WeightKg { get; init; }

    /// <summary>Height in centimeters.</summary>
    public required int HeightCentimeters { get; init; }

    /// <summary>Type of bicycle.</summary>
    public required BikeType BikeType { get; init; }
}