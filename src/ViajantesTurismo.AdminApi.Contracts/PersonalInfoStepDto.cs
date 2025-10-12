using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.AdminApi.Contracts;

/// <summary>
/// Data Transfer Object representing the personal information step in a multistep customer creation wizard.
/// Contains basic personal details with validation attributes for each property.
/// </summary>
public sealed record PersonalInfoStepDto
{
    /// <summary>
    /// The first name of the customer.
    /// </summary>
    [Required]
    [MaxLength(ContractConstants.MaxNameLength)]
    public required string? FirstName { get; init; }

    /// <summary>
    /// The last name of the customer.
    /// </summary>
    [Required]
    [MaxLength(ContractConstants.MaxNameLength)]
    public required string? LastName { get; init; }

    /// <summary>
    /// The date of birth of the customer.
    /// </summary>
    [Required]
    public required DateTime? BirthDate { get; init; }

    /// <summary>
    /// The gender of the customer.
    /// </summary>
    [Required]
    [MaxLength(ContractConstants.MaxDefaultLength)]
    public required string? Gender { get; init; }

    /// <summary>
    /// The nationality of the customer.
    /// </summary>
    [Required]
    [MaxLength(ContractConstants.MaxNameLength)]
    public required string? Nationality { get; init; }

    /// <summary>
    /// The profession or occupation of the customer.
    /// </summary>
    [Required]
    [MaxLength(ContractConstants.MaxNameLength)]
    public required string? Profession { get; init; }
}