using Microsoft.EntityFrameworkCore;
using SharedKernel.IntegrationTesting;
using ViajantesTurismo.Management.Security;

namespace ViajantesTurismo.Management.WebIntegrationTests;

internal sealed class PostgreSqlManagementSecurityPersistenceScenario : IAsyncDisposable
{
    private const string PostgreSqlResourceName = "postgres";
    private const string DatabaseResourceName = "managementsecurity";

    private readonly AspireTestApplication app;
    private readonly string connectionString;

    private PostgreSqlManagementSecurityPersistenceScenario(
        AspireTestApplication app,
        string connectionString)
    {
        this.app = app;
        this.connectionString = connectionString;
    }

    public static async ValueTask<PostgreSqlManagementSecurityPersistenceScenario> Create(CancellationToken ct)
    {
        var app = await StartApplication(ct);
        return await Initialize(app, ct);
    }

    internal static async ValueTask<AspireTestApplication> StartApplication(CancellationToken ct)
    {
        var appBuilder = AspireTestApplication.CreateBuilder();
        var databaseServer = appBuilder.AddPostgres(PostgreSqlResourceName);
        _ = databaseServer.AddDatabase(DatabaseResourceName);

        return await AspireTestApplication.Start(appBuilder, [PostgreSqlResourceName], null, ct);
    }

    internal static async ValueTask<PostgreSqlManagementSecurityPersistenceScenario> Initialize(
        AspireTestApplication app,
        CancellationToken ct)
    {
        try
        {
            var connectionString = await app.GetConnectionString(DatabaseResourceName, ct);
            var options = new DbContextOptionsBuilder<ManagementSecurityDbContext>()
                .UseNpgsql(connectionString)
                .Options;
            await using var dbContext = new ManagementSecurityDbContext(options);
            await dbContext.Database.MigrateAsync(ct);
            return new PostgreSqlManagementSecurityPersistenceScenario(app, connectionString);
        }
        catch (Exception initializationFailure)
        {
            await PostgreSqlTestCleanup.DisposeResources(initializationFailure, app);
            throw;
        }
    }

    internal static Task<string> GetConnectionString(
        AspireTestApplication app,
        CancellationToken ct) => app.GetConnectionString(DatabaseResourceName, ct);

    public ManagementSecurityPersistenceTestHost CreateHost(string? applicationName = null) =>
        ManagementSecurityPersistenceTestHost.Create(connectionString, applicationName);

    public async Task<int> GetDataProtectionKeyCount(CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*)::int FROM security.data_protection_keys;";
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct), System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<int> GetTicketCount(string ticketKey, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*)::int FROM security.management_cookie_tickets WHERE id = @ticketKey;";
        _ = command.Parameters.AddWithValue("ticketKey", ticketKey);
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct), System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<bool> TicketContainsPlaintext(
        string ticketKey,
        string principalName,
        CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT EXISTS (
                SELECT 1
                FROM security.management_cookie_tickets
                WHERE id = @ticketKey
                  AND position(convert_to(@principalName, 'UTF8') IN value) > 0);
            """;
        _ = command.Parameters.AddWithValue("ticketKey", ticketKey);
        _ = command.Parameters.AddWithValue("principalName", principalName);
        return (bool)(await command.ExecuteScalarAsync(ct) ?? false);
    }

    public ValueTask DisposeAsync() => app.DisposeAsync();
}
