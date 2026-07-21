namespace ViajantesTurismo.MigrationService;

internal static partial class MigrationRunnerLogger
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
