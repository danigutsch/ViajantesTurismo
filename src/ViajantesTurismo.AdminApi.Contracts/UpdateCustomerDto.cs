using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.AdminApi.Contracts;

/// <summary>
/// Data Transfer Object for updating an existing customer.
/// </summary>
public sealed record UpdateCustomerDto
{
    /// <summary>
    /// Personal information.
    /// </summary>
    [Required]
    public required PersonalInfoDto PersonalInfo { get; init; }

    /// <summary>
    /// Identification information.
    /// </summary>
    [Required]
    public required IdentificationInfoDto IdentificationInfo { get; init; }

    /// <summary>
    /// Contact information.
    /// </summary>
    [Required]
    public required ContactInfoDto ContactInfo { get; init; }

    /// <summary>
    /// Physical address.
    /// </summary>
    [Required]
    public required AddressDto Address { get; init; }

    /// <summary>
    /// Physical characteristics and bike preferences.
    /// </summary>
    [Required]
    public required PhysicalInfoDto PhysicalInfo { get; init; }

    /// <summary>
    /// Accommodation preferences.
    /// </summary>
    [Required]
    public required AccommodationPreferencesDto AccommodationPreferences { get; init; }

    /// <summary>
    /// Emergency contact information.
    /// </summary>
    [Required]
    public required EmergencyContactDto EmergencyContact { get; init; }

    /// <summary>
    /// Medical information and allergies.
    /// </summary>
    [Required]
    public required MedicalInfoDto MedicalInfo { get; init; }
}