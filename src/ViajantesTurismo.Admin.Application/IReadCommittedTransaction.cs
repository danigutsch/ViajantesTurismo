namespace ViajantesTurismo.Admin.Application;

/// <summary>Executes a unit of work inside an explicit read-committed transaction.</summary>
public interface IReadCommittedTransaction
{
    /// <summary>Runs the unit and commits its changes. Transient failures can replay the entire unit.</summary>
    /// <typeparam name="T">The value returned by the unit of work.</typeparam>
    /// <param name="unit">The work to run inside the transaction.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The unit's value after the transaction commits.</returns>
    Task<T> Execute<T>(Func<Task<T>> unit, CancellationToken ct);
}
