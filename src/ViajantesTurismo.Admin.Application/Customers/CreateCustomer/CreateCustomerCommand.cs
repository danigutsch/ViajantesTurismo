using SharedKernel.Mediator;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Customers.CreateCustomer;

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
    MedicalInfoDto MedicalInfo) : ICommand<Result<Guid>>
{
    /// <summary>Gets the customer identifier retained across attempts of this command.</summary>
    public Guid CustomerId { get; } = Guid.CreateVersion7();
}
