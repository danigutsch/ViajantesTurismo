using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// Data Transfer Object representing the address information of a customer.
/// Contains address-related fields with validation attributes for each property.
/// </summary>
public sealed record AddressDto
{
    /// <summary>
    /// The street name of the address.
    /// </summary>
    [Required]
    [StringLength(ContractConstants.MaxNameLength, MinimumLength = 1)]
    public required string Street { get; init; }

    /// <summary>
    /// Additional address information, such as apartment or suite number.
    /// </summary>
    [Required]
    [StringLength(ContractConstants.MaxNameLength)]
    public required string? Complement { get; init; }

    /// <summary>
    /// The neighborhood or district of the address.
    /// </summary>
    [Required]
    [StringLength(ContractConstants.MaxNameLength)]
    public required string? Neighborhood { get; init; }

    /// <summary>
    /// The postal code or ZIP code of the address.
    /// </summary>
    [Required]
    [StringLength(ContractConstants.MaxDefaultLength, MinimumLength = 1)]
    public required string PostalCode { get; init; }

    /// <summary>
    /// The city where the address is located.
    /// </summary>
    [Required]
    [StringLength(ContractConstants.MaxNameLength, MinimumLength = 1)]
    public required string City { get; init; }

    /// <summary>
    /// The state or province of the address.
    /// </summary>
    [Required]
    [StringLength(ContractConstants.MaxNameLength, MinimumLength = 1)]
    public required string State { get; init; }

    /// <summary>
    /// The country of the address.
    /// </summary>
    [Required]
    [StringLength(ContractConstants.MaxNameLength, MinimumLength = 1)]
    public required string Country { get; init; }
}
