using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.AdminApi.Contracts;

/// <summary>
/// DTO for updating booking details (price and notes).
/// </summary>
public sealed class UpdateBookingDetailsDto
{
    /// <summary>The total price of the booking.</summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Total price must be greater than zero")] // Must be greater than zero (exclusive)
    public required decimal TotalPrice { get; init; }

    /// <summary>Optional notes about the booking.</summary>
    [MaxLength(ContractConstants.MaxBookingNotesLength)]
    public string? Notes { get; init; }
}