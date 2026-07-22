using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.EventSourcing.Npgsql;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Management.Security;

namespace ViajantesTurismo.MigrationService;

internal sealed class DatabaseInitializationWorker
{
    private const string ActivityOperationName = "DatabaseInitialization";
    public const string ActivitySourceName = "ViajantesTurismo.MigrationService.DatabaseInitializationWorker";
    private static readonly ActivitySource DefaultActivitySource = new(
        ActivitySourceName,
        typeof(DatabaseInitializationWorker).Assembly.GetName().Version?.ToString());
    private readonly ActivitySource activitySource;
    private readonly Func<IServiceProvider, CancellationToken, Task> developmentDataOperation;
    private readonly IHostEnvironment environment;
    private readonly ILogger logger;
    private readonly Func<IServiceProvider, CancellationToken, Task> migrationOperation;
    private readonly IServiceScopeFactory scopeFactory;

    public DatabaseInitializationWorker(
        IServiceScopeFactory scopeFactory,
        IHostEnvironment environment,
        ILoggerFactory loggerFactory)
        : this(
            scopeFactory,
            environment,
            loggerFactory.CreateLogger<DatabaseInitializationWorker>(),
            RunMigrations,
            RunDevelopmentDataInitialization,
            DefaultActivitySource)
    {
    }

    internal DatabaseInitializationWorker(
        IServiceScopeFactory scopeFactory,
        IHostEnvironment environment,
        ILogger logger,
        Func<IServiceProvider, CancellationToken, Task> migrationOperation,
        Func<IServiceProvider, CancellationToken, Task> developmentDataOperation,
        ActivitySource? activitySource = null)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(migrationOperation);
        ArgumentNullException.ThrowIfNull(developmentDataOperation);

        this.scopeFactory = scopeFactory;
        this.environment = environment;
        this.logger = logger;
        this.migrationOperation = migrationOperation;
        this.developmentDataOperation = developmentDataOperation;
        this.activitySource = activitySource ?? DefaultActivitySource;
    }

    internal async Task Run(CancellationToken ct)
    {
        using var activity = activitySource.StartActivity(ActivityOperationName, ActivityKind.Producer, parentContext: default);

        try
        {
            activity?.SetTag("operation.type", "database_initialization");
            activity?.SetTag("worker.type", "migration");
            logger.InitializationStarted();

            var scope = scopeFactory.CreateAsyncScope();
            await using (scope.ConfigureAwait(false))
            {
                await migrationOperation(scope.ServiceProvider, ct);
                if (environment.IsDevelopment())
                {
                    await developmentDataOperation(scope.ServiceProvider, ct);
                }
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.InitializationCompleted();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            logger.InitializationCancelled();
            throw;
        }
        catch (Exception exception)
        {
            activity?.SetTag("error.type", exception.GetType().Name);
            activity?.SetStatus(ActivityStatusCode.Error);

            logger.InitializationFailed(exception.GetType().Name);
            throw;
        }
    }

    private static async Task RunMigrations(IServiceProvider serviceProvider, CancellationToken ct)
    {
        var catalogDbContext = serviceProvider.GetRequiredService<CatalogDbContext>();
        var brandingDbContext = serviceProvider.GetRequiredService<BrandingDbContext>();
        var managementSecurityDbContext = serviceProvider.GetRequiredService<ManagementSecurityDbContext>();

        await serviceProvider.MigrateAdminDatabase(ct);

        if (catalogDbContext.Database.IsRelational())
        {
            var catalogDataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();
            await catalogDbContext.Database.MigrateAsync(ct);
            await PostgreSqlEventSourcingSchema.Initialize(catalogDataSource, options: null, ct);
        }

        if (brandingDbContext.Database.IsRelational())
        {
            await brandingDbContext.Database.MigrateAsync(ct);
        }

        if (managementSecurityDbContext.Database.IsRelational())
        {
            await managementSecurityDbContext.Database.MigrateAsync(ct);
        }
    }

    private static Task RunDevelopmentDataInitialization(
        IServiceProvider serviceProvider,
        CancellationToken ct)
    {
        var initializer = serviceProvider.GetRequiredService<DevelopmentDataInitializer>();
        return initializer.Initialize(ct);
    }
}
