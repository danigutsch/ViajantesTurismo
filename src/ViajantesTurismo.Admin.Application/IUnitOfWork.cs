namespace ViajantesTurismo.Admin.Application;

/// <summary>
/// Defines a contract for a unit of work that encapsulates a series of operations to be committed as a single transaction.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Commits all changes made in the current unit of work to the underlying data store.
    /// </summary>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    Task SaveEntities(CancellationToken ct);
}
