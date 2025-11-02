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

    /// <summary>The ID of the customer making the booking.</summary>
    [Required]
    public required int CustomerId { get; init; }

    /// <summary>The ID of the companion customer, if any.</summary>
    public int? CompanionId { get; init; }

    /// <summary>Optional notes about the booking.</summary>
    [MaxLength(2000)]
    public string? Notes { get; init; }
}