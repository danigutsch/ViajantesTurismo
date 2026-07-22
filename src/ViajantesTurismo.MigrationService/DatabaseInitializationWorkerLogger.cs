namespace ViajantesTurismo.MigrationService;

internal static partial class DatabaseInitializationWorkerLogger
{
    [LoggerMessage(1, LogLevel.Information, "Starting database initialization...")]
    public static partial void InitializationStarted(this ILogger logger);

    [LoggerMessage(2, LogLevel.Information, "Database initialization completed.")]
    public static partial void InitializationCompleted(this ILogger logger);

    [LoggerMessage(3, LogLevel.Error, "Database initialization failed. Failure type: {FailureType}.")]
    public static partial void InitializationFailed(this ILogger logger, string failureType);

    [LoggerMessage(4, LogLevel.Information, "Database initialization cancelled.")]
    public static partial void InitializationCancelled(this ILogger logger);
}
