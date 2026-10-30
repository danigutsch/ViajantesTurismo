using PostgreSqlTestCleanup = ViajantesTurismo.HostedTests.Infrastructure.HostedTestCleanup;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using PandaBear.IntegrationTesting;
using ViajantesTurismo.Management.Security;

namespace ViajantesTurismo.HostedTests.Management.Security;

/// <summary>Provisions a PostgreSQL security database through Aspire, migrates it, and queries persisted security data.</summary>
internal sealed class PostgreSqlManagementSecurityPersistenceScenario : IAsyncDisposable
{
    private const string DatabaseResourceName = "managementsecurity";

    private readonly SecurityPersistenceApplication app;
    private readonly string connectionString;

    private PostgreSqlManagementSecurityPersistenceScenario(
        SecurityPersistenceApplication app,
        string connectionString)
    {
        this.app = app;
        this.connectionString = connectionString;
    }

    /// <summary>Starts the application and returns an initialized scenario.</summary>
    public static async ValueTask<PostgreSqlManagementSecurityPersistenceScenario> Create(HostedTestFixture shared, CancellationToken ct)
    {
        var app = await StartApplication(shared, ct);
        return await Initialize(app, ct);
    }

    /// <summary>Creates a database lease and starts an Aspire test application wired to it, cleaning up on failure.</summary>
    internal static async ValueTask<SecurityPersistenceApplication> StartApplication(HostedTestFixture shared, CancellationToken ct)
    {
        var database = await shared.Databases.Create(ct);
        var builder = AspireTestApplication.CreateBuilder();
        builder.Configuration[$"ConnectionStrings:{DatabaseResourceName}"] = database.ConnectionString;
        _ = builder.AddConnectionString(DatabaseResourceName);
        try
        {
            var application = await AspireTestApplication.Start(builder, [], null, ct);
            return new SecurityPersistenceApplication(application, database);
        }
        catch (Exception failure)
        {
            await PostgreSqlTestCleanup.DisposeResources(failure, database);
            throw;
        }
    }

    /// <summary>Migrates the security database and returns the scenario, disposing the application on failure.</summary>
    internal static async ValueTask<PostgreSqlManagementSecurityPersistenceScenario> Initialize(
        SecurityPersistenceApplication app,
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

    /// <summary>Returns the connection string of the security database resource.</summary>
    internal static Task<string> GetConnectionString(
        SecurityPersistenceApplication app,
        CancellationToken ct) => app.GetConnectionString(DatabaseResourceName, ct);

    /// <summary>Creates a persistence test host connected to the scenario database.</summary>
    public ManagementSecurityPersistenceTestHost CreateHost(string? applicationName = null) =>
        ManagementSecurityPersistenceTestHost.Create(connectionString, applicationName);

    /// <summary>Returns the number of stored data protection keys.</summary>
    public async Task<int> GetDataProtectionKeyCount(CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*)::int FROM security.data_protection_keys;";
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct), System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Returns how many tickets are stored under the given key.</summary>
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

    /// <summary>Returns whether the stored ticket bytes contain the principal name in plaintext.</summary>
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

    /// <summary>Disposes the application and database.</summary>
    public ValueTask DisposeAsync() => app.DisposeAsync();
}
