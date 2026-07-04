using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ViajantesTurismo.ServiceDefaults;

internal sealed class ApplicationVersionLoggingService(
    IHostEnvironment environment,
    ILogger<ApplicationVersionLoggingService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        logger.ApplicationVersion(environment.ApplicationName, string.IsNullOrWhiteSpace(version) ? "unknown" : version);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal static partial class ApplicationVersionLogger
{
    [LoggerMessage(1, LogLevel.Information, "Application {ApplicationName} version {ApplicationVersion}")]
    public static partial void ApplicationVersion(this ILogger logger, string applicationName, string applicationVersion);
}
