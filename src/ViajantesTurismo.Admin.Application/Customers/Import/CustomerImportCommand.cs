using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>
/// Command carrying customer import execution options and counters.
/// </summary>
public sealed record CustomerImportCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerImportCommand"/> record.
    /// </summary>
    /// <param name="successCount">Number of successfully processed rows.</param>
    /// <param name="errorCount">Number of rows that failed processing.</param>
    /// <param name="dryRun">Whether the import should skip persistence.</param>
    public CustomerImportCommand(
        int successCount,
        int errorCount,
        bool dryRun)
        : this(successCount, errorCount, dryRun, [], [])
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerImportCommand"/> record.
    /// </summary>
    /// <param name="successCount">Number of successfully processed rows.</param>
    /// <param name="errorCount">Number of rows that failed processing.</param>
    /// <param name="dryRun">Whether the import should skip persistence.</param>
    /// <param name="customersToCreate">Customers that should be created when import is committed.</param>
    public CustomerImportCommand(
        int successCount,
        int errorCount,
        bool dryRun,
        IReadOnlyList<Customer> customersToCreate)
        : this(successCount, errorCount, dryRun, customersToCreate, [])
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerImportCommand"/> record.
    /// </summary>
    /// <param name="successCount">Number of successfully processed rows.</param>
    /// <param name="errorCount">Number of rows that failed processing.</param>
    /// <param name="dryRun">Whether the import should skip persistence.</param>
    /// <param name="customersToCreate">Customers that should be created when import is committed.</param>
    /// <param name="customersToOverwrite">Existing customers that should be overwritten by imported data.</param>
    public CustomerImportCommand(
        int successCount,
        int errorCount,
        bool dryRun,
        IReadOnlyList<Customer> customersToCreate,
        IReadOnlyList<CustomerOverwritePair> customersToOverwrite)
    {
        SuccessCount = successCount;
        ErrorCount = errorCount;
        DryRun = dryRun;
        CustomersToCreate = customersToCreate;
        CustomersToOverwrite = customersToOverwrite;
    }

    /// <summary>
    /// Number of successfully processed rows.
    /// </summary>
    public int SuccessCount { get; }

    /// <summary>
    /// Number of rows that failed processing.
    /// </summary>
    public int ErrorCount { get; }

    /// <summary>
    /// Whether the import should skip persistence.
    /// </summary>
    public bool DryRun { get; }

    /// <summary>
    /// Customers that should be created when import is committed.
    /// </summary>
    public IReadOnlyList<Customer> CustomersToCreate { get; }

    /// <summary>
    /// Existing customers that should be overwritten by imported data.
    /// </summary>
    public IReadOnlyList<CustomerOverwritePair> CustomersToOverwrite { get; }
}
