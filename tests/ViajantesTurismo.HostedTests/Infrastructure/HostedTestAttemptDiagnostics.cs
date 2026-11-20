using System.Collections.Concurrent;
using System.Text.Json;
using Xunit.Sdk;

namespace ViajantesTurismo.HostedTests.Infrastructure;

/// <summary>Writes failure evidence from xUnit's metadata and forwards its original messages.</summary>
/// <param name="sink">The test platform's normal message sink.</param>
/// <param name="directory">The dedicated directory for bounded attempt evidence.</param>
internal sealed class HostedTestAttemptDiagnostics(IMessageSink sink, string directory) : IMessageSink
{
    private const int SchemaVersion = 1;
    private const int MaximumRecordedFailures = 10;
    private const int MaximumIdentifierLength = 256;
    private readonly ConcurrentDictionary<string, (string? TestClass, string? TestMethod)> cases = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, (DateTimeOffset StartedUtc, string? TestClass, string? TestMethod, FailureCause? Cause, bool CleanupFailed)>
        active = new(StringComparer.Ordinal);
    private readonly string? commit = ReadCommit();
    private int failures;

    /// <inheritdoc/>
    public bool OnMessage(IMessageSinkMessage message)
    {
        var continueRunning = sink.OnMessage(message);
        switch (message)
        {
            case ITestCaseStarting started:
                cases.TryAdd(started.TestCaseUniqueID, (BoundIdentifier(started.TestClassName), BoundIdentifier(started.TestMethodName)));
                break;
            case ITestStarting started:
                cases.TryGetValue(started.TestCaseUniqueID, out var identity);
                active.TryAdd(started.TestUniqueID, (started.StartTime.ToUniversalTime(), identity.TestClass, identity.TestMethod, null, false));
                break;
            case ITestFailed failed when active.TryGetValue(failed.TestUniqueID, out var attempt):
                attempt.Cause = failed.Cause;
                active[failed.TestUniqueID] = attempt;
                break;
            case ITestCleanupFailure failed when active.TryGetValue(failed.TestUniqueID, out var attempt):
                attempt.CleanupFailed = true;
                active[failed.TestUniqueID] = attempt;
                break;
            case ITestFinished finished when active.TryRemove(finished.TestUniqueID, out var attempt):
                if (attempt.Cause is not null || attempt.CleanupFailed)
                {
                    WriteFailure(attempt.StartedUtc, finished.FinishTime.ToUniversalTime(), attempt.TestClass, attempt.TestMethod,
                        attempt.Cause, attempt.CleanupFailed);
                }

                break;
            case ITestCaseFinished finished:
                cases.TryRemove(finished.TestCaseUniqueID, out _);
                break;
        }

        return continueRunning;
    }

    private void WriteFailure(
        DateTimeOffset startedUtc, DateTimeOffset finishedUtc, string? testClass, string? testMethod,
        FailureCause? failureCause, bool cleanupFailed)
    {
        var number = Interlocked.Increment(ref failures);
        if (number > MaximumRecordedFailures)
        {
            if (number == MaximumRecordedFailures + 1)
            {
                Write("truncation", new
                {
                    schemaVersion = SchemaVersion,
                    maximumRecordedFailures = MaximumRecordedFailures,
                });
            }

            return;
        }

        var id = Guid.NewGuid().ToString("N");
        Write(id, new
        {
            schemaVersion = SchemaVersion,
            attemptId = id,
            suite = "ViajantesTurismo.HostedTests",
            commit,
            testClass,
            testMethod,
            startedUtc,
            finishedUtc,
            outcome = "failed",
            failureCause = failureCause?.ToString(),
            cleanupFailed,
        });
    }

    private void Write<T>(string name, T evidence)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var temporary = Path.Combine(directory, $"{name}.tmp");
            File.WriteAllText(temporary, JsonSerializer.Serialize(evidence));
            File.Move(temporary, Path.Combine(directory, $"{name}.json"), overwrite: true);
        }
        catch (IOException)
        {
            // Keep the original xUnit result when collecting its evidence fails.
        }
        catch (UnauthorizedAccessException)
        {
            // The runner can still report the same failure when the evidence directory is unwritable.
        }
    }

    private static string? BoundIdentifier(string? value) =>
        value is { Length: > MaximumIdentifierLength } ? value[..MaximumIdentifierLength] : value;

    private static string? ReadCommit()
    {
        var sha = Environment.GetEnvironmentVariable("GITHUB_SHA");
        return sha is { Length: 40 or 64 } && sha.All(Uri.IsHexDigit) ? sha : null;
    }
}
