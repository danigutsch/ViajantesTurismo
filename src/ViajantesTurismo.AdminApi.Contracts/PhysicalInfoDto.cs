using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.AdminApi.Contracts;

/// <summary>
/// Data Transfer Object representing the physical information of a customer.
/// Contains physical characteristics and bike preference with validation attributes for each property.
/// </summary>
public sealed record PhysicalInfoDto
{
    /// <summary>
    /// The weight of the customer in kilograms. Valid range is 1 to 500 kg.
    /// </summary>
    [Required]
    [Range(1, 500)]
    public required decimal WeightKg { get; init; }

    /// <summary>
    /// The height of the customer in centimeters. Valid range is 50 to 300 cm.
    /// </summary>
    [Required]
    [Range(50, 300)]
    public required int HeightCentimeters { get; init; }

    /// <summary>
    /// The type of bike preferred by the customer (None, Regular, or E-Bike).
    /// </summary>
    [Required]
    public required BikeTypeDto BikeType { get; init; }
}