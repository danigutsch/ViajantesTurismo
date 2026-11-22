using ViajantesTurismo.Admin.Application;

namespace ViajantesTurismo.Admin.Testing.Fakes;

/// <summary>Runs a unit directly for tests with in-memory stores.</summary>
public sealed class FakeReadCommittedTransaction : IReadCommittedTransaction
{
    /// <inheritdoc />
    public Task<T> Execute<T>(Func<Task<T>> unit, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return unit();
    }
}
