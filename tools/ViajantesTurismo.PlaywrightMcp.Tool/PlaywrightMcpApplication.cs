using System.ComponentModel;
using System.Diagnostics;

namespace ViajantesTurismo.PlaywrightMcp.Tool;

internal readonly record struct ProcessResult(int ExitCode, string StandardOutput, string StandardError);

internal static class PlaywrightMcpApplication
{
    public static Task<int> Run(
        string[] args,
        TextWriter error,
        CancellationToken cancellationToken = default)
    {
        return Run(
            args,
            error,
            Environment.GetEnvironmentVariable,
            ResolveExecutable,
            RunProcess,
            cancellationToken);
    }

    internal static async Task<int> Run(
        string[] args,
        TextWriter error,
        Func<string, string?> getEnvironmentVariable,
        Func<string, string?> resolveExecutable,
        Func<ProcessStartInfo, CancellationToken, Task<ProcessResult>> runProcess,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);
        ArgumentNullException.ThrowIfNull(resolveExecutable);
        ArgumentNullException.ThrowIfNull(runProcess);

        try
        {
            var options = PlaywrightMcpOptions.Parse(getEnvironmentVariable, resolveExecutable);
            cancellationToken.ThrowIfCancellationRequested();
            if (options.Engine == ContainerEngine.Docker)
            {
                var probe = PlaywrightMcpCommand.CreateDockerContextProbeStartInfo(options);
                var probeResult = await runProcess(probe, cancellationToken).ConfigureAwait(false);
                if (probeResult.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"Docker context inspection failed: {probeResult.StandardError.Trim()}");
                }

                if (!PlaywrightMcpCommand.IsLocalDockerEndpoint(probeResult.StandardOutput))
                {
                    throw new InvalidOperationException(
                        $"Refusing non-local Docker default context: {probeResult.StandardOutput.Trim()}");
                }
            }
            else
            {
                var probe = PlaywrightMcpCommand.CreatePodmanRootlessProbeStartInfo(options);
                var probeResult = await runProcess(probe, cancellationToken).ConfigureAwait(false);
                if (probeResult.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"Podman rootless inspection failed: {probeResult.StandardError.Trim()}");
                }

                if (!string.Equals(probeResult.StandardOutput.Trim(), "true", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Playwright MCP requires local rootless Podman mode.");
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (args is ["prepare"])
            {
                var result = await runProcess(
                    PlaywrightMcpCommand.CreatePullStartInfo(options),
                    cancellationToken).ConfigureAwait(false);
                return result.ExitCode;
            }

            if (args is ["clean"])
            {
                var result = await runProcess(
                    PlaywrightMcpCommand.CreateImageRemoveStartInfo(options),
                    cancellationToken).ConfigureAwait(false);
                return result.ExitCode;
            }

            var containerName = $"viajantes-playwright-mcp-{Environment.ProcessId}-{Guid.NewGuid():N}";
            try
            {
                var startInfo = PlaywrightMcpCommand.CreateRuntimeStartInfo(options, args, containerName);
                var result = await runProcess(startInfo, cancellationToken).ConfigureAwait(false);
                return result.ExitCode;
            }
            catch (OperationCanceledException)
            {
                using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var cleanup = PlaywrightMcpCommand.CreateContainerCleanupStartInfo(options, containerName);
                ProcessResult cleanupResult;
                try
                {
                    cleanupResult = await runProcess(cleanup, cleanupTimeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException exception)
                {
                    throw new InvalidOperationException(
                        $"Cancellation cleanup timed out for container '{containerName}'. Remove it manually.",
                        exception);
                }

                if (cleanupResult.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"Cancellation cleanup failed for container '{containerName}': {cleanupResult.StandardError.Trim()}");
                }

                throw;
            }
        }
        catch (ArgumentException exception)
        {
            await error.WriteLineAsync($"Error: {exception.Message}").ConfigureAwait(false);
            return 2;
        }
        catch (InvalidOperationException exception)
        {
            await error.WriteLineAsync($"Error: {exception.Message}").ConfigureAwait(false);
            return 1;
        }
        catch (Win32Exception exception)
        {
            await error.WriteLineAsync($"Error: Could not start Playwright MCP: {exception.Message}").ConfigureAwait(false);
            return 1;
        }
        catch (OperationCanceledException)
        {
            await error.WriteLineAsync("Error: Playwright MCP was cancelled.").ConfigureAwait(false);
            return 1;
        }
    }

    private static async Task<ProcessResult> RunProcess(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = startInfo };
        cancellationToken.ThrowIfCancellationRequested();
        if (!process.Start())
        {
            throw new InvalidOperationException($"Could not start {startInfo.FileName}.");
        }

        var standardOutput = startInfo.RedirectStandardOutput
            ? process.StandardOutput.ReadToEndAsync(CancellationToken.None)
            : Task.FromResult(string.Empty);
        var standardError = startInfo.RedirectStandardError
            ? process.StandardError.ReadToEndAsync(CancellationToken.None)
            : Task.FromResult(string.Empty);

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            }

            throw;
        }

        return new ProcessResult(
            process.ExitCode,
            await standardOutput.ConfigureAwait(false),
            await standardError.ConfigureAwait(false));
    }

    private static string? ResolveExecutable(string command)
    {
        return ResolveExecutable(
            command,
            Environment.GetEnvironmentVariable("PATH"),
            OperatingSystem.IsWindows());
    }

    internal static string? ResolveExecutable(string command, string? path, bool isWindows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var fileNames = isWindows
            ? new[] { $"{command}.exe", command }
            : new[] { command };

        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var absoluteDirectory = directory.Trim('"');
            if (!Path.IsPathFullyQualified(absoluteDirectory))
            {
                continue;
            }

            foreach (var fileName in fileNames)
            {
                var candidate = Path.Combine(absoluteDirectory, fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }
}
