using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;
using NpgsqlTypes;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure.DispositionHolds;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>
/// Entity Framework cleanup of document idempotency entries whose replay window elapsed. An entry belongs to the document
/// lineage recorded as its retention owner when the entry started, so a hold keeps it after the lineage's revisions were
/// purged. An expired document entry without an owner is retained and counted, never purged.
/// </summary>
internal sealed class DocumentIdempotencyRetentionStore(AdminWriteDbContext dbContext) : IDocumentIdempotencyRetentionStore
{
    private const string IdempotencyEntityTypeName = "IdempotencyEntryEntity";
    private const int PurgeBatchSize = 500;

    private static readonly CompositeFormat CandidatesSql = CompositeFormat.Parse(
        """
        SELECT {1} AS "Scope", {2} AS "Key" FROM {0}
        WHERE starts_with({1}, @prefix) AND COALESCE({3}, {4}) <= @cutoff AND {5} IS NOT NULL AND NOT ({5} = ANY(@heldOwners))
        ORDER BY COALESCE({3}, {4}), {1}, {2}
        LIMIT {6}
        """);

    private static readonly CompositeFormat CountSql = CompositeFormat.Parse(
        """
        SELECT count(*)::int AS "Value" FROM {0}
        WHERE starts_with({1}, @prefix) AND COALESCE({3}, {4}) <= @cutoff AND ({5})
        """);

    private static readonly CompositeFormat DeleteSql = CompositeFormat.Parse(
        """
        DELETE FROM {0}
        WHERE ({1}, {2}) IN (SELECT * FROM unnest(@scopes, @keys))
        """);

    /// <summary>
    /// Deletes one batch of document-scoped entries completed, or never completed and started, at or before
    /// <paramref name="cutoff" />, except entries owned by a lineage that <paramref name="heldLineages" /> reports as held
    /// and entries without an owner. The filter is called once with the lineages of every effective hold. Table and column
    /// names come from the EF model, never from input, and values are bound as parameters.
    /// </summary>
    public async Task<DocumentPurgeCounts> PurgeExpiredEntries(DateTimeOffset cutoff, HeldDocumentLineages heldLineages, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(heldLineages);

        var entityType = dbContext.Model.GetEntityTypes().Single(type => type.ClrType.Name == IdempotencyEntityTypeName);
        var table = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());
        var tableName = QuoteTable(entityType.GetSchema(), entityType.GetTableName()!);
        var scope = Column(entityType, table, "Scope");
        var key = Column(entityType, table, "Key");
        var owner = Column(entityType, table, "RetentionOwner");

        var activeLineages = (await ActiveHoldLineages.Find(dbContext, ct)).OfType<Guid>().ToArray();
        var held = await heldLineages(activeLineages, ct);
        var heldOwners = held.Select(id => id.ToString("N")).ToArray();

        var heldCount = await Count(tableName, scope, table, entityType, $"{owner} = ANY(@heldOwners)", cutoff, heldOwners, ct);
        var unattributedCount = await Count(tableName, scope, table, entityType, $"{owner} IS NULL", cutoff, heldOwners, ct);
        var candidates = await dbContext.Database
            .SqlQueryRaw<ExpiredEntry>(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CandidatesSql,
                    tableName,
                    scope,
                    key,
                    Column(entityType, table, "CompletedAt"),
                    Column(entityType, table, "StartedAt"),
                    owner,
                    PurgeBatchSize),
                ExpiryParameters(cutoff, heldOwners))
            .ToListAsync(ct);

        if (candidates.Count > 0)
        {
            _ = await dbContext.Database.ExecuteSqlRawAsync(
                string.Format(CultureInfo.InvariantCulture, DeleteSql, tableName, scope, key),
                [
                    new NpgsqlParameter("scopes", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = candidates.Select(entry => entry.Scope).ToArray() },
                    new NpgsqlParameter("keys", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = candidates.Select(entry => entry.Key).ToArray() },
                ],
                ct);
        }

        return new DocumentPurgeCounts(candidates.Count, heldCount, unattributedCount);
    }

    private Task<int> Count(
        string tableName,
        string scope,
        StoreObjectIdentifier table,
        IEntityType entityType,
        string ownerPredicate,
        DateTimeOffset cutoff,
        string[] heldOwners,
        CancellationToken ct) => dbContext.Database
            .SqlQueryRaw<int>(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CountSql,
                    tableName,
                    scope,
                    string.Empty,
                    Column(entityType, table, "CompletedAt"),
                    Column(entityType, table, "StartedAt"),
                    ownerPredicate),
                ExpiryParameters(cutoff, heldOwners))
            .SingleAsync(ct);

    private static NpgsqlParameter[] ExpiryParameters(DateTimeOffset cutoff, string[] heldOwners) =>
    [
        new NpgsqlParameter("prefix", DocumentCommandIdempotency.ScopePrefix),
        new NpgsqlParameter("cutoff", cutoff),
        new NpgsqlParameter("heldOwners", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = heldOwners },
    ];

    private static string Column(IEntityType entityType, StoreObjectIdentifier table, string propertyName) =>
        $"\"{entityType.FindProperty(propertyName)!.GetColumnName(table)}\"";

    private static string QuoteTable(string? schema, string name) =>
        schema is null ? $"\"{name}\"" : $"\"{schema}\".\"{name}\"";

    /// <summary>The identifying columns of one expired entry; no request content.</summary>
    /// <param name="Scope">The idempotency scope.</param>
    /// <param name="Key">The digest of the caller key.</param>
    internal sealed record ExpiredEntry(string Scope, string Key);
}
