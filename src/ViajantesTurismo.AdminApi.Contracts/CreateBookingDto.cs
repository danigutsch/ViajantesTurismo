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

    /// <summary>The total price of the booking.</summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Total price must be greater than zero")]
    public required decimal TotalPrice { get; init; }

    /// <summary>Optional notes about the booking.</summary>
    [MaxLength(2000)]
    public string? Notes { get; init; }
}