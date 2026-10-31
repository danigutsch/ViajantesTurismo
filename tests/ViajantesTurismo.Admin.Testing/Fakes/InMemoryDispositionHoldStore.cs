using System.Collections.ObjectModel;
using PandaBear.DispositionHold;

namespace ViajantesTurismo.Admin.Testing.Fakes;

/// <summary>An in-memory hold store; holds are visible at once, with no locking.</summary>
public sealed class InMemoryDispositionHoldStore : IDispositionHoldStore
{
    /// <summary>Gets the stored holds.</summary>
    public Collection<Hold> Holds { get; } = [];

    /// <inheritdoc />
    public ValueTask Add(Hold hold, CancellationToken ct)
    {
        Holds.Add(hold);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<Hold?> Find(Guid id, CancellationToken ct) =>
        ValueTask.FromResult(Holds.FirstOrDefault(hold => hold.Id == id));
}
