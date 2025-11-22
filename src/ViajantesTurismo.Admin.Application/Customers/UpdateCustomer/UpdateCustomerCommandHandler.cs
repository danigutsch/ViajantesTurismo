using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Application.Customers.UpdateCustomer;

/// <summary>
/// Handles updating an existing customer with validation.
/// </summary>
public sealed class UpdateCustomerCommandHandler(
    ICustomerStore customerStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Handles the UpdateCustomerCommand and updates the customer if validation passes.
    /// </summary>
    /// <param name="command">The command containing the customer ID and updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or validation errors.</returns>
    public async Task<Result> Handle(UpdateCustomerCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var customer = await customerStore.GetById(command.CustomerId, ct);
        if (customer is null)
        {
            return CustomerErrors.CustomerNotFound(command.CustomerId);
        }

        if (await customerStore.EmailExistsExcluding(command.ContactInfo.Email, command.CustomerId, ct))
        {
            return CustomerErrors.EmailAlreadyExists(command.ContactInfo.Email);
        }

        var errors = new ValidationErrors();

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
            return errors.ToResult();
        }

        var address = CustomerMapper.MapToAddress(command.Address);
        var physicalInfo = CustomerMapper.MapToPhysicalInfo(command.PhysicalInfo);
        var accommodationPreferences = CustomerMapper.MapToAccommodationPreferences(command.AccommodationPreferences);
        var emergencyContact = CustomerMapper.MapToEmergencyContact(command.EmergencyContact);
        var medicalInfo = CustomerMapper.MapToMedicalInfo(command.MedicalInfo);

        customer.UpdatePersonalInfo(personalInfoResult.Value);
        customer.UpdateIdentificationInfo(identificationInfoResult.Value);
        customer.UpdateContactInfo(contactInfoResult.Value);
        customer.UpdateAddress(address);
        customer.UpdatePhysicalInfo(physicalInfo);
        customer.UpdateAccommodationPreferences(accommodationPreferences);
        customer.UpdateEmergencyContact(emergencyContact);
        customer.UpdateMedicalInfo(medicalInfo);

        await unitOfWork.SaveEntities(ct);

        return Result.Ok();
    }
}
