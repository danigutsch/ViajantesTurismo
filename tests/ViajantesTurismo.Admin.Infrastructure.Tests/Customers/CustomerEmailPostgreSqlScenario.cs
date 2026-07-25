using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SharedKernel.Domain.EntityFrameworkCore;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.Idempotency.EntityFrameworkCore;
using SharedKernel.IntegrationTesting;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Customers;

internal sealed class CustomerEmailPostgreSqlScenario : IAsyncDisposable
{
    private const string PostgreSqlResourceName = "postgres";
    private const string DatabaseResourceName = "admin";

    private readonly AspireTestApplication app;
    private readonly string connectionString;

    private CustomerEmailPostgreSqlScenario(AspireTestApplication app, string connectionString)
    {
        this.app = app;
        this.connectionString = connectionString;
    }

    public static async ValueTask<CustomerEmailPostgreSqlScenario> Create(CancellationToken ct)
    {
        var appBuilder = AspireTestApplication.CreateBuilder();
        var databaseServer = appBuilder.AddPostgres(PostgreSqlResourceName);
        _ = databaseServer.AddDatabase(DatabaseResourceName);

        var app = await AspireTestApplication.Start(appBuilder, [PostgreSqlResourceName], null, ct);
        var connectionString = await app.GetConnectionString(DatabaseResourceName, ct);
        return new CustomerEmailPostgreSqlScenario(app, connectionString);
    }

    public async Task ApplyLatestMigration(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(ct);
    }

    public async Task CreateCustomerEmailExtension(CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "CREATE EXTENSION IF NOT EXISTS citext;";
        _ = await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> CustomerEmailExtensionExists(CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'citext');";
        return (bool)(await command.ExecuteScalarAsync(ct) ?? false);
    }

    public async Task ApplyMigration(string migration, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        var migrator = dbContext.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(migration, ct);
    }

    public async Task<Customer> AddCustomer(string email, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        var customer = EntityBuilders.BuildCustomer(new CustomerOptions(Email: email));
        dbContext.Customers.Add(customer);
        await dbContext.SaveEntities(ct);
        return customer;
    }

    public async Task<Customer?> GetByEmail(string email, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await new CustomerStore(dbContext).GetByEmail(email, ct);
    }

    public async Task<int> CountCustomers(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.Customers.CountAsync(ct);
    }

    public ValueTask DisposeAsync() => app.DisposeAsync();

    private AdminWriteDbContext CreateDbContext()
    {
        var services = new ServiceCollection();
        services.AddDomainEventDispatch<AdminWriteDbContext>();
        services.AddIdempotencyStore<AdminWriteDbContext>();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddPostgreSqlIntegrationEventTransportProducer<AdminWriteDbContext>(
            IntegrationEventConsumerNames.Catalog);
        using var provider = services.BuildServiceProvider();
        var configurations = provider.GetServices<IDbContextConfiguration<AdminWriteDbContext>>().ToArray();
        var options = new DbContextOptionsBuilder<AdminWriteDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new AdminWriteDbContext(options, configurations);
    }
}
