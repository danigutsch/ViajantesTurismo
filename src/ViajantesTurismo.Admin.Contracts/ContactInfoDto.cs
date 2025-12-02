using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// Data Transfer Object representing the contact information of a customer.
/// Contains communication and social media details with validation attributes for each property.
/// </summary>
public sealed record ContactInfoDto
{
    /// <summary>
    /// The email address of the customer.
    /// </summary>
    [Required]
    [StringLength(ContractConstants.MaxNameLength, MinimumLength = 1)]
    [EmailAddress]
    public required string Email { get; init; }

    /// <summary>
    /// The mobile phone number of the customer.
    /// </summary>
    [Required]
    [StringLength(ContractConstants.MaxDefaultLength, MinimumLength = 1)]
    [Phone]
    public required string Mobile { get; init; }

    /// <summary>
    /// The Instagram handle or profile URL of the customer.
    /// </summary>
    [Required]
    [StringLength(ContractConstants.MaxDefaultLength)]
    public required string? Instagram { get; init; }

    /// <summary>
    /// The Facebook profile name or URL of the customer.
    /// </summary>
    [Required]
    [StringLength(ContractConstants.MaxDefaultLength)]
    public required string? Facebook { get; init; }
}
