using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Domain.Customers;
using PandaBear.Results;
using PandaBear.InputNormalization;

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

        var sanitizedEmail = StringSanitizer.Sanitize(command.ContactInfo.Email);

        if (await customerStore.EmailExists(sanitizedEmail, ct))
        {
            return CustomerErrors.EmailAlreadyExists().ConvertError<Guid>();
        }

        var customerResult = Customer.Create(
            new CustomerDefinition(
                CustomerMapper.MapToPersonalInfoDefinition(command.PersonalInfo),
                CustomerMapper.MapToIdentificationInfoDefinition(command.IdentificationInfo),
                CustomerMapper.MapToContactInfoDefinition(command.ContactInfo),
                CustomerMapper.MapToAddressDefinition(command.Address),
                CustomerMapper.MapToPhysicalInfoDefinition(command.PhysicalInfo),
                CustomerMapper.MapToAccommodationPreferencesDefinition(command.AccommodationPreferences),
                CustomerMapper.MapToHealthDefinition(command.EmergencyContact, command.MedicalInfo)),
            DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime));
        if (customerResult.IsFailure)
        {
            return customerResult.ConvertError<Customer, Guid>();
        }

        var customer = customerResult.Value;
        customerStore.Add(customer);
        try
        {
            await unitOfWork.SaveEntities(ct);
        }
        catch (CustomerEmailConflictException)
        {
            return CustomerErrors.EmailAlreadyExists().ConvertError<Guid>();
        }

        return Result.Ok(customer.Id);
    }
}
