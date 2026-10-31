using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;
using NpgsqlTypes;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Entity Framework cleanup of document idempotency entries whose replay window elapsed.</summary>
internal sealed class DocumentIdempotencyRetentionStore(AdminWriteDbContext dbContext) : IDocumentIdempotencyRetentionStore
{
    private const string IdempotencyEntityTypeName = "IdempotencyEntryEntity";
    private const int PurgeBatchSize = 500;
    private const string RegenerateScopeSegment = "regenerate-draft:";
    private const int GuidNLength = 32;

    private static readonly CompositeFormat SelectExpiredSql = CompositeFormat.Parse(
        """
        SELECT {1} AS "Scope", {2} AS "Key", {4} AS "ResultFingerprint" FROM {0}
        WHERE starts_with({1}, @prefix) AND COALESCE({5}, {3}) <= @cutoff
        ORDER BY {3}
        LIMIT {6}
        """);

    private static readonly CompositeFormat DeleteSql = CompositeFormat.Parse(
        """
        DELETE FROM {0}
        WHERE ({1}, {2}) IN (SELECT * FROM unnest(@scopes, @keys))
        """);

    /// <summary>
    /// Deletes one batch of document-scoped entries completed, or never completed and started, at or before
    /// <paramref name="cutoff" />, except entries whose document lineage <paramref name="heldLineages" /> reports as
    /// held. The filter is called once with every candidate lineage. Table and column names come from the EF model,
    /// never from input, and values are bound as parameters.
    /// </summary>
    public async Task<DocumentPurgeCounts> PurgeExpiredEntries(DateTimeOffset cutoff, HeldDocumentLineages heldLineages, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(heldLineages);

        var entityType = dbContext.Model.GetEntityTypes().Single(type => type.ClrType.Name == IdempotencyEntityTypeName);
        var table = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());
        var tableName = QuoteTable(entityType.GetSchema(), entityType.GetTableName()!);
        var scope = Column(entityType, table, "Scope");
        var key = Column(entityType, table, "Key");

        var selectSql = string.Format(
            CultureInfo.InvariantCulture,
            SelectExpiredSql,
            tableName,
            scope,
            key,
            Column(entityType, table, "StartedAt"),
            Column(entityType, table, "ResultFingerprint"),
            Column(entityType, table, "CompletedAt"),
            PurgeBatchSize);
        var candidates = await dbContext.Database
            .SqlQueryRaw<ExpiredEntry>(
                selectSql,
                new NpgsqlParameter("prefix", DocumentCommandIdempotency.ScopePrefix),
                new NpgsqlParameter("cutoff", cutoff))
            .ToListAsync(ct);
        if (candidates.Count == 0)
        {
            return new DocumentPurgeCounts(0, 0);
        }

        var lineageByDocument = await FindLineages(candidates, ct);
        var held = await heldLineages([.. lineageByDocument.Values.Distinct()], ct);
        var purgeable = candidates
            .Where(entry => !IsHeld(entry, lineageByDocument, held))
            .ToArray();
        if (purgeable.Length > 0)
        {
            var deleteSql = string.Format(CultureInfo.InvariantCulture, DeleteSql, tableName, scope, key);
            _ = await dbContext.Database.ExecuteSqlRawAsync(
                deleteSql,
                [
                    new NpgsqlParameter("scopes", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = purgeable.Select(entry => entry.Scope).ToArray() },
                    new NpgsqlParameter("keys", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = purgeable.Select(entry => entry.Key).ToArray() },
                ],
                ct);
        }

        return new DocumentPurgeCounts(purgeable.Length, candidates.Count - purgeable.Length);
    }

    private async Task<Dictionary<Guid, Guid>> FindLineages(List<ExpiredEntry> candidates, CancellationToken ct)
    {
        var documentIds = candidates.SelectMany(DocumentIds).Distinct().ToArray();
        return await dbContext.DocumentDrafts
            .AsNoTracking()
            .Where(draft => documentIds.Contains(draft.Id))
            .Select(draft => new { draft.Id, draft.DocumentLineageId })
            .ToDictionaryAsync(draft => draft.Id, draft => draft.DocumentLineageId, ct);
    }

    private static bool IsHeld(ExpiredEntry entry, Dictionary<Guid, Guid> lineageByDocument, IReadOnlySet<Guid> held) =>
        DocumentIds(entry).Any(id => lineageByDocument.TryGetValue(id, out var lineageId) && held.Contains(lineageId));

    private static IEnumerable<Guid> DocumentIds(ExpiredEntry entry)
    {
        var scopeIndex = entry.Scope.IndexOf(RegenerateScopeSegment, StringComparison.Ordinal);
        if (scopeIndex >= 0
            && Guid.TryParseExact(entry.Scope.AsSpan(scopeIndex + RegenerateScopeSegment.Length), "N", out var regeneratedId))
        {
            yield return regeneratedId;
        }

        if (entry.ResultFingerprint is { Length: > GuidNLength } fingerprint
            && Guid.TryParseExact(fingerprint.AsSpan(fingerprint.Length - GuidNLength), "N", out var resultId))
        {
            yield return resultId;
        }
    }

    private static string Column(IEntityType entityType, StoreObjectIdentifier table, string propertyName) =>
        $"\"{entityType.FindProperty(propertyName)!.GetColumnName(table)}\"";

    private static string QuoteTable(string? schema, string name) =>
        schema is null ? $"\"{name}\"" : $"\"{schema}\".\"{name}\"";

    /// <summary>The identifying columns of one expired entry; no request content.</summary>
    /// <param name="Scope">The idempotency scope.</param>
    /// <param name="Key">The digest of the caller key.</param>
    /// <param name="ResultFingerprint">The stored result fingerprint, or null when never completed.</param>
    internal sealed record ExpiredEntry(string Scope, string Key, string? ResultFingerprint);
}
