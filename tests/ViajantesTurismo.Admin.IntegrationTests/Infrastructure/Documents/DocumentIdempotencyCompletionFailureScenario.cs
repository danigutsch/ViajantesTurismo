using Npgsql;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure.Documents;

internal sealed class DocumentIdempotencyCompletionFailureScenario : IAsyncDisposable
{
    private readonly NpgsqlDataSource dataSource;
    private readonly string scope;
    private readonly Guid idempotencyKey;
    private readonly bool failOnceWithRetryableError;
    private readonly string triggerName;
    private readonly string functionName;
    private readonly string sequenceName;

    private DocumentIdempotencyCompletionFailureScenario(
        NpgsqlDataSource dataSource,
        string scope,
        Guid idempotencyKey,
        bool failOnceWithRetryableError)
    {
        this.dataSource = dataSource;
        this.scope = scope;
        this.idempotencyKey = idempotencyKey;
        this.failOnceWithRetryableError = failOnceWithRetryableError;
        var suffix = Guid.CreateVersion7().ToString("N");
        triggerName = $"document_idem_failure_trigger_{suffix}";
        functionName = $"document_idem_failure_function_{suffix}";
        sequenceName = $"doc_idem_failure_sequence_{suffix}";
    }

    public static async Task<DocumentIdempotencyCompletionFailureScenario> Create(
        string connectionString,
        string scope,
        Guid idempotencyKey,
        bool failOnceWithRetryableError,
        CancellationToken ct)
    {
        var dataSource = NpgsqlDataSource.Create(connectionString);
        var scenario = new DocumentIdempotencyCompletionFailureScenario(
            dataSource,
            scope,
            idempotencyKey,
            failOnceWithRetryableError);

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

    internal async Task<string?> GetIdempotencyState(CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT "State"
            FROM "messaging"."idempotency_keys"
            WHERE "Scope" = @scope
              AND "Key" = @key;
            """);
        command.Parameters.AddWithValue("scope", scope);
        command.Parameters.AddWithValue("key", idempotencyKey.ToString("N"));
        var state = await command.ExecuteScalarAsync(ct);
        return state?.ToString();
    }

    public async ValueTask DisposeAsync()
    {
        await using var command = dataSource.CreateCommand(
            $"""
            DROP TRIGGER IF EXISTS "{triggerName}" ON "messaging"."idempotency_keys";
            DROP FUNCTION IF EXISTS "{functionName}"();
            DROP SEQUENCE IF EXISTS "{sequenceName}";
            """);
        _ = await command.ExecuteNonQueryAsync(CancellationToken.None);
        await dataSource.DisposeAsync();
    }

    private async Task Install(CancellationToken ct)
    {
        var sequenceStatement = failOnceWithRetryableError
            ? $"CREATE SEQUENCE \"{sequenceName}\" START WITH 1;"
            : string.Empty;
        var failureStatement = failOnceWithRetryableError
            ? $"""
                IF OLD."Key" = '{idempotencyKey:N}'
                   AND OLD."State" = 'Started'
                   AND NEW."State" = 'Completed'
                   AND nextval('"{sequenceName}"') = 1 THEN
                    RAISE EXCEPTION USING
                        ERRCODE = '40001',
                        MESSAGE = 'test-owned retryable idempotency completion failure';
                END IF;
                """
            : $"""
                IF OLD."Key" = '{idempotencyKey:N}'
                   AND OLD."State" = 'Started'
                   AND NEW."State" = 'Completed' THEN
                    RAISE EXCEPTION 'test-owned idempotency completion failure';
                END IF;
                """;
        await using var command = dataSource.CreateCommand(
            $"""
            {sequenceStatement}

            CREATE FUNCTION "{functionName}"()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
            {failureStatement}

                RETURN NEW;
            END;
            $$;

            CREATE TRIGGER "{triggerName}"
            BEFORE UPDATE OF "State" ON "messaging"."idempotency_keys"
            FOR EACH ROW
            EXECUTE FUNCTION "{functionName}"();
            """);
        _ = await command.ExecuteNonQueryAsync(ct);
    }
}
