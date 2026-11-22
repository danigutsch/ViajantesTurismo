using PostgreSqlTestDatabase = PandaBear.Npgsql.Testing.PostgreSqlTestDatabaseLease;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PandaBear.Domain.EntityFrameworkCore;
using PandaBear.EntityFrameworkCore;
using PandaBear.Idempotency.EntityFrameworkCore;
using PandaBear.Mediator;
using PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Application.Import;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Resources;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Infrastructure.Customers;
using ViajantesTurismo.Admin.Infrastructure.DispositionHolds;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Customers;

/// <summary>Runs customer email behavior against a real PostgreSQL database.</summary>
internal sealed class CustomerEmailPostgreSqlScenario : IAsyncDisposable
{
    private readonly PostgreSqlTestDatabase database;
    private readonly string connectionString;

    private CustomerEmailPostgreSqlScenario(PostgreSqlTestDatabase database)
    {
        this.database = database;
        connectionString = database.ConnectionString;
    }

    /// <summary>Creates a scenario over a new fixture-issued database.</summary>
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "The returned scenario owns and disposes the fixture-issued database.")]
    public static async ValueTask<CustomerEmailPostgreSqlScenario> Create(
        PostgreSqlTestServerFixture fixture,
        CancellationToken ct)
    {
        var database = await fixture.CreateDatabase(ct);
        return new CustomerEmailPostgreSqlScenario(database);
    }

    /// <summary>Applies all pending migrations.</summary>
    public async Task ApplyLatestMigration(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(ct);
    }

    /// <summary>Creates the <c>citext</c> extension when it is missing.</summary>
    public async Task CreateCustomerEmailExtension(CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "CREATE EXTENSION IF NOT EXISTS citext;";
        _ = await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>Returns whether the <c>citext</c> extension is installed in the database.</summary>
    public async Task<bool> CustomerEmailExtensionExists(CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'citext');";
        return (bool)(await command.ExecuteScalarAsync(ct) ?? false);
    }

    /// <summary>Migrates the database up to the named migration.</summary>
    public async Task ApplyMigration(string migration, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        var migrator = dbContext.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(migration, ct);
    }

    /// <summary>Generates the SQL script of all migrations without applying it.</summary>
    public string GenerateLatestMigrationScript()
    {
        using var dbContext = CreateDbContext();
        var migrator = dbContext.Database.GetService<IMigrator>();
        return migrator.GenerateScript();
    }

    /// <summary>Persists a customer built with the given email and returns it.</summary>
    public async Task<Customer> AddCustomer(string email, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        var customer = EntityBuilders.BuildCustomer(new CustomerOptions(Email: email));
        dbContext.Customers.Add(customer);
        await dbContext.SaveEntities(ct);
        return customer;
    }

    /// <summary>Looks up a customer by email through the customer store.</summary>
    public async Task<Customer?> GetByEmail(string email, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await new CustomerStore(dbContext).GetByEmail(email, ct);
    }

    /// <summary>Counts the stored customers.</summary>
    public async Task<int> CountCustomers(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.Customers.CountAsync(ct);
    }

    /// <summary>Commits an import, injecting a competing insert immediately before the real database save.</summary>
    public async Task<(ImportResultDto Result, bool UsedTransaction, int SaveCalls)> CommitImportWithConcurrentInsert(
        string csv,
        IReadOnlyDictionary<string, string> resolutions,
        string racedEmail,
        CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        var store = new CustomerStore(dbContext);
        var saveProbe = new CustomerImportSaveProbe(dbContext, () => AddCustomer(racedEmail, ct));
        await using var provider = CreateImportProvider(dbContext, store, saveProbe);
        await using var scope = provider.CreateAsyncScope();
        var workflow = scope.ServiceProvider.GetRequiredService<CustomerImportWorkflowService>();
        var result = await workflow.Commit(csv, resolutions, ct);
        return (result, saveProbe.UsedTransaction, saveProbe.SaveCalls);
    }

    /// <summary>Dispatches a dry run through the generated Admin mediator against the real write context.</summary>
    public async Task<(Result<ImportResult> Result, int SaveCalls)> DryRunImport(string csv, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        var store = new CustomerStore(dbContext);
        var saveProbe = new CustomerImportSaveProbe(dbContext);
        await using var provider = CreateImportProvider(dbContext, store, saveProbe);
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var result = await sender.Send(new CustomerImportCommand(csv, DryRun: true), ct);
        return (result, saveProbe.SaveCalls);
    }

    /// <summary>Dispatches a write that encounters a transient failure after its first database flush.</summary>
    public async Task<(Result<ImportResult> Result, int SaveCalls)> RetryImportAfterRealSave(string csv, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext(enableRetry: true);
        var store = new CustomerStore(dbContext);
        var saveProbe = new CustomerImportSaveProbe(dbContext, afterSave: static attempt =>
        {
            if (attempt == 1)
            {
                throw new TimeoutException("Fixture timeout after the first import save.");
            }
        });
        await using var provider = CreateImportProvider(dbContext, store, saveProbe);
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var result = await sender.Send(new CustomerImportCommand(csv, DryRun: false), ct);
        return (result, saveProbe.SaveCalls);
    }

    /// <summary>Disposes the fixture-issued database.</summary>
    public ValueTask DisposeAsync() => database.DisposeAsync();

    private static ServiceProvider CreateImportProvider(AdminWriteDbContext dbContext, ICustomerStore store, IUnitOfWork unitOfWork)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<DbContext>(dbContext);
        services.AddSingleton(store);
        services.AddSingleton(unitOfWork);
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CustomerImportConflictDetector>();
        services.AddScoped<CustomerImportWorkflowService>();
        services.AddSharedKernelMediator();
        return services.BuildServiceProvider();
    }

    private AdminWriteDbContext CreateDbContext(bool enableRetry = false)
    {
        var services = new ServiceCollection();
        services.AddDomainEventDispatch<AdminWriteDbContext>();
        services.AddIdempotencyStore<AdminWriteDbContext>();
        services.AddAdminDispositionHolds();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddPostgreSqlIntegrationEventTransportProducer<AdminWriteDbContext>(
            IntegrationEventConsumerNames.Catalog);
        using var provider = services.BuildServiceProvider();
        var configurations = provider.GetServices<IDbContextConfiguration<AdminWriteDbContext>>().ToArray();
        var options = new DbContextOptionsBuilder<AdminWriteDbContext>()
            .UseNpgsql(connectionString, options =>
            {
                if (enableRetry)
                {
                    options.EnableRetryOnFailure();
                }
            })
            .Options;
        return new AdminWriteDbContext(options, configurations);
    }
}
