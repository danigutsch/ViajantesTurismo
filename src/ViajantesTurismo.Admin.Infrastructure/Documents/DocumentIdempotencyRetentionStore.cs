using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Idempotency;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure.DispositionHolds;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>
/// Purges document idempotency entries at or before the cutoff through the Admin write context's retention provider.
/// The recorded lineage owner governs holds after revisions are gone, and unattributed document entries are retained.
/// </summary>
internal sealed class DocumentIdempotencyRetentionStore(
    AdminWriteDbContext dbContext,
    [FromKeyedServices(typeof(AdminWriteDbContext))] IIdempotencyRetentionStore retentionStore) : IDocumentIdempotencyRetentionStore
{
    private const int PurgeBatchSize = 500;

    /// <summary>Purges a bounded batch while checking the selected owners in the existing transaction.</summary>
    public async Task<DocumentPurgeCounts> PurgeExpiredEntries(DateTimeOffset cutoff, HeldDocumentLineages heldLineages, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(heldLineages);

        var activeOwners = (await ActiveHoldLineages.Find(dbContext, ct))
            .OfType<Guid>()
            .Select(static id => id.ToString("N"))
            .ToArray();
        var result = await retentionStore.PurgeExpiredThrough(
            cutoff,
            DocumentCommandIdempotency.ScopePrefix,
            activeOwners,
            PurgeBatchSize,
            ct,
            scopePrefix: DocumentCommandIdempotency.ScopePrefix,
            guard: CreateHoldGuard(heldLineages));

        return new DocumentPurgeCounts(checked((int)result.Purged), checked((int)result.Held), checked((int)result.Unattributed));
    }

    private static Func<IReadOnlyCollection<string>, CancellationToken, ValueTask<IReadOnlySet<string>>> CreateHoldGuard(
        HeldDocumentLineages heldLineages) =>
        async (owners, ct) =>
        {
            var lineages = owners.Select(ParseLineage).Distinct().ToArray();
            var held = await heldLineages(lineages, ct);
            return owners.Where(owner => held.Contains(ParseLineage(owner))).ToHashSet(StringComparer.Ordinal);
        };

    private static Guid ParseLineage(string retentionOwner) =>
        Guid.TryParseExact(retentionOwner, "N", out var lineageId) ? lineageId : Guid.Empty;
}
