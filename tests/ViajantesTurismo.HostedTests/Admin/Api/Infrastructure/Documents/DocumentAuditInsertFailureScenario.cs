using Npgsql;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure.Documents;

/// <summary>Installs a database trigger that rejects document audit inserts for one booking, and removes it on dispose.</summary>
internal sealed class DocumentAuditInsertFailureScenario : IAsyncDisposable
{
    private readonly NpgsqlDataSource dataSource;
    private readonly Guid bookingId;
    private readonly string triggerName;
    private readonly string functionName;

    private DocumentAuditInsertFailureScenario(NpgsqlDataSource dataSource, Guid bookingId)
    {
        this.dataSource = dataSource;
        this.bookingId = bookingId;
        var suffix = Guid.CreateVersion7().ToString("N");
        triggerName = $"document_audit_insert_failure_trigger_{suffix}";
        functionName = $"document_audit_insert_failure_function_{suffix}";
    }

    /// <summary>Installs the failing trigger through a new data source for <paramref name="bookingId"/>.</summary>
    public static async Task<DocumentAuditInsertFailureScenario> Create(
        string connectionString,
        Guid bookingId,
        CancellationToken ct)
    {
        var dataSource = NpgsqlDataSource.Create(connectionString);
        var scenario = new DocumentAuditInsertFailureScenario(dataSource, bookingId);

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

    /// <summary>Drops the trigger and function and disposes the data source.</summary>
    public async ValueTask DisposeAsync()
    {
        await using var command = dataSource.CreateCommand(
            $"""
            DROP TRIGGER IF EXISTS "{triggerName}" ON "DocumentAuditRecords";
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
                IF NEW."BookingId" = '{bookingId:D}'::uuid THEN
                    RAISE EXCEPTION 'test-owned audit insert failure';
                END IF;

                RETURN NEW;
            END;
            $$;

            CREATE TRIGGER "{triggerName}"
            BEFORE INSERT ON "DocumentAuditRecords"
            FOR EACH ROW
            EXECUTE FUNCTION "{functionName}"();
            """);
        _ = await command.ExecuteNonQueryAsync(ct);
    }
}
