using JetBrains.Annotations;
using ViajantesTurismo.Common.Results;
using static ViajantesTurismo.Admin.Domain.Customers.CustomerErrors;

namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Represents physical characteristics and bike preferences.
/// </summary>
public sealed class PhysicalInfo
{
    private const decimal MinWeightKg = 1m;
    private const decimal MaxWeightKg = 500m;
    private const int MinHeightCm = 50;
    private const int MaxHeightCm = 300;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicalInfo"/> class.
    /// </summary>
    /// <param name="weightKg">The weight in kilograms.</param>
    /// <param name="heightCentimeters">The height in centimeters.</param>
    /// <param name="bikeType">The type of bicycle.</param>
    private PhysicalInfo(decimal weightKg, int heightCentimeters, BikeType bikeType)
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
    /// Creates a new instance of <see cref="PhysicalInfo"/> with validation.
    /// </summary>
    /// <param name="weightKg">The weight in kilograms.</param>
    /// <param name="heightCentimeters">The height in centimeters.</param>
    /// <param name="bikeType">The type of bicycle.</param>
    /// <returns>A <see cref="Result{PhysicalInfo}"/> containing the physical info or validation errors.</returns>
    public static Result<PhysicalInfo> Create(decimal weightKg, int heightCentimeters, BikeType bikeType)
    {
        var errors = new ValidationErrors();

        if (weightKg < MinWeightKg || weightKg > MaxWeightKg)
        {
            errors.Add(InvalidWeight());
        }

        if (heightCentimeters < MinHeightCm || heightCentimeters > MaxHeightCm)
        {
            errors.Add(InvalidHeight());
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<PhysicalInfo>();
        }

        return new PhysicalInfo(weightKg, heightCentimeters, bikeType);
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
#pragma warning disable CS8618
    [UsedImplicitly]
    private PhysicalInfo()
    {
    }
}