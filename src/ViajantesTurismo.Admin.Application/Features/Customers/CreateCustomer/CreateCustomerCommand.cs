using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Application.Features.Customers.CreateCustomer;

/// <summary>
/// Command to create a new customer with all required information.
/// </summary>
public sealed record CreateCustomerCommand(
    PersonalInfoDto PersonalInfo,
    IdentificationInfoDto IdentificationInfo,
    ContactInfoDto ContactInfo,
    AddressDto Address,
    PhysicalInfoDto PhysicalInfo,
    AccommodationPreferencesDto AccommodationPreferences,
    EmergencyContactDto EmergencyContact,
    MedicalInfoDto MedicalInfo);
