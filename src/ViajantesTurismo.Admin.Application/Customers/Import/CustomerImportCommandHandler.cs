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
    /// <returns>Aggregated import result counts.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="command"/> is null.</exception>
    public async Task<ImportResult> Handle(CustomerImportCommand command, CancellationToken ct)
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
        var errorCount = 0;

        for (var rowIndex = 0; rowIndex < document.Rows.Count; rowIndex++)
        {
            var lineNumber = rowIndex + 2;
            if (duplicateLineNumbers.Contains(lineNumber))
            {
                errorCount++;
                continue;
            }

            var row = document.Rows[rowIndex];
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
                errorCount++;
                continue;
            }

            customersToCreate.Add(customer);
        }

        if (!command.DryRun && customersToCreate.Count > 0)
        {
            foreach (var customer in customersToCreate)
            {
                customerStore.Add(customer);
            }

            await unitOfWork.SaveEntities(ct);
        }

        return new ImportResult(customersToCreate.Count, errorCount);
    }
}
