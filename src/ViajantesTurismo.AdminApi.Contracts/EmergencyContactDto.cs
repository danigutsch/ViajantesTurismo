using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.AdminApi.Contracts;

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
    [MaxLength(ContractConstants.MaxNameLength)]
    public required string Name { get; init; }

    /// <summary>
    /// The mobile phone number of the emergency contact person.
    /// </summary>
    [Required]
    [MaxLength(ContractConstants.MaxDefaultLength)]
    [Phone]
    public required string Mobile { get; init; }
}
