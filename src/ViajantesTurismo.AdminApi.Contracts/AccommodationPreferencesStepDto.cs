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
    public required BedTypeDto? BedType { get; init; }

    /// <summary>
    /// The ID of the companion.
    /// </summary>
    [Required]
    public required int? CompanionId { get; init; }
}
