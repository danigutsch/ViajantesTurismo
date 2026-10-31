using System.Collections.ObjectModel;
using PandaBear.DispositionHold;

namespace ViajantesTurismo.Admin.Testing.Fakes;

/// <summary>A configurable guard that reports given holds as restraining, or fails as an unknown state.</summary>
public sealed class FakeDispositionHoldGuard : IDispositionHoldGuard
{
    /// <summary>Gets or sets the hold ids reported as restraining; empty means clear.</summary>
    public Collection<Guid> RestrainingHoldIds { get; } = [];

    /// <summary>Gets or sets the failure thrown by every check, or null.</summary>
    public Exception? Failure
    {
        get; set;
    }

    /// <summary>Gets the scopes of every check, one entry per call.</summary>
    public Collection<IReadOnlyCollection<DispositionScope>> CheckedScopes { get; } = [];

    /// <inheritdoc />
    public ValueTask<DispositionHoldDecision> Check(IReadOnlyCollection<DispositionScope> scopes, DateTimeOffset at, CancellationToken ct)
    {
        CheckedScopes.Add(scopes);
        if (Failure is not null)
        {
            throw Failure;
        }

        return ValueTask.FromResult(
            RestrainingHoldIds.Count == 0 ? DispositionHoldDecision.Clear : DispositionHoldDecision.HeldBy([.. RestrainingHoldIds]));
    }
}
