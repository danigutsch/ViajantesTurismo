using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

namespace ViajantesTurismo.HostedTests.Infrastructure;

/// <summary>Cleanup helper for hosted tests.</summary>
internal static class HostedTestCleanup
{
    /// <summary>Disposes every resource even if some fail, then rethrows the single failure or an <see cref="AggregateException" /> of all failures including the operation failure.</summary>
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Cleanup must attempt every resource and report all failures without losing the operation failure.")]
    internal static async Task DisposeResources(
        Exception? operationFailure,
        params IAsyncDisposable?[] resources)
    {
        List<Exception> failures = operationFailure is null ? [] : [operationFailure];
        foreach (var resource in resources)
        {
            if (resource is null)
            {
                continue;
            }

            try
            {
                await resource.DisposeAsync();
            }
            catch (Exception cleanupFailure)
            {
                failures.Add(cleanupFailure);
            }
        }

        if (failures.Count == 0)
        {
            return;
        }

        if (failures.Count == 1)
        {
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        }

        throw new AggregateException("Hosted test cleanup failed.", failures);
    }
}
