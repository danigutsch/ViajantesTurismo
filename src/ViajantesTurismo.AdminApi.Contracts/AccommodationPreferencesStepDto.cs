using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.AdminApi.Contracts;

/// <summary>
/// Represents the accommodation preferences for a customer.
/// </summary>
public sealed record AccommodationPreferencesStepDto
{
    /// <summary>
    /// The type of room preferred.
    /// </summary>
    [Required]
    public required RoomTypeDto? RoomType { get; init; }

    /// <summary>
    /// The type of bed preferred.
    /// </summary>
    [Required]
    [MaxLength(ContractConstants.MaxDefaultLength)]
    public required string? BedType { get; init; }

    /// <summary>
    /// The first name of the companion.
    /// </summary>
    [Required]
    [MaxLength(ContractConstants.MaxNameLength)]
    public required string? CompanionFirstName { get; init; }

    /// <summary>
    /// The last name of the companion.
    /// </summary>
    [Required]
    [MaxLength(ContractConstants.MaxNameLength)]
    public required string? CompanionLastName { get; init; }
}
