using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// Data Transfer Object representing the emergency contact information of a customer.
/// Contains emergency contact details with validation attributes for each property.
/// </summary>
public sealed record EmergencyContactDto
{
    /// <summary>
    /// The full name of the emergency contact person.
    /// </summary>
    [Required]
    [StringLength(ContractConstants.MaxNameLength, MinimumLength = 1)]
    public required string Name { get; init; }

    /// <summary>
    /// The mobile phone number of the emergency contact person.
    /// </summary>
    [Required]
    [StringLength(ContractConstants.MaxDefaultLength, MinimumLength = 1)]
    [Phone]
    public required string Mobile { get; init; }
}
