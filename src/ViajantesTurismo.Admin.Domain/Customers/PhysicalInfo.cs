using JetBrains.Annotations;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Common.Results;
using static ViajantesTurismo.Admin.Domain.Customers.CustomerErrors;

using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.Domain.Customers;

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
    private PhysicalInfo(decimal weightKg, int heightCentimeters, BikeType bikeType)
    {
        WeightKg = weightKg;
        HeightCentimeters = heightCentimeters;
        BikeType = bikeType;
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
#pragma warning disable CS8618
    [UsedImplicitly]
    private PhysicalInfo()
    {
    }
#pragma warning restore CS8618

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

        if (weightKg is < ContractConstants.MinWeightKg or > ContractConstants.MaxWeightKg)
        {
            errors.Add(InvalidWeight());
        }

        if (heightCentimeters is < ContractConstants.MinHeightCm or > ContractConstants.MaxHeightCm)
        {
            errors.Add(InvalidHeight());
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<PhysicalInfo>();
        }

        return new PhysicalInfo(weightKg, heightCentimeters, bikeType);
    }
}
