using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.AdminApi.Contracts;

/// <summary>
/// DTO for updating an existing booking.
/// </summary>
public sealed class UpdateBookingDto
{
    /// <summary>The total price of the booking.</summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Total price must be greater than zero")]
    public required decimal TotalPrice { get; init; }

    /// <summary>Optional notes about the booking.</summary>
    [MaxLength(2000)]
    public string? Notes { get; init; }

    /// <summary>The booking status.</summary>
    [Required]
    public required BookingStatusDto Status { get; init; }

    /// <summary>The payment status.</summary>
    [Required]
    public required PaymentStatusDto PaymentStatus { get; init; }
}