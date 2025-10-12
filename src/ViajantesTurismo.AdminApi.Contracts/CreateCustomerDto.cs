namespace ViajantesTurismo.AdminApi.Contracts;

/// <summary>
/// Represents the data required to create a new customer with all required information.
/// </summary>
public sealed record CreateCustomerDto
{
    /// <summary>
    /// Gets the personal information of the customer.
    /// </summary>
    public required PersonalInfoStepDto PersonalInfo { get; init; }

    /// <summary>
    /// Gets the identification information of the customer.
    /// </summary>
    public required IdentificationInfoStepDto IdentificationInfo { get; init; }

    /// <summary>
    /// Gets the contact information of the customer.
    /// </summary>
    public required ContactInfoStepDto ContactInfo { get; init; }

    /// <summary>
    /// Gets the address of the customer.
    /// </summary>
    public required AddressStepDto Address { get; init; }

    /// <summary>
    /// Gets the physical information of the customer.
    /// </summary>
    public required PhysicalInfoStepDto PhysicalInfo { get; init; }

    /// <summary>
    /// Gets the accommodation preferences of the customer.
    /// </summary>
    public required AccommodationPreferencesStepDto AccommodationPreferences { get; init; }

    /// <summary>
    /// Gets the emergency contact information of the customer.
    /// </summary>
    public required EmergencyContactStepDto EmergencyContact { get; init; }

    /// <summary>
    /// Gets the medical information of the customer.
    /// </summary>
    public required MedicalInfoStepDto MedicalInfo { get; init; }
}