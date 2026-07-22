using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.EventSourcing.Npgsql;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Management.Security;

namespace ViajantesTurismo.MigrationService;

internal sealed class MigrationRunner
{
    private const string ActivityOperationName = "DatabaseSeeding";
    public const string ActivitySourceName = "ViajantesTurismo.MigrationService.SeederWorker";
    private static readonly ActivitySource DefaultActivitySource = new(
        ActivitySourceName,
        Assembly.GetAssembly(typeof(MigrationRunner))!.GetName().Version?.ToString());
    private readonly ActivitySource activitySource;
    private readonly ILogger logger;
    private readonly Func<IServiceProvider, CancellationToken, Task> seedOperation;
    private readonly IServiceScopeFactory scopeFactory;

    public MigrationRunner(IServiceScopeFactory scopeFactory, ILoggerFactory loggerFactory)
        : this(scopeFactory, loggerFactory.CreateLogger(ActivitySourceName), RunDatabaseInitialization, DefaultActivitySource)
    {
    }

    internal MigrationRunner(
        IServiceScopeFactory scopeFactory,
        ILogger logger,
        Func<IServiceProvider, CancellationToken, Task> seedOperation,
        ActivitySource? activitySource = null)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(seedOperation);

        this.scopeFactory = scopeFactory;
        this.logger = logger;
        this.seedOperation = seedOperation;
        this.activitySource = activitySource ?? DefaultActivitySource;
    }

    internal MigrationRunner(IServiceScopeFactory scopeFactory, ILogger logger, ActivitySource activitySource)
        : this(scopeFactory, logger, RunDatabaseInitialization, activitySource)
    {
    }

    internal async Task Run(CancellationToken ct)
    {
        using var activity = activitySource.StartActivity(ActivityOperationName, ActivityKind.Producer, parentContext: default);

        try
        {
            activity?.SetTag("operation.type", "database_seeding");
            activity?.SetTag("worker.type", "migration");

            logger.SeedingStarted();

            var scope = scopeFactory.CreateAsyncScope();
            await using (scope.ConfigureAwait(false))
            {
                await seedOperation(scope.ServiceProvider, ct);
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.SeedingCompleted();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            logger.SeedingCancelled();
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);

            logger.SeedingFailed(ex);
            throw;
        }
    }

    private static async Task RunDatabaseInitialization(IServiceProvider serviceProvider, CancellationToken ct)
    {
        var catalogDbContext = serviceProvider.GetRequiredService<CatalogDbContext>();
        var brandingDbContext = serviceProvider.GetRequiredService<BrandingDbContext>();
        var managementSecurityDbContext = serviceProvider.GetRequiredService<ManagementSecurityDbContext>();
        var seeder = serviceProvider.GetRequiredService<Seeder>();

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

        await seeder.Seed(ct);
    }
}
