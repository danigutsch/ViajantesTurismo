using ViajantesTurismo.Common;

namespace ViajantesTurismo.Admin.Domain;

/// <summary>
/// Represents physical characteristics and bike preferences.
/// </summary>
public sealed class PhysicalInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicalInfo"/> class.
    /// </summary>
    /// <param name="weightKg">The weight in kilograms.</param>
    /// <param name="heightCentimeters">The height in centimeters.</param>
    /// <param name="bikeType">The type of bicycle.</param>
    public PhysicalInfo(decimal weightKg, int heightCentimeters, BikeType bikeType)
    {
        WeightKg = weightKg;
        HeightCentimeters = heightCentimeters;
        BikeType = bikeType;
    }

    /// <summary>Weight in kilograms.</summary>
    public decimal WeightKg { get; private set; }

    /// <summary>Height in centimeters.</summary>
    public int HeightCentimeters { get; private set; }

    /// <summary>Type of bicycle.</summary>
    public BikeType BikeType { get; private set; }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
#pragma warning disable CS8618
    private PhysicalInfo()
    {
    }
}
