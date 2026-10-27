using System.ComponentModel;
using System.Diagnostics;

namespace ViajantesTurismo.ArchitectureTests.Repository;

/// <summary>Runs child processes and locates the repository root for repository tests.</summary>
internal static class RepositoryTestProcess
{
    /// <summary>Starts a process, captures its output and exit code, and kills its process tree if <paramref name="cancellationToken"/> is canceled.</summary>
    public static async Task<ProcessResult> Run(
        string fileName,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (environment is not null)
        {
            foreach (var variable in environment)
            {
                startInfo.Environment[variable.Key] = variable.Value;
            }
        }

        using var process = Process.Start(startInfo).ShouldNotBeNull();
        var standardOutput = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var standardError = process.StandardError.ReadToEndAsync(CancellationToken.None);
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    using var terminationTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    await process.WaitForExitAsync(terminationTimeout.Token);
                }
            }
            catch (Exception exception) when (exception is InvalidOperationException
                or Win32Exception
                or NotSupportedException
                or OperationCanceledException)
            {
                // Preserve the original cancellation after best-effort child cleanup.
            }

            var outputDrain = Task.WhenAll(standardOutput, standardError);
            try
            {
                await outputDrain.WaitAsync(TimeSpan.FromSeconds(2), CancellationToken.None);
            }
            catch (Exception exception) when (exception is TimeoutException
                or IOException
                or InvalidOperationException)
            {
                _ = outputDrain.ContinueWith(
                    static completed => _ = completed.Exception,
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            throw;
        }

        return new ProcessResult(
            process.ExitCode,
            await standardOutput,
            await standardError);
    }

    /// <summary>Walks up from the test base directory to the folder containing <c>ViajantesTurismo.slnx</c>.</summary>
    public static string GetRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ViajantesTurismo.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    /// <summary>Exit code and captured output of a finished process.</summary>
    /// <param name="ExitCode">Process exit code.</param>
    /// <param name="StandardOutput">Full text written to standard output.</param>
    /// <param name="StandardError">Full text written to standard error.</param>
    internal readonly record struct ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
