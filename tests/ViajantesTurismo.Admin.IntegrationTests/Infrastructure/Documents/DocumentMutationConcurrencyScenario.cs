using Npgsql;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure.Documents;

internal sealed class DocumentMutationConcurrencyScenario : IAsyncDisposable
{
    private readonly NpgsqlDataSource dataSource;
    private readonly Guid documentId;
    private readonly string triggerName;
    private readonly string functionName;

    private DocumentMutationConcurrencyScenario(NpgsqlDataSource dataSource, Guid documentId)
    {
        this.dataSource = dataSource;
        this.documentId = documentId;
        var suffix = Guid.CreateVersion7().ToString("N");
        triggerName = $"document_mutation_concurrency_trigger_{suffix}";
        functionName = $"document_mutation_concurrency_function_{suffix}";
    }

    public static async Task<DocumentMutationConcurrencyScenario> Create(
        string connectionString,
        Guid documentId,
        CancellationToken ct)
    {
        var dataSource = NpgsqlDataSource.Create(connectionString);
        var scenario = new DocumentMutationConcurrencyScenario(dataSource, documentId);

        try
        {
            await scenario.Install(ct);
            return scenario;
        }
        catch
        {
            await dataSource.DisposeAsync();
            throw;
        }
    }

    public Task<IReadOnlyList<DocumentAuditEntry>> GetDocumentAuditMetadata(CancellationToken ct) =>
        DocumentAuditMetadataReader.ReadByDocumentId(dataSource, documentId, ct);

    public async ValueTask DisposeAsync()
    {
        await using var command = dataSource.CreateCommand(
            $"""
            DROP TRIGGER IF EXISTS "{triggerName}" ON "DocumentDrafts";
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
                IF NEW."Id" = '{documentId:D}'::uuid THEN
                    RETURN NULL;
                END IF;

                RETURN NEW;
            END;
            $$;

            CREATE TRIGGER "{triggerName}"
            BEFORE UPDATE ON "DocumentDrafts"
            FOR EACH ROW
            EXECUTE FUNCTION "{functionName}"();
            """);
        _ = await command.ExecuteNonQueryAsync(ct);
    }
}
