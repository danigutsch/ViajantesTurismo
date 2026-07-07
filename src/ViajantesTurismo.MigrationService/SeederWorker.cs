using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.EventSourcing.Npgsql;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.MigrationService;

internal sealed class SeederWorker : BackgroundService
{
    private const string ActivityOperationName = "DatabaseSeeding";
    public static readonly string ActivitySourceName = typeof(SeederWorker).FullName!;
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName, Assembly.GetAssembly(typeof(SeederWorker))!.GetName().Version?.ToString());
    private readonly IHostApplicationLifetime host;
    private readonly ILogger<SeederWorker> logger;
    private readonly Func<IServiceProvider, CancellationToken, Task> seedOperation;
    private readonly IServiceScopeFactory scopeFactory;

    public SeederWorker(IServiceScopeFactory scopeFactory, ILogger<SeederWorker> logger, IHostApplicationLifetime host)
        : this(scopeFactory, logger, host, RunDatabaseInitialization)
    {
    }

    internal SeederWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<SeederWorker> logger,
        IHostApplicationLifetime host,
        Func<IServiceProvider, CancellationToken, Task> seedOperation)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(seedOperation);

        this.scopeFactory = scopeFactory;
        this.logger = logger;
        this.host = host;
        this.seedOperation = seedOperation;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = ActivitySource.StartActivity(ActivityOperationName, ActivityKind.Producer, parentContext: default);

        try
        {
            activity?.SetTag("operation.type", "database_seeding");
            activity?.SetTag("worker.type", "migration");

            logger.SeedingStarted();

            using var scope = scopeFactory.CreateScope();
            await seedOperation(scope.ServiceProvider, stoppingToken);

            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.SeedingCompleted();
        }
        catch (OperationCanceledException)
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
        finally
        {
            host.StopApplication();
        }
    }

    private static async Task RunDatabaseInitialization(IServiceProvider serviceProvider, CancellationToken stoppingToken)
    {
        var catalogDbContext = serviceProvider.GetRequiredService<CatalogDbContext>();
        var seeder = serviceProvider.GetRequiredService<Seeder>();

        if (catalogDbContext.Database.IsRelational())
        {
            await catalogDbContext.Database.MigrateAsync(stoppingToken);
            await InitializeCatalogEventSourcingSchema(serviceProvider, stoppingToken);
        }

        await seeder.Seed(stoppingToken);
    }

    private static async ValueTask InitializeCatalogEventSourcingSchema(IServiceProvider serviceProvider, CancellationToken ct)
    {
        var dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();
        await PostgreSqlEventSourcingSchema.Initialize(dataSource, options: null, ct);
    }
}

internal static partial class SeederWorkerLogger
{
    [LoggerMessage(1, LogLevel.Information, "Starting database seeding...")]
    public static partial void SeedingStarted(this ILogger logger);

    [LoggerMessage(2, LogLevel.Information, "Database seeding completed.")]
    public static partial void SeedingCompleted(this ILogger logger);

    [LoggerMessage(3, LogLevel.Error, "Database seeding failed")]
    public static partial void SeedingFailed(this ILogger logger, Exception exception);

    [LoggerMessage(4, LogLevel.Information, "Database seeding cancelled.")]
    public static partial void SeedingCancelled(this ILogger logger);
}
