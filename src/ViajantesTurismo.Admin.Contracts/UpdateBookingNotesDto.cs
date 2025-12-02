using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// DTO for updating booking notes.
/// </summary>
public sealed record UpdateBookingNotesDto
{
    /// <summary>Optional notes about the booking.</summary>
    [StringLength(ContractConstants.MaxBookingNotesLength)]
    public string? Notes { get; init; }
}
