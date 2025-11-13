using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Application.Customers.Commands.UpdateCustomer;

/// <summary>
/// Command to update an existing customer's information.
/// </summary>
public sealed record UpdateCustomerCommand(
    Guid CustomerId,
    PersonalInfoDto PersonalInfo,
    IdentificationInfoDto IdentificationInfo,
    ContactInfoDto ContactInfo,
    AddressDto Address,
    PhysicalInfoDto PhysicalInfo,
    AccommodationPreferencesDto AccommodationPreferences,
    EmergencyContactDto EmergencyContact,
    MedicalInfoDto MedicalInfo);
