using PandaBear.DispositionHold;

namespace ViajantesTurismo.Admin.Application.Documents.Holds;

/// <summary>
/// Answers, for a purge, which candidate document lineages a hold restrains. It makes exactly one guard check per
/// purge with every candidate scope and relies on the caller's open read committed transaction; a failure to decide
/// surfaces as <see cref="DispositionHoldStateUnknownException" /> and is never reported as a clear result.
/// </summary>
public sealed class DocumentHoldGate(
    IDispositionHoldGuard guard,
    IDispositionHoldStore store,
    TimeProvider timeProvider)
{
    /// <summary>Returns the lineages restrained by a hold at the current time.</summary>
    /// <param name="lineageIds">Every candidate lineage of the purge.</param>
    /// <param name="ct">A token that can cancel the operation.</param>
    /// <returns>The restrained lineage identifiers.</returns>
    /// <exception cref="DispositionHoldStateUnknownException">Thrown when hold state cannot be determined.</exception>
    public async Task<IReadOnlySet<Guid>> FindHeldLineages(IReadOnlyCollection<Guid> lineageIds, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(lineageIds);

        var scopes = lineageIds.Distinct().Select(DocumentHoldScopes.ForLineage).ToArray();
        if (scopes.Length == 0)
        {
            return new HashSet<Guid>();
        }

        var decision = await guard.Check(scopes, timeProvider.GetUtcNow(), ct);
        var held = new HashSet<Guid>();
        foreach (var holdId in decision.HoldIds)
        {
            var hold = await store.Find(holdId, ct);
            if (hold is null || !DocumentHoldScopes.TryGetLineageId(hold.Scope, out var lineageId))
            {
                throw new DispositionHoldStateUnknownException("A restraining hold could not be resolved to a document lineage.");
            }

            _ = held.Add(lineageId);
        }

        return held;
    }
}
