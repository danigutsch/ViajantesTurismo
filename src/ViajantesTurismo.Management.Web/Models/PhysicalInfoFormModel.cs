using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Management.Web.Models;

/// <summary>
/// Form model for customer physical details and bike choice.
/// </summary>
internal sealed class PhysicalInfoFormModel
{
    /// <summary>
    /// Body weight in kilograms; required.
    /// </summary>
    [Required(ErrorMessage = "Weight is required")]
    [Range(ContractConstants.MinWeightKg, ContractConstants.MaxWeightKg, ErrorMessage = "Weight must be between 1 and 500 kg")]
    public decimal? WeightKg
    {
        get; set;
    }

    /// <summary>
    /// Height in centimeters; required.
    /// </summary>
    [Required(ErrorMessage = "Height is required")]
    [Range(ContractConstants.MinHeightCm, ContractConstants.MaxHeightCm, ErrorMessage = "Height must be between 50 and 300 cm")]
    public int? HeightCentimeters
    {
        get; set;
    }

    /// <summary>
    /// Bike type the customer will ride; required.
    /// </summary>
    [Required(ErrorMessage = "Bike type is required")]
    public BikeTypeDto? BikeType
    {
        get; set;
    }

    /// <summary>
    /// Converts the validated form to a physical info DTO.
    /// </summary>
    public PhysicalInfoDto ToDto() => new()
    {
        WeightKg = WeightKg!.Value,
        HeightCentimeters = HeightCentimeters!.Value,
        BikeType = BikeType!.Value
    };

    /// <summary>
    /// Creates a form model from a physical info DTO.
    /// </summary>
    public static PhysicalInfoFormModel FromDto(PhysicalInfoDto dto) => new()
    {
        WeightKg = dto.WeightKg,
        HeightCentimeters = dto.HeightCentimeters,
        BikeType = dto.BikeType
    };
}
