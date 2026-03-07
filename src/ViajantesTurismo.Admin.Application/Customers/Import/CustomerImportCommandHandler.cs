using ViajantesTurismo.Admin.Application.Import;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>
/// Handles customer import execution and persistence behavior.
/// </summary>
public sealed class CustomerImportCommandHandler(
    ICustomerStore customerStore,
    IUnitOfWork unitOfWork)
{
    /// <summary>
    /// Handles customer import execution.
    /// </summary>
    /// <param name="command">Import command with counts and dry-run option.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Aggregated import result counts.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="command"/> is null.</exception>
    public async Task<ImportResult> Handle(CustomerImportCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!command.DryRun)
        {
            foreach (var customer in command.CustomersToCreate)
            {
                customerStore.Add(customer);
            }

            foreach (var overwritePair in command.CustomersToOverwrite)
            {
                overwritePair.ExistingCustomer.UpdatePersonalInfo(overwritePair.IncomingCustomer.PersonalInfo);
                overwritePair.ExistingCustomer.UpdateIdentificationInfo(overwritePair.IncomingCustomer.IdentificationInfo);
                overwritePair.ExistingCustomer.UpdateContactInfo(overwritePair.IncomingCustomer.ContactInfo);
                overwritePair.ExistingCustomer.UpdateAddress(overwritePair.IncomingCustomer.Address);
                overwritePair.ExistingCustomer.UpdatePhysicalInfo(overwritePair.IncomingCustomer.PhysicalInfo);
                overwritePair.ExistingCustomer.UpdateAccommodationPreferences(overwritePair.IncomingCustomer.AccommodationPreferences);
                overwritePair.ExistingCustomer.UpdateEmergencyContact(overwritePair.IncomingCustomer.EmergencyContact);
                overwritePair.ExistingCustomer.UpdateMedicalInfo(overwritePair.IncomingCustomer.MedicalInfo);
            }

            await unitOfWork.SaveEntities(ct);
        }

        return new ImportResult(command.SuccessCount, command.ErrorCount);
    }
}
