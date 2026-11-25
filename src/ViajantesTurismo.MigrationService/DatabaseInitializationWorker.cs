using System.Diagnostics;
using System.Diagnostics.Metrics;
using SharedKernel.EntityFrameworkCore;
using Npgsql;
using SharedKernel.EventSourcing.Npgsql;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Management.Security;

namespace ViajantesTurismo.MigrationService;

/// <summary>Runs database migrations and, in development, seeds development data.</summary>
internal sealed class DatabaseInitializationWorker
{
    private const string ActivityOperationName = "DatabaseInitialization";
    private const string MeterName = "ViajantesTurismo.MigrationService";
    /// <summary>Name of the activity source that traces database initialization.</summary>
    public const string ActivitySourceName = "ViajantesTurismo.MigrationService.DatabaseInitializationWorker";
    private static readonly ActivitySource DefaultActivitySource = new(
        ActivitySourceName,
        typeof(DatabaseInitializationWorker).Assembly.GetName().Version?.ToString());
    private readonly ActivitySource activitySource;
    private readonly Func<IServiceProvider, CancellationToken, Task> developmentDataOperation;
    private readonly IHostEnvironment environment;
    private readonly ILogger logger;
    private readonly Meter meter;
    private readonly Func<IServiceProvider, CancellationToken, Task> migrationOperation;
    private readonly IServiceScopeFactory scopeFactory;

    /// <summary>Creates the worker with the default migration and development data operations.</summary>
    public DatabaseInitializationWorker(
        IServiceScopeFactory scopeFactory,
        IHostEnvironment environment,
        ILoggerFactory loggerFactory,
        IMeterFactory meterFactory)
        : this(
            scopeFactory,
            environment,
            loggerFactory.CreateLogger<DatabaseInitializationWorker>(),
            meterFactory,
            RunMigrations,
            RunDevelopmentDataInitialization,
            DefaultActivitySource)
    {
    }

    /// <summary>Creates the worker with explicit operations and an optional activity source.</summary>
    internal DatabaseInitializationWorker(
        IServiceScopeFactory scopeFactory,
        IHostEnvironment environment,
        ILogger logger,
        IMeterFactory meterFactory,
        Func<IServiceProvider, CancellationToken, Task> migrationOperation,
        Func<IServiceProvider, CancellationToken, Task> developmentDataOperation,
        ActivitySource? activitySource = null)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(meterFactory);
        ArgumentNullException.ThrowIfNull(migrationOperation);
        ArgumentNullException.ThrowIfNull(developmentDataOperation);

        this.scopeFactory = scopeFactory;
        this.environment = environment;
        this.logger = logger;
        this.migrationOperation = migrationOperation;
        this.developmentDataOperation = developmentDataOperation;
        this.activitySource = activitySource ?? DefaultActivitySource;
        meter = meterFactory.Create(MeterName);
    }

    /// <summary>Runs the migration and, in development, the development data operation inside a traced scope.</summary>
    internal async Task Run(CancellationToken ct)
    {
        var duration = meter.CreateHistogram<double>(
            "migration.database_initialization.duration",
            "s",
            "Duration of database migrations and any development data initialization.");
        using var activity = activitySource.StartActivity(ActivityOperationName, ActivityKind.Producer, parentContext: default);
        var started = Stopwatch.GetTimestamp();
        var outcome = "error";

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
            outcome = "success";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            outcome = "cancelled";
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
        finally
        {
            duration.Record(
                Stopwatch.GetElapsedTime(started).TotalSeconds,
                new KeyValuePair<string, object?>("migration.outcome", outcome));
        }
    }

    private static async Task RunMigrations(IServiceProvider serviceProvider, CancellationToken ct)
    {
        await serviceProvider.MigrateAdminDatabase(ct);

        if (await serviceProvider.ApplyMigrations<CatalogDbContext>(ct))
        {
            var catalogDataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();
            await PostgreSqlEventSourcingSchema.Initialize(catalogDataSource, options: null, ct);
        }

        if (await serviceProvider.ApplyMigrations<BrandingDbContext>(ct))
        {
            await serviceProvider.BootstrapBrandingRevisions(ct);
        }

        await serviceProvider.ApplyMigrations<ManagementSecurityDbContext>(ct);
    }

    private static Task RunDevelopmentDataInitialization(
        IServiceProvider serviceProvider,
        CancellationToken ct)
    {
        var initializer = serviceProvider.GetRequiredService<DevelopmentDataInitializer>();
        return initializer.Initialize(ct);
    }
}
