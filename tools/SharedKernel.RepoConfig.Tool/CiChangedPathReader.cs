using System.ComponentModel;
using System.Diagnostics;

namespace SharedKernel.RepoConfig.Tool;

internal static class CiChangedPathReader
{
    private static readonly TimeSpan GitTimeout = TimeSpan.FromSeconds(30);

    public static async Task<IReadOnlyList<string>> Read(
        string rootPath,
        string baseSha,
        string headSha,
        bool useMergeBase,
        CancellationToken ct)
    {
        if (!IsFullObjectId(baseSha) || !IsFullObjectId(headSha))
        {
            throw new InvalidOperationException("Git diff revisions must be full hexadecimal Git object IDs.");
        }

        var range = $"{baseSha}{(useMergeBase ? "..." : "..")}{headSha}";
        var gitExecutable = ExecutableResolver.Resolve(
            "git",
            Environment.GetEnvironmentVariable("PATH"),
            OperatingSystem.IsWindows())
            ?? throw new InvalidOperationException("Could not resolve git to an absolute executable path.");
        ProcessStartInfo startInfo = new(gitExecutable)
        {
            WorkingDirectory = rootPath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("diff");
        startInfo.ArgumentList.Add("--no-renames");
        startInfo.ArgumentList.Add("--name-only");
        startInfo.ArgumentList.Add(range);
        startInfo.ArgumentList.Add("--");

        using var process = StartProcess(startInfo, static info => Process.Start(info));
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(GitTimeout);

        try
        {
            var standardOutput = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
            var standardError = process.StandardError.ReadToEndAsync(timeoutCts.Token);
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            var output = await standardOutput.ConfigureAwait(false);
            var error = await standardError.ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"git diff failed for CI test selection: {Limit(error)}");
            }

            return output
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(RepoConfigPaths.Normalize)
                .ToArray();
        }
        catch (OperationCanceledException exception) when (!ct.IsCancellationRequested)
        {
            await Stop(process).ConfigureAwait(false);
            throw new TimeoutException("git diff timed out during CI test selection.", exception);
        }
        catch
        {
            await Stop(process).ConfigureAwait(false);

            throw;
        }
    }

    private static string Limit(string value) =>
        value.Length <= 1_000 ? value : value[..1_000];

    private static bool IsFullObjectId(string value) =>
        value.Length is 40 or 64 && value.All(Uri.IsHexDigit);

    internal static Process StartProcess(
        ProcessStartInfo startInfo,
        Func<ProcessStartInfo, Process?> start)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        ArgumentNullException.ThrowIfNull(start);

        try
        {
            return start(startInfo)
                ?? throw new InvalidOperationException("Could not start git diff for CI test selection.");
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException("Could not launch git for CI test selection.", exception);
        }
    }

    private static async Task Stop(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }

        await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
