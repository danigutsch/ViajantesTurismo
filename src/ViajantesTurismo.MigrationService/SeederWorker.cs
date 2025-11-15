using System.Diagnostics;
using ViajantesTurismo.Admin.Application;

namespace ViajantesTurismo.MigrationService;

internal sealed class SeederWorker(IServiceScopeFactory scopeFactory, ILogger<SeederWorker> logger, IHostApplicationLifetime host) : BackgroundService
{
    private const string ActivityOperationName = "DatabaseSeeding";
    public static readonly string ActivitySourceName = typeof(SeederWorker).FullName!;
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName, "1.0.0");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = ActivitySource.StartActivity(ActivityOperationName, ActivityKind.Producer, parentContext: default);

        try
        {
            activity?.SetTag("operation.type", "database_seeding");
            activity?.SetTag("worker.type", "migration");

            logger.SeedingStarted();

            using var scope = scopeFactory.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<ISeeder>();

            await seeder.Seed(stoppingToken);

            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.SeedingCompleted();
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);
            activity?.SetTag("exception.stacktrace", ex.StackTrace);

            logger.SeedingFailed(ex);
            throw;
        }
        finally
        {
            host.StopApplication();
        }
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
}
