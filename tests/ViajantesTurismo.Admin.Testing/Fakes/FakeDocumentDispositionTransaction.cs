using System.Collections.ObjectModel;
using ViajantesTurismo.Admin.Application.Documents.Holds;

namespace ViajantesTurismo.Admin.Testing.Fakes;

/// <summary>Runs the unit directly and records the command lock names, standing in for a database transaction.</summary>
public sealed class FakeDocumentDispositionTransaction : IDocumentDispositionTransaction
{
    /// <summary>Gets the lock names of every executed unit, in order.</summary>
    public Collection<IReadOnlyList<string>> ExecutedLockNames { get; } = [];

    /// <inheritdoc />
    public async Task<T> Execute<T>(IReadOnlyList<string> lockNames, Func<Task<T>> unit, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ExecutedLockNames.Add(lockNames);
        return await unit();
    }
}
