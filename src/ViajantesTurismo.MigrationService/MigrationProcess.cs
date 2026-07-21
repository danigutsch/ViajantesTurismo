using System.Diagnostics.CodeAnalysis;

namespace ViajantesTurismo.MigrationService;

internal static class MigrationProcess
{
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "The process boundary converts every startup, migration, shutdown, and disposal failure to a nonzero exit code.")]
    public static async Task<int> Run(
        Func<IHost> createHost,
        Action<Exception>? reportFailure = null)
    {
        ArgumentNullException.ThrowIfNull(createHost);

        var failures = new List<Exception>();
        IHost? host = null;
        var started = false;
        try
        {
            host = createHost();
            await host.StartAsync().ConfigureAwait(false);
            started = true;

            var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            var worker = host.Services.GetRequiredService<DatabaseInitializationWorker>();
            await worker.Run(lifetime.ApplicationStopping).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }
        finally
        {
            if (host is not null)
            {
                if (started)
                {
                    try
                    {
                        await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        failures.Add(exception);
                    }
                }

                try
                {
                    host.Dispose();
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
        }

        if (failures.Count == 0)
        {
            return 0;
        }

        var failure = failures.Count == 1
            ? failures[0]
            : new AggregateException("Migration process and cleanup failed.", failures);
        (reportFailure ?? ReportFailure)(failure);
        return 1;
    }

    private static void ReportFailure(Exception exception) => Console.Error.WriteLine(exception);
}
