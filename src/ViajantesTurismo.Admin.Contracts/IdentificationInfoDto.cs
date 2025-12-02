using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// Data Transfer Object representing the identification information of a customer.
/// Contains identification document details with validation attributes for each property.
/// </summary>
public sealed record IdentificationInfoDto
{
    /// <summary>
    /// The national identification number or document number of the customer.
    /// </summary>
    [Required]
    [StringLength(ContractConstants.MaxDefaultLength, MinimumLength = 1)]
    public required string NationalId { get; init; }

    /// <summary>
    /// The country of issuance for the identification document.
    /// </summary>
    [Required]
    [StringLength(ContractConstants.MaxDefaultLength, MinimumLength = 1)]
    public required string IdNationality { get; init; }
}
