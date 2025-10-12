using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.AdminApi.Contracts;

/// <summary>
/// Data Transfer Object representing the identification information step in a multistep customer creation wizard.
/// Contains identification document details with validation attributes for each property.
/// </summary>
public sealed record IdentificationInfoStepDto
{
    /// <summary>
    /// The national identification number or document number of the customer.
    /// </summary>
    [Required]
    [MaxLength(ContractConstants.MaxDefaultLength)]
    public required string? NationalId { get; init; }

    /// <summary>
    /// The country of issuance for the identification document.
    /// </summary>
    [Required]
    [MaxLength(ContractConstants.MaxNameLength)]
    public required string? IdNationality { get; init; }
}
