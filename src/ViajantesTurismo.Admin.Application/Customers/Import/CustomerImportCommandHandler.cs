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
        var customersToCreate = new List<Customer>();
        var errorCount = 0;

        foreach (var row in document.Rows)
        {
            var customerResult = RowToCustomerMapper.MapCustomer(document, row, timeProvider);
            if (customerResult.IsSuccess)
            {
                customersToCreate.Add(customerResult.Value);
            }
            else
            {
                errorCount++;
            }
        }

        if (!command.DryRun)
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
