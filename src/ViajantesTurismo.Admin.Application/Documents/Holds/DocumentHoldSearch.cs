using PandaBear.DispositionHold;

namespace ViajantesTurismo.Admin.Application.Documents.Holds;

/// <summary>
/// Describes a cross-document hold search as of one instant. It owns the filter and the order so that every
/// <see cref="IDocumentHoldQuery" /> implementation selects the same holds.
/// </summary>
/// <param name="State">Keeps only holds in this state, or every hold when <see langword="null" />.</param>
/// <param name="Basis">Keeps only holds with this basis, or every basis when <see langword="null" />.</param>
/// <param name="ReleasedSince">Keeps only holds released at or after this instant, or ignores release time when <see langword="null" />.</param>
/// <param name="Now">The instant against which a review date is overdue. The Active, OverdueReview and Released states are disjoint.</param>
public sealed record DocumentHoldSearch(DocumentHoldState? State, HoldBasis? Basis, DateTimeOffset? ReleasedSince, DateTimeOffset Now)
{
    /// <summary>Applies the filters and the stable order, oldest placement first and then by identifier.</summary>
    /// <param name="holds">The holds to search.</param>
    /// <returns>The matching holds in a stable order.</returns>
    public IQueryable<Hold> Apply(IQueryable<Hold> holds)
    {
        ArgumentNullException.ThrowIfNull(holds);

        var now = Now;
        var filtered = State switch
        {
            DocumentHoldState.Active => holds.Where(hold => hold.ReleasedAt == null && hold.ReviewDueAt >= now),
            DocumentHoldState.OverdueReview => holds.Where(hold => hold.ReleasedAt == null && hold.ReviewDueAt < now),
            DocumentHoldState.Released => holds.Where(hold => hold.ReleasedAt != null),
            _ => holds,
        };

        if (Basis is { } basis)
        {
            filtered = filtered.Where(hold => hold.Basis == basis);
        }

        if (ReleasedSince is { } since)
        {
            filtered = filtered.Where(hold => hold.ReleasedAt >= since);
        }

        return filtered.OrderBy(hold => hold.PlacedAt).ThenBy(hold => hold.Id);
    }
}
