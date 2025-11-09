namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// Represents the data required to create a new customer with all required information.
/// </summary>
public sealed record CreateCustomerDto
{
    /// <summary>
    /// Gets the personal information of the customer.
    /// </summary>
    public required PersonalInfoDto PersonalInfo { get; init; }

    /// <summary>
    /// Gets the identification information of the customer.
    /// </summary>
    public required IdentificationInfoDto IdentificationInfo { get; init; }

    /// <summary>
    /// Gets the contact information of the customer.
    /// </summary>
    public required ContactInfoDto ContactInfo { get; init; }

    /// <summary>
    /// Gets the address of the customer.
    /// </summary>
    public required AddressDto Address { get; init; }

    /// <summary>
    /// Gets the physical information of the customer.
    /// </summary>
    public required PhysicalInfoDto PhysicalInfo { get; init; }

    /// <summary>
    /// Gets the accommodation preferences of the customer.
    /// </summary>
    public required AccommodationPreferencesDto AccommodationPreferences { get; init; }

    /// <summary>
    /// Gets the emergency contact information of the customer.
    /// </summary>
    public required EmergencyContactDto EmergencyContact { get; init; }

    /// <summary>
    /// Gets the medical information of the customer.
    /// </summary>
    public required MedicalInfoDto MedicalInfo { get; init; }
}
