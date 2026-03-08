using ViajantesTurismo.Admin.Application.Import;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>
/// Handles customer import execution: parses CSV, maps rows to domain entities, and persists.
/// </summary>
public sealed class CustomerImportCommandHandler(
    ICustomerStore customerStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Parses the CSV content, maps valid rows to customers, and persists when not a dry run.
    /// </summary>
    /// <param name="command">Import command carrying CSV content and dry-run flag.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="conflictResolutions">Optional conflict resolutions keyed by email ("keep" or "overwrite").</param>
    /// <returns>Aggregated import result counts.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="command"/> is null.</exception>
    public async Task<ImportResult> Handle(
        CustomerImportCommand command,
        CancellationToken ct,
        IReadOnlyDictionary<string, string>? conflictResolutions = null)
    {
        ArgumentNullException.ThrowIfNull(command);

        var documentResult = CsvDocument.Parse(command.CsvContent);
        if (documentResult.IsFailure)
        {
            return new ImportResult(0, 0);
        }

        var document = documentResult.Value;
        var duplicateLineNumbers = new HashSet<int>(DuplicateDetector.FindDuplicateEmailLineNumbers(document));

        var customersToCreate = new List<Customer>();
        var customersToOverwrite = new List<CustomerOverwritePair>();
        var errorCount = 0;

        foreach (var (row, rowIndex) in document.Rows.Select((row, index) => (row, index)))
        {
            var lineNumber = rowIndex + 2;
            if (duplicateLineNumbers.Contains(lineNumber))
            {
                errorCount++;
                continue;
            }

            var customerResult = RowToCustomerMapper.MapCustomer(document, row, timeProvider);
            if (customerResult.IsFailure)
            {
                errorCount++;
                continue;
            }

            var customer = customerResult.Value;
            var emailAlreadyExists = await customerStore.EmailExists(customer.ContactInfo.Email, ct);
            if (emailAlreadyExists)
            {
                if (TryResolveConflict(conflictResolutions, customer.ContactInfo.Email, out var resolution))
                {
                    if (resolution.SkipsImport)
                    {
                        continue;
                    }

                    var existingCustomer = await customerStore.GetByEmail(customer.ContactInfo.Email, ct);
                    if (existingCustomer is null)
                    {
                        errorCount++;
                        continue;
                    }

                    customersToOverwrite.Add(new CustomerOverwritePair(existingCustomer, customer));
                    continue;
                }

                errorCount++;
                continue;
            }

            customersToCreate.Add(customer);
        }

        if (!command.DryRun && (customersToCreate.Count > 0 || customersToOverwrite.Count > 0))
        {
            foreach (var customer in customersToCreate)
            {
                customerStore.Add(customer);
            }

            foreach (var pair in customersToOverwrite)
            {
                ApplyOverwrite(pair.ExistingCustomer, pair.IncomingCustomer);
            }

            await unitOfWork.SaveEntities(ct);
        }

        return new ImportResult(customersToCreate.Count + customersToOverwrite.Count, errorCount);
    }

    private static bool TryResolveConflict(
        IReadOnlyDictionary<string, string>? conflictResolutions,
        string email,
        out ConflictResolution resolution)
    {
        resolution = default;

        if (conflictResolutions is null)
        {
            return false;
        }

        var matched = conflictResolutions.FirstOrDefault(kvp =>
            string.Equals(kvp.Key, email, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(matched.Key))
        {
            return false;
        }

        resolution = matched.Value.Equals("overwrite", StringComparison.OrdinalIgnoreCase)
            ? ConflictResolution.Overwrite
            : ConflictResolution.Keep;

        return true;
    }

    private static void ApplyOverwrite(Customer existingCustomer, Customer incomingCustomer)
    {
        existingCustomer.UpdatePersonalInfo(incomingCustomer.PersonalInfo);
        existingCustomer.UpdateIdentificationInfo(incomingCustomer.IdentificationInfo);
        existingCustomer.UpdateContactInfo(incomingCustomer.ContactInfo);
        existingCustomer.UpdateAddress(incomingCustomer.Address);
        existingCustomer.UpdatePhysicalInfo(incomingCustomer.PhysicalInfo);
        existingCustomer.UpdateAccommodationPreferences(incomingCustomer.AccommodationPreferences);
        existingCustomer.UpdateEmergencyContact(incomingCustomer.EmergencyContact);
        existingCustomer.UpdateMedicalInfo(incomingCustomer.MedicalInfo);
    }
}
