namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>
/// Decides which of the given document lineages a disposition hold restrains. A purge calls it once with every
/// candidate lineage, inside the transaction that deletes, and must not delete anything of a returned lineage.
/// </summary>
/// <param name="lineageIds">Every lineage the purge would affect.</param>
/// <param name="ct">A token that can cancel the check.</param>
/// <returns>The restrained lineages; the call faults instead of returning when hold state cannot be determined.</returns>
public delegate Task<IReadOnlySet<Guid>> HeldDocumentLineages(IReadOnlyCollection<Guid> lineageIds, CancellationToken ct);
