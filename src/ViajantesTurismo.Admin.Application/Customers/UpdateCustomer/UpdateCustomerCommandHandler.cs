using PandaBear.Mediator;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Domain.Customers;
using PandaBear.Results;
using PandaBear.InputNormalization;

namespace ViajantesTurismo.Admin.Application.Customers.UpdateCustomer;

/// <summary>
/// Handles updating an existing customer with validation.
/// </summary>
public sealed class UpdateCustomerCommandHandler(
    ICustomerStore customerStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<UpdateCustomerCommand, Result>
{
    /// <summary>
    /// Handles the UpdateCustomerCommand and updates the customer if validation passes.
    /// </summary>
    /// <param name="command">The command containing the customer ID and updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or validation errors.</returns>
    public async ValueTask<Result> Handle(UpdateCustomerCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var customer = await customerStore.GetById(command.CustomerId, ct);
        if (customer is null)
        {
            return CustomerErrors.CustomerNotFound();
        }

        var sanitizedEmail = StringSanitizer.Sanitize(command.ContactInfo.Email);

        if (await customerStore.EmailExistsExcluding(sanitizedEmail, command.CustomerId, ct))
        {
            return CustomerErrors.EmailAlreadyExists();
        }

        var revisionResult = customer.Revise(
            new CustomerDefinition(
                CustomerMapper.MapToPersonalInfoDefinition(command.PersonalInfo),
                CustomerMapper.MapToIdentificationInfoDefinition(command.IdentificationInfo),
                CustomerMapper.MapToContactInfoDefinition(command.ContactInfo),
                CustomerMapper.MapToAddressDefinition(command.Address),
                CustomerMapper.MapToPhysicalInfoDefinition(command.PhysicalInfo),
                CustomerMapper.MapToAccommodationPreferencesDefinition(command.AccommodationPreferences),
                CustomerMapper.MapToHealthDefinition(command.EmergencyContact, command.MedicalInfo)),
            DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime));
        if (revisionResult.IsFailure)
        {
            return revisionResult;
        }

        try
        {
            await unitOfWork.SaveEntities(ct);
        }
        catch (CustomerEmailConflictException)
        {
            return CustomerErrors.EmailAlreadyExists();
        }

        return Result.Ok();
    }
}
