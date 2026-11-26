using Npgsql;
using SharedKernel.Npgsql;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Reads integrity outcomes for retained Admin document artifacts.</summary>
public static class FinalizedArtifactIntegrityReader
{
    private const string SchemaName = "public";

    private static readonly PostgreSqlArtifactIntegrityMapping Mapping = new(
        SchemaName,
        nameof(AdminWriteDbContext.DocumentDrafts),
        nameof(DocumentDraft.Id),
        nameof(DocumentDraft.FinalizedAt),
        nameof(DocumentDraft.FinalizedArtifactLength),
        nameof(DocumentDraft.FinalizedArtifactSha256),
        SchemaName,
        DocumentArtifactBodyConfiguration.TableName,
        nameof(DocumentArtifactBody.DocumentId),
        DocumentArtifactBodyConfiguration.ContentColumn);

    /// <summary>Reads one page of opaque revision IDs and integrity outcomes using the caller's PostgreSQL identity.</summary>
    /// <param name="dataSource">The data source for the authorized Admin operator.</param>
    /// <param name="afterId">The exclusive revision cursor, or null for the first page.</param>
    /// <param name="pageSize">The maximum number of rows in the page, between 1 and 100.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The integrity outcomes in PostgreSQL UUID order.</returns>
    public static Task<IReadOnlyList<PostgreSqlArtifactIntegrityResult>> ReadPage(
        NpgsqlDataSource dataSource, Guid? afterId, int pageSize, CancellationToken ct) =>
        PostgreSqlArtifactIntegrity.ReadPage(dataSource, Mapping, afterId, pageSize, ct);
}
