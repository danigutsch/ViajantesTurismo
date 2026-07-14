using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.IntegrationTesting;
using ViajantesTurismo.Branding.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Branding;

public sealed class BrandingPostgreSqlMigrationScenario(ApiFixture fixture) : IAsyncLifetime
{
    private const string BrandingMigrationsHistoryTable = "__EFMigrationsHistory_Branding";
    private const string BrandingSettingsTableExistsCommandText = "SELECT to_regclass('branding.\"BrandingSettings\"') IS NOT NULL;";
    private const string PublicBrandingSettingsTableExistsCommandText = "SELECT to_regclass('public.\"BrandingSettings\"') IS NOT NULL;";
    private const string ResetBrandingTablesCommandText = """
                                                       DO $$
                                                       DECLARE
                                                           tables_to_truncate text;
                                                       BEGIN
                                                           SELECT string_agg(format('%I.%I', schemaname, tablename), ', ')
                                                           INTO tables_to_truncate
                                                           FROM pg_catalog.pg_tables
                                                           WHERE schemaname = 'branding'
                                                             AND tablename NOT LIKE '__EFMigrationsHistory%';

                                                           IF tables_to_truncate IS NOT NULL THEN
                                                               EXECUTE 'TRUNCATE TABLE ' || tables_to_truncate || ' RESTART IDENTITY CASCADE';
                                                           END IF;
                                                       END $$;
                                                       """;
    private PostgreSqlTestDatabase? _database;
    private NpgsqlDataSource? _dataSource;

    public async ValueTask InitializeAsync()
    {
        _database = await fixture.CreateIsolatedPostgreSqlDatabase(TestContext.Current.CancellationToken);
        _dataSource = _database.CreateDataSource("branding-migration-test");
    }

    public BrandingDbContext CreateDbContext()
    {
        var dataSource = _dataSource ?? throw new InvalidOperationException("The Branding PostgreSQL migration scenario has not started.");
        var options = new DbContextOptionsBuilder<BrandingDbContext>()
            .UseNpgsql(dataSource, providerOptions => providerOptions.MigrationsHistoryTable(BrandingMigrationsHistoryTable, schema: "public"))
            .Options;

        return new BrandingDbContext(options);
    }

    public async Task ApplyMigrations(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(ct);
    }

    public async Task ResetSchemas(CancellationToken ct)
    {
        var dataSource = _dataSource ?? throw new InvalidOperationException("The Branding PostgreSQL migration scenario has not started.");
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await PostgreSqlPublicSchemaReset.Reset(connection, ct);
        await ResetBrandingTables(connection, ct);
    }

    public Task<bool> BrandingSettingsTableExists(CancellationToken ct)
    {
        return BrandingSettingsTableExistsCore(ct);
    }

    public Task<bool> PublicBrandingSettingsTableExists(CancellationToken ct)
    {
        return PublicBrandingSettingsTableExistsCore(ct);
    }

    public async Task<string[]> GetMigrationHistory(CancellationToken ct)
    {
        var dataSource = _dataSource ?? throw new InvalidOperationException("The Branding PostgreSQL migration scenario has not started.");
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT \"MigrationId\" FROM public.\"__EFMigrationsHistory_Branding\" ORDER BY \"MigrationId\";";
        await using var reader = await command.ExecuteReaderAsync(ct);
        var migrationHistory = new List<string>();
        while (await reader.ReadAsync(ct))
        {
            migrationHistory.Add(reader.GetString(0));
        }

        return [.. migrationHistory];
    }

    public async ValueTask DisposeAsync()
    {
        var dataSource = _dataSource;
        var database = _database;
        _dataSource = null;
        _database = null;

        if (dataSource is not null)
        {
            await dataSource.DisposeAsync();
        }

        if (database is not null)
        {
            await database.DisposeAsync();
        }
    }

    private static async Task ResetBrandingTables(NpgsqlConnection connection, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(ResetBrandingTablesCommandText, connection);
        _ = await command.ExecuteNonQueryAsync(ct);
    }

    private async Task<bool> BrandingSettingsTableExistsCore(CancellationToken ct)
    {
        var dataSource = _dataSource ?? throw new InvalidOperationException("The Branding PostgreSQL migration scenario has not started.");
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = BrandingSettingsTableExistsCommandText;
        var result = await command.ExecuteScalarAsync(ct);
        return result is true;
    }

    private async Task<bool> PublicBrandingSettingsTableExistsCore(CancellationToken ct)
    {
        var dataSource = _dataSource ?? throw new InvalidOperationException("The Branding PostgreSQL migration scenario has not started.");
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = PublicBrandingSettingsTableExistsCommandText;
        var result = await command.ExecuteScalarAsync(ct);
        return result is true;
    }
}
