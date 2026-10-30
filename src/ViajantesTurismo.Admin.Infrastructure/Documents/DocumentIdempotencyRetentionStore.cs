using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;
using ViajantesTurismo.Admin.Application.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Entity Framework cleanup of document idempotency entries whose replay window elapsed.</summary>
internal sealed class DocumentIdempotencyRetentionStore(AdminWriteDbContext dbContext) : IDocumentIdempotencyRetentionStore
{
    private const string IdempotencyEntityTypeName = "IdempotencyEntryEntity";
    private const int PurgeBatchSize = 500;

    private static readonly CompositeFormat DeleteExpiredSql = CompositeFormat.Parse(
        """
        DELETE FROM {0}
        WHERE ({1}, {2}) IN (
            SELECT {1}, {2} FROM {0}
            WHERE starts_with({1}, @prefix) AND COALESCE({4}, {3}) <= @cutoff
            ORDER BY {3}
            LIMIT {5})
        """);

    /// <summary>
    /// Deletes, in batches, every document-scoped entry completed, or never completed and started, at or before
    /// <paramref name="cutoff" />. Table and column names come from the EF model, never from input, and values are
    /// bound as parameters. Once legal hold exists, held documents' entries must be excluded here.
    /// </summary>
    public async Task<int> PurgeExpiredEntries(DateTimeOffset cutoff, CancellationToken ct)
    {
        var entityType = dbContext.Model.GetEntityTypes().Single(type => type.ClrType.Name == IdempotencyEntityTypeName);
        var table = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());
        var tableName = QuoteTable(entityType.GetSchema(), entityType.GetTableName()!);
        var scope = Column(entityType, table, "Scope");
        var key = Column(entityType, table, "Key");
        var startedAt = Column(entityType, table, "StartedAt");
        var completedAt = Column(entityType, table, "CompletedAt");

        var sql = string.Format(
            CultureInfo.InvariantCulture,
            DeleteExpiredSql,
            tableName,
            scope,
            key,
            startedAt,
            completedAt,
            PurgeBatchSize);

        var total = 0;
        int batchCount;
        do
        {
            batchCount = await dbContext.Database.ExecuteSqlRawAsync(
                sql,
                [
                    new NpgsqlParameter("prefix", DocumentCommandIdempotency.ScopePrefix),
                    new NpgsqlParameter("cutoff", cutoff),
                ],
                ct);
            total += batchCount;
        }
        while (batchCount == PurgeBatchSize);

        return total;
    }

    private static string Column(IEntityType entityType, StoreObjectIdentifier table, string propertyName) =>
        $"\"{entityType.FindProperty(propertyName)!.GetColumnName(table)}\"";

    private static string QuoteTable(string? schema, string name) =>
        schema is null ? $"\"{name}\"" : $"\"{schema}\".\"{name}\"";
}
