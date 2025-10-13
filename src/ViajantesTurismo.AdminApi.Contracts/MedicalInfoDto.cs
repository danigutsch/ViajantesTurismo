using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.AdminApi.Contracts;

/// <summary>
/// Data Transfer Object representing the medical information of a customer.
/// Contains health-related details with validation attributes for each property.
/// </summary>
public sealed record MedicalInfoDto
{
    /// <summary>
    /// Known allergies of the customer (e.g., food, medication, environmental).
    /// </summary>
    [Required]
    [MaxLength(ContractConstants.MaxServiceDescriptionLength)]
    public required string? Allergies { get; init; }

    /// <summary>
    /// Additional medical information or conditions relevant for the tour (e.g., chronic conditions, medications, mobility limitations).
    /// </summary>
    [Required]
    [MaxLength(ContractConstants.MaxServiceDescriptionLength)]
    public required string? AdditionalInfo { get; init; }
}
