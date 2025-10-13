namespace ViajantesTurismo.AdminApi.Contracts;

/// <summary>
/// Data Transfer Object representing complete customer details for display purposes.
/// Contains all customer information including personal, contact, address, and other details.
/// </summary>
public sealed record CustomerDetailsDto
{
    /// <summary>
    /// The unique identifier of the customer.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Personal information.
    /// </summary>
    public required PersonalInfoDto PersonalInfo { get; init; }

    /// <summary>
    /// Identification information.
    /// </summary>
    public required IdentificationInfoDto IdentificationInfo { get; init; }

    /// <summary>
    /// Contact information.
    /// </summary>
    public required ContactInfoDto ContactInfo { get; init; }

    /// <summary>
    /// Physical address.
    /// </summary>
    public required AddressDto Address { get; init; }

    /// <summary>
    /// Physical characteristics and bike preferences.
    /// </summary>
    public required PhysicalInfoDto PhysicalInfo { get; init; }

    /// <summary>
    /// Accommodation preferences.
    /// </summary>
    public required AccommodationPreferencesDto AccommodationPreferences { get; init; }

    /// <summary>
    /// Emergency contact information.
    /// </summary>
    public required EmergencyContactDto EmergencyContact { get; init; }

    /// <summary>
    /// Medical information and allergies.
    /// </summary>
    public required MedicalInfoDto MedicalInfo { get; init; }
}