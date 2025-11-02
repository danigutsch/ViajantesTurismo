using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.AdminApi.Contracts;

/// <summary>
/// DTO for creating a new booking.
/// </summary>
public sealed class CreateBookingDto
{
    /// <summary>The ID of the tour being booked.</summary>
    [Required]
    public required int TourId { get; init; }

    /// <summary>The ID of the principal customer making the booking.</summary>
    [Required]
    public required int PrincipalCustomerId { get; init; }

    /// <summary>The bike type selected by the principal customer.</summary>
    [Required]
    public required BikeTypeDto PrincipalBikeType { get; init; }

    /// <summary>The ID of the companion customer, if any.</summary>
    public int? CompanionCustomerId { get; init; }

    /// <summary>The bike type selected by the companion, if any.</summary>
    public BikeTypeDto? CompanionBikeType { get; init; }

    /// <summary>The room type for the booking.</summary>
    [Required]
    public required RoomTypeDto RoomType { get; init; }

    /// <summary>Optional notes about the booking.</summary>
    [MaxLength(ContractConstants.MaxBookingNotesLength)]
    public string? Notes { get; init; }
}