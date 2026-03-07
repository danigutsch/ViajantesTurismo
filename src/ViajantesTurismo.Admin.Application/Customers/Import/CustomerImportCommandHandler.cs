namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>
/// Handles customer import execution and persistence behavior.
/// </summary>
public sealed class CustomerImportCommandHandler(IUnitOfWork unitOfWork)
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
            await unitOfWork.SaveEntities(ct);
        }

        return new ImportResult(command.SuccessCount, command.ErrorCount);
    }
}
