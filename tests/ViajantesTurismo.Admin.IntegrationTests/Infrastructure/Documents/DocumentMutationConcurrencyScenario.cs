using Npgsql;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure.Documents;

internal sealed class DocumentMutationConcurrencyScenario : IAsyncDisposable
{
    private readonly NpgsqlDataSource dataSource;
    private readonly Guid documentId;
    private readonly Guid lineageId;
    private readonly string triggerName;
    private readonly string functionName;

    private DocumentMutationConcurrencyScenario(NpgsqlDataSource dataSource, Guid documentId, Guid lineageId)
    {
        this.dataSource = dataSource;
        this.documentId = documentId;
        this.lineageId = lineageId;
        var suffix = Guid.CreateVersion7().ToString("N");
        triggerName = $"document_mutation_concurrency_trigger_{suffix}";
        functionName = $"document_mutation_concurrency_function_{suffix}";
    }

    public static async Task<DocumentMutationConcurrencyScenario> Create(
        string connectionString,
        Guid documentId,
        CancellationToken ct)
    {
        Guid lineageId;
        await using (var lookupConnection = new NpgsqlConnection(connectionString))
        {
            await lookupConnection.OpenAsync(ct);
            await using var command = new NpgsqlCommand(
                "SELECT \"DocumentLineageId\" FROM \"DocumentDrafts\" WHERE \"Id\" = @documentId;");
            command.Connection = lookupConnection;
            command.Parameters.AddWithValue("documentId", documentId);
            var lineageIdValue = await command.ExecuteScalarAsync(ct);
            if (lineageIdValue is not Guid resolvedLineageId)
            {
                throw new InvalidOperationException("The document mutation scenario requires an existing document lineage.");
            }

            lineageId = resolvedLineageId;
        }

        var dataSource = NpgsqlDataSource.Create(connectionString);
        var scenario = new DocumentMutationConcurrencyScenario(dataSource, documentId, lineageId);
        try
        {
            await scenario.Install(ct);
            return scenario;
        }
        catch
        {
            await scenario.DisposeAsync();
            throw;
        }
    }

    public Task<IReadOnlyList<DocumentAuditEntry>> GetDocumentAuditMetadata(CancellationToken ct) =>
        DocumentAuditMetadataReader.ReadByDocumentId(dataSource, documentId, ct);

    public async ValueTask DisposeAsync()
    {
        await using var command = dataSource.CreateCommand(
            $"""
            DROP TRIGGER IF EXISTS "{triggerName}" ON "DocumentLineages";
            DROP FUNCTION IF EXISTS "{functionName}"();
            """);
        _ = await command.ExecuteNonQueryAsync(CancellationToken.None);
        await dataSource.DisposeAsync();
    }

    private async Task Install(CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand(
            $"""
            CREATE FUNCTION "{functionName}"()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                IF NEW."Id" = '{lineageId:D}'::uuid THEN
                    RETURN NULL;
                END IF;

                RETURN NEW;
            END;
            $$;

            CREATE TRIGGER "{triggerName}"
            BEFORE UPDATE ON "DocumentLineages"
            FOR EACH ROW
            EXECUTE FUNCTION "{functionName}"();
            """);
        _ = await command.ExecuteNonQueryAsync(ct);
    }
}
