using System.Diagnostics;

namespace ViajantesTurismo.OpenApi.Tool;

internal static class OpenApiToolApplication
{
    private const string Usage = "Usage: dotnet run --project tools/ViajantesTurismo.OpenApi.Tool --no-restore -- generate <admin|catalog|branding> [--refresh]";
    private static readonly TimeSpan GenerationTimeout = TimeSpan.FromMinutes(10);

    public static Task<int> Run(
        string[] args,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken = default)
    {
        return Run(args, output, error, static startInfo => Process.Start(startInfo), cancellationToken);
    }

    internal static async Task<int> Run(
        string[] args,
        TextWriter output,
        TextWriter error,
        Func<ProcessStartInfo, Process?> startProcess,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(startProcess);

        if (args is [] or ["--help"] or ["-h"])
        {
            await output.WriteLineAsync(Usage).ConfigureAwait(false);
            return 0;
        }

        using var timeout = new CancellationTokenSource(GenerationTimeout);
        using var generationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        Process? process = null;

        try
        {
            var options = OpenApiGenerationOptions.Parse(args, FindRepositoryRoot(Directory.GetCurrentDirectory()));
            var startInfo = OpenApiGenerationCommand.CreateStartInfo(options);
            generationCancellation.Token.ThrowIfCancellationRequested();
            process = startProcess(startInfo) ?? throw new InvalidOperationException("Could not start dotnet build.");
            await process.WaitForExitAsync(generationCancellation.Token).ConfigureAwait(false);
            return process.ExitCode;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || timeout.IsCancellationRequested)
        {
            var processStopped = await StopProcess(process).ConfigureAwait(false);
            var reason = cancellationToken.IsCancellationRequested
                ? "OpenAPI generation was cancelled."
                : "OpenAPI generation timed out.";
            if (!processStopped)
            {
                reason = $"{reason} Child process cleanup did not complete.";
            }

            await error.WriteLineAsync($"Error: {reason}").ConfigureAwait(false);
            return 1;
        }
        catch (ArgumentException exception)
        {
            await error.WriteLineAsync($"Error: {exception.Message}").ConfigureAwait(false);
            return 2;
        }
        catch (InvalidOperationException exception)
        {
            var processStopped = await StopProcess(process).ConfigureAwait(false);
            var detail = processStopped
                ? exception.Message
                : $"{exception.Message} Child process cleanup did not complete.";
            await error.WriteLineAsync($"Error: OpenAPI generation failed: {detail}").ConfigureAwait(false);
            return 1;
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            var processStopped = await StopProcess(process).ConfigureAwait(false);
            var detail = processStopped
                ? exception.Message
                : $"{exception.Message} Child process cleanup did not complete.";
            await error.WriteLineAsync($"Error: OpenAPI generation failed: {detail}").ConfigureAwait(false);
            return 1;
        }
        finally
        {
            process?.Dispose();
        }
    }

    internal static string FindRepositoryRoot(string startDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startDirectory);

        var currentDirectory = new DirectoryInfo(startDirectory);

        while (currentDirectory is not null)
        {
            if (File.Exists(Path.Combine(currentDirectory.FullName, "ViajantesTurismo.slnx")))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate ViajantesTurismo.slnx from '{startDirectory}'.");
    }

    private static async Task<bool> StopProcess(Process? process)
    {
        if (process is null)
        {
            return true;
        }

        try
        {
            if (process.HasExited)
            {
                return true;
            }

            process.Kill(entireProcessTree: true);

            using var terminationTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await process.WaitForExitAsync(terminationTimeout.Token).ConfigureAwait(false);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
