namespace ViajantesTurismo.Admin.Application.Documents.Holds;

/// <summary>
/// Runs a document disposition or hold change in one explicit database transaction at read committed, re-run as a
/// whole on a transient failure, so the unit must be idempotent and must not rely on a save's implicit transaction.
/// </summary>
public interface IDocumentDispositionTransaction
{
    /// <summary>
    /// Takes the named command locks first, in a fixed order, then runs <paramref name="unit" /> and commits. Hold locks
    /// are taken by the hold store and guard inside the unit, so they come last.
    /// </summary>
    /// <typeparam name="T">The unit's result type.</typeparam>
    /// <param name="lockNames">Opaque names of the locks the command declares; empty for a purge.</param>
    /// <param name="unit">The work to run inside the transaction.</param>
    /// <param name="ct">A token that can cancel the operation.</param>
    /// <returns>The unit's result, once committed.</returns>
    Task<T> Execute<T>(IReadOnlyList<string> lockNames, Func<Task<T>> unit, CancellationToken ct);
}
