using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Application.Customers.CreateCustomer;

/// <summary>
/// Handles the creation of a new customer with application-level validation.
/// </summary>
public sealed class CreateCustomerCommandHandler(
    ICustomerStore customerStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Handles the CreateCustomerCommand and returns the ID of the created customer.
    /// </summary>
    /// <param name="command">The command containing customer data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the customer ID if successful, or validation errors.</returns>
    public async Task<Result<Guid>> Handle(CreateCustomerCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new ValidationErrors();

        if (await customerStore.EmailExists(command.ContactInfo.Email, ct))
        {
            errors.Add(CustomerErrors.EmailAlreadyExists(command.ContactInfo.Email));
        }

        var personalInfoResult = PersonalInfo.Create(
            command.PersonalInfo.FirstName,
            command.PersonalInfo.LastName,
            command.PersonalInfo.Gender,
            command.PersonalInfo.BirthDate.ToUniversalTime(),
            command.PersonalInfo.Nationality,
            command.PersonalInfo.Profession,
            timeProvider);

        var identificationInfoResult = IdentificationInfo.Create(
            command.IdentificationInfo.NationalId,
            command.IdentificationInfo.IdNationality);

        var contactInfoResult = ContactInfo.Create(
            command.ContactInfo.Email,
            command.ContactInfo.Mobile,
            command.ContactInfo.Instagram,
            command.ContactInfo.Facebook);

        if (!personalInfoResult.IsSuccess)
        {
            errors.Add(personalInfoResult);
        }

        if (!identificationInfoResult.IsSuccess)
        {
            errors.Add(identificationInfoResult);
        }

        if (!contactInfoResult.IsSuccess)
        {
            errors.Add(contactInfoResult);
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<Guid>();
        }

        var address = CustomerMapper.MapToAddress(command.Address);
        var physicalInfo = CustomerMapper.MapToPhysicalInfo(command.PhysicalInfo);
        var accommodationPreferences = CustomerMapper.MapToAccommodationPreferences(command.AccommodationPreferences);
        var emergencyContact = CustomerMapper.MapToEmergencyContact(command.EmergencyContact);
        var medicalInfo = CustomerMapper.MapToMedicalInfo(command.MedicalInfo);

        var customer = new Customer(
            personalInfoResult.Value,
            identificationInfoResult.Value,
            contactInfoResult.Value,
            address,
            physicalInfo,
            accommodationPreferences,
            emergencyContact,
            medicalInfo);

        customerStore.Add(customer);
        await unitOfWork.SaveEntities(ct);

        return Result<Guid>.Ok(customer.Id);
    }
}
