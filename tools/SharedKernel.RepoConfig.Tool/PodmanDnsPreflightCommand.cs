using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace SharedKernel.RepoConfig.Tool;

internal static class PodmanDnsPreflightCommand
{
    private const int MaximumPodmanOutputLength = 1_048_576;
    private const string Usage = "Usage: sharedkernel-repo podman-dns-preflight [--check] [--quiet]";
    private static readonly TimeSpan PodmanTimeout = TimeSpan.FromSeconds(30);
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static Task<int> Run(
        string[] args,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        var podmanExecutable = ExecutableResolver.Resolve(
            "podman",
            Environment.GetEnvironmentVariable("PATH"),
            OperatingSystem.IsWindows());
        var statExecutable = ExecutableResolver.Resolve(
            "stat",
            string.Concat("/usr/bin", Path.PathSeparator, "/bin"),
            isWindows: false);
        return Run(
            args,
            output,
            error,
            podmanExecutable,
            statExecutable,
            "/proc",
            OperatingSystem.IsLinux(),
            RunProcess,
            cancellationToken);
    }

    internal static async Task<int> Run(
        string[] args,
        TextWriter output,
        TextWriter error,
        string? podmanExecutable,
        string? statExecutable,
        string procRoot,
        bool isLinux,
        Func<ProcessStartInfo, CancellationToken, Task<PodmanCommandResult>> runProcess,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentException.ThrowIfNullOrWhiteSpace(procRoot);
        ArgumentNullException.ThrowIfNull(runProcess);

        var quiet = false;
        foreach (var argument in args)
        {
            switch (argument)
            {
                case "--check":
                    break;

                case "--quiet":
                    quiet = true;
                    break;

                case "-h" or "--help" when args.Length == 1:
                    await output.WriteLineAsync(Usage.AsMemory(), cancellationToken).ConfigureAwait(false);
                    return 0;

                default:
                    await error.WriteLineAsync($"Unknown argument: {RepoConfigToolApplication.EscapeControlCharacters(argument)}".AsMemory(), cancellationToken).ConfigureAwait(false);
                    await error.WriteLineAsync(Usage.AsMemory(), cancellationToken).ConfigureAwait(false);
                    return 2;
            }
        }

        if (podmanExecutable is null)
        {
            return await WriteHealthy(output, quiet, "podman is not installed; nothing to check", cancellationToken).ConfigureAwait(false);
        }

        if (!Path.IsPathFullyQualified(podmanExecutable))
        {
            return await WriteFailure(error, "podman executable path is not absolute", cancellationToken).ConfigureAwait(false);
        }

        if (!isLinux)
        {
            return await WriteFailure(error, "local Podman state can be inspected only on Linux", cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var serviceIsRemote = await QuerySingleValue(
                podmanExecutable,
                ["info", "--format", "{{.Host.ServiceIsRemote}}"],
                "podman info failed while reading remote-service mode",
                runProcess,
                cancellationToken).ConfigureAwait(false);
            switch (serviceIsRemote)
            {
                case "false":
                    break;

                case "true":
                    throw new InvalidOperationException("remote Podman state cannot be verified from the local filesystem");

                default:
                    throw new InvalidOperationException($"podman reported an invalid remote-service mode: {Display(serviceIsRemote)}");
            }

            var rootless = await QuerySingleValue(
                podmanExecutable,
                ["info", "--format", "{{.Host.Security.Rootless}}"],
                "podman info failed while reading rootless mode",
                runProcess,
                cancellationToken).ConfigureAwait(false);
            switch (rootless)
            {
                case "true":
                    break;

                case "false":
                    return await WriteHealthy(
                        output,
                        quiet,
                        "podman is rootful; the shared rootless namespace does not exist",
                        cancellationToken).ConfigureAwait(false);

                default:
                    throw new InvalidOperationException($"podman reported an invalid rootless mode: {Display(rootless)}");
            }

            var reportedRunRoot = await QuerySingleValue(
                podmanExecutable,
                ["info", "--format", "{{.Store.RunRoot}}"],
                "podman info failed while reading the run root",
                runProcess,
                cancellationToken).ConfigureAwait(false);
            if (!Path.IsPathFullyQualified(reportedRunRoot))
            {
                throw new InvalidOperationException($"podman reported an invalid run root: {Display(reportedRunRoot)}");
            }

            var runRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(reportedRunRoot));
            if (string.Equals(runRoot, Path.GetPathRoot(runRoot), StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"podman reported an unsafe run root: {Display(reportedRunRoot)}");
            }
            if (!DirectoryEntryExists(runRoot))
            {
                throw new InvalidOperationException($"podman run root does not exist: {runRoot}");
            }

            var runningContainerOutput = await Query(
                podmanExecutable,
                ["ps", "--quiet"],
                "podman ps failed while checking running containers",
                runProcess,
                cancellationToken).ConfigureAwait(false);
            var runningContainerCount = SplitLines(runningContainerOutput).Length;

            var networksDirectory = Path.Combine(runRoot, "networks");
            var networksDirectoryExists = DirectoryEntryExists(networksDirectory);
            var aardvarkDirectory = Path.Combine(networksDirectory, "aardvark-dns");
            var pidFile = Path.Combine(aardvarkDirectory, "aardvark.pid");
            var refCountFile = Path.Combine(networksDirectory, "rootless-netns", "ref-count");
            var currentUserId = ReadEffectiveUserId(Path.Combine(procRoot, "self", "status"));
            var runRootDaemons = FindRunRootDaemons(procRoot, currentUserId, aardvarkDirectory);
            var runRootDaemonPids = runRootDaemons.Keys.ToList();
            List<string> reasons = [];

            var activeNamespacePath = Path.Combine(networksDirectory, "rootless-netns", "rootless-netns");
            if (runRootDaemonPids.Count > 0 && !FileSystemEntryExists(activeNamespacePath))
            {
                reasons.Add($"aardvark-dns PID(s) {string.Join(' ', runRootDaemonPids)} are running but {activeNamespacePath} does not exist");
            }
            else if (runRootDaemonPids.Count > 0)
            {
                if (statExecutable is null)
                {
                    throw new InvalidOperationException("GNU stat is required to compare network namespace identities");
                }

                foreach (var processId in runRootDaemonPids)
                {
                    var processNamespacePath = Path.Combine(procRoot, processId.ToString(CultureInfo.InvariantCulture), "ns", "net");
                    var namespaceMatches = await NamespaceMatches(
                        statExecutable,
                        activeNamespacePath,
                        processNamespacePath,
                        runProcess,
                        cancellationToken).ConfigureAwait(false);
                    if (!namespaceMatches)
                    {
                        reasons.Add($"aardvark-dns PID {processId.ToString(CultureInfo.InvariantCulture)} runs in a different network namespace than {activeNamespacePath}");
                    }
                }
            }

            var pidFileValue = ReadOptionalFileValue(pidFile, "PID");
            var aardvarkPid = ReadAndValidatePidFile(pidFile, pidFileValue, procRoot, runRootDaemonPids, aardvarkDirectory, reasons);
            if (pidFileValue is null && runRootDaemonPids.Count > 0)
            {
                reasons.Add($"live aardvark-dns PID(s) {string.Join(' ', runRootDaemonPids)} use {aardvarkDirectory}, but {pidFile} does not exist");
            }

            if (runRootDaemonPids.Count > 1)
            {
                reasons.Add($"multiple aardvark-dns processes use {aardvarkDirectory}: {string.Join(' ', runRootDaemonPids)}");
            }

            string? refCountValue = null;
            if (runRootDaemonPids.Count > 0)
            {
                if (!FileSystemEntryExists(refCountFile))
                {
                    reasons.Add($"aardvark-dns PID(s) {string.Join(' ', runRootDaemonPids)} are running but {refCountFile} does not exist");
                }
                else
                {
                    refCountValue = ReadFileValue(refCountFile, "reference count");
                    if (!uint.TryParse(refCountValue, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedRefCount))
                    {
                        throw new InvalidOperationException($"{refCountFile} contains an invalid reference count: {Display(refCountValue)}");
                    }

                    if (parsedRefCount == 0)
                    {
                        reasons.Add($"aardvark-dns PID(s) {string.Join(' ', runRootDaemonPids)} are running while the shared namespace reference count is 0");
                    }
                }
            }

            var configEntries = ReadConfigEntries(aardvarkDirectory);
            string? knownNetworkOutput = null;
            if (configEntries.Length > 0)
            {
                knownNetworkOutput = await Query(
                    podmanExecutable,
                    ["network", "ls", "--format", "{{.Name}}"],
                    "podman network ls failed while checking aardvark-dns configuration",
                    runProcess,
                    cancellationToken).ConfigureAwait(false);
                var knownNetworks = SplitLines(knownNetworkOutput).ToHashSet(StringComparer.Ordinal);
                var orphanConfigs = configEntries
                    .Where(entry => !knownNetworks.Contains(RemoveInternalSuffix(entry)))
                    .ToArray();
                if (orphanConfigs.Length > 0)
                {
                    reasons.Add($"aardvark-dns holds config for networks Podman no longer has: {string.Join(' ', orphanConfigs)}");
                }
            }

            if (configEntries.Length > 0 && runRootDaemonPids.Count == 0)
            {
                reasons.Add($"{aardvarkDirectory} contains config but {pidFile} does not identify a live aardvark-dns daemon");
            }

            if (reasons.Count == 0)
            {
                var finalRunningContainerOutput = await Query(
                    podmanExecutable,
                    ["ps", "--quiet"],
                    "podman ps failed while rechecking running containers",
                    runProcess,
                    cancellationToken).ConfigureAwait(false);
                if (!string.Equals(runningContainerOutput, finalRunningContainerOutput, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("running container state changed during inspection");
                }

                var finalRunRootDaemons = FindRunRootDaemons(procRoot, currentUserId, aardvarkDirectory);
                if (!runRootDaemons.SequenceEqual(finalRunRootDaemons))
                {
                    throw new InvalidOperationException("aardvark-dns process identity changed during inspection");
                }

                var finalPidFileValue = ReadOptionalFileValue(pidFile, "PID");
                if (!string.Equals(pidFileValue, finalPidFileValue, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("aardvark-dns PID file changed during inspection");
                }

                var finalRefCountValue = runRootDaemonPids.Count > 0
                    ? ReadOptionalFileValue(refCountFile, "reference count")
                    : null;
                if (!string.Equals(refCountValue, finalRefCountValue, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("rootless network namespace reference count changed during inspection");
                }

                var finalConfigEntries = ReadConfigEntries(aardvarkDirectory);
                if (!configEntries.SequenceEqual(finalConfigEntries, StringComparer.Ordinal))
                {
                    throw new InvalidOperationException("aardvark-dns configuration changed during inspection");
                }

                if (configEntries.Length > 0)
                {
                    var finalKnownNetworkOutput = await Query(
                        podmanExecutable,
                        ["network", "ls", "--format", "{{.Name}}"],
                        "podman network ls failed while rechecking aardvark-dns configuration",
                        runProcess,
                        cancellationToken).ConfigureAwait(false);
                    if (!string.Equals(knownNetworkOutput, finalKnownNetworkOutput, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Podman network state changed during inspection");
                    }
                }

                var finalReportedRunRoot = await QuerySingleValue(
                    podmanExecutable,
                    ["info", "--format", "{{.Store.RunRoot}}"],
                    "podman info failed while rechecking the run root",
                    runProcess,
                    cancellationToken).ConfigureAwait(false);
                var finalRunRoot = Path.IsPathFullyQualified(finalReportedRunRoot)
                    ? Path.TrimEndingDirectorySeparator(Path.GetFullPath(finalReportedRunRoot))
                    : string.Empty;
                if (!string.Equals(runRoot, finalRunRoot, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Podman run root changed during inspection");
                }
                if (!DirectoryEntryExists(finalRunRoot)
                    || networksDirectoryExists != DirectoryEntryExists(Path.Combine(finalRunRoot, "networks")))
                {
                    throw new InvalidOperationException("Podman run-root directory state changed during inspection");
                }

                var lastRunRootDaemons = FindRunRootDaemons(procRoot, currentUserId, aardvarkDirectory);
                if (!runRootDaemons.SequenceEqual(lastRunRootDaemons))
                {
                    throw new InvalidOperationException("aardvark-dns process identity changed during inspection");
                }

                if (runRootDaemonPids.Count > 0)
                {
                    if (!FileSystemEntryExists(activeNamespacePath) || statExecutable is null)
                    {
                        throw new InvalidOperationException("active rootless network namespace cannot be revalidated");
                    }

                    foreach (var processId in runRootDaemonPids)
                    {
                        var processNamespacePath = Path.Combine(procRoot, processId.ToString(CultureInfo.InvariantCulture), "ns", "net");
                        if (!await NamespaceMatches(
                                statExecutable,
                                activeNamespacePath,
                                processNamespacePath,
                                runProcess,
                                cancellationToken).ConfigureAwait(false))
                        {
                            throw new InvalidOperationException("network namespace identity changed during inspection");
                        }
                    }
                }

                return await WriteHealthy(
                    output,
                    quiet,
                    $"healthy (aardvark-dns pid {aardvarkPid?.ToString(CultureInfo.InvariantCulture) ?? "none"}, {runningContainerCount.ToString(CultureInfo.InvariantCulture)} container(s) running)",
                    cancellationToken).ConfigureAwait(false);
            }

            await error.WriteLineAsync("podman dns preflight: stale aardvark-dns state detected".AsMemory(), cancellationToken).ConfigureAwait(false);
            foreach (var reason in reasons)
            {
                await error.WriteLineAsync($"  - {RepoConfigToolApplication.EscapeControlCharacters(reason)}".AsMemory(), cancellationToken).ConfigureAwait(false);
            }

            await error.WriteLineAsync("No changes were made. Stop all rootless Podman containers and other Podman operations,".AsMemory(), cancellationToken).ConfigureAwait(false);
            await error.WriteLineAsync("then follow the scoped recovery guidance in docs/TEST_GUIDELINES.md.".AsMemory(), cancellationToken).ConfigureAwait(false);
            return 1;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException
            or InvalidOperationException
            or TimeoutException
            or UnauthorizedAccessException
            or DecoderFallbackException)
        {
            return await WriteFailure(error, exception.Message, cancellationToken).ConfigureAwait(false);
        }
    }

    private static int? ReadAndValidatePidFile(
        string pidFile,
        string? value,
        string procRoot,
        List<int> runRootDaemonPids,
        string aardvarkDirectory,
        List<string> reasons)
    {
        if (value is null)
        {
            return null;
        }

        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var processId) || processId <= 0)
        {
            reasons.Add($"{pidFile} contains an invalid PID: {Display(value)}");
            return null;
        }

        if (runRootDaemonPids.Contains(processId))
        {
            return processId;
        }

        var processName = TryReadProcessName(Path.Combine(procRoot, processId.ToString(CultureInfo.InvariantCulture)));
        if (processName is null)
        {
            reasons.Add($"{pidFile} references PID {processId.ToString(CultureInfo.InvariantCulture)}, which is not running");
        }
        else if (!string.Equals(processName, "aardvark-dns", StringComparison.Ordinal))
        {
            reasons.Add($"{pidFile} references PID {processId.ToString(CultureInfo.InvariantCulture)}, which is {processName}");
        }
        else
        {
            reasons.Add($"{pidFile} references aardvark-dns PID {processId.ToString(CultureInfo.InvariantCulture)}, which is not configured for {aardvarkDirectory}");
        }

        return processId;
    }

    private static SortedDictionary<int, ulong> FindRunRootDaemons(string procRoot, uint currentUserId, string aardvarkDirectory)
    {
        if (!DirectoryEntryExists(procRoot))
        {
            throw new InvalidOperationException($"process filesystem does not exist: {procRoot}");
        }

        SortedDictionary<int, ulong> processes = [];
        foreach (var processDirectory in Directory.EnumerateDirectories(procRoot))
        {
            var processDirectoryName = Path.GetFileName(processDirectory);
            if (!int.TryParse(processDirectoryName, NumberStyles.None, CultureInfo.InvariantCulture, out var processId) || processId <= 0)
            {
                continue;
            }

            var processName = TryReadProcessName(processDirectory);
            if (!string.Equals(processName, "aardvark-dns", StringComparison.Ordinal))
            {
                continue;
            }

            uint processUserId;
            try
            {
                processUserId = ReadEffectiveUserId(Path.Combine(processDirectory, "status"));
            }
            catch (Exception exception) when (ProcessDisappeared(processDirectory, exception))
            {
                continue;
            }

            if (processUserId != currentUserId)
            {
                continue;
            }

            byte[] commandLine;
            try
            {
                commandLine = File.ReadAllBytes(Path.Combine(processDirectory, "cmdline"));
            }
            catch (Exception exception) when (ProcessDisappeared(processDirectory, exception))
            {
                continue;
            }

            if (UsesConfigDirectory(ParseCommandLine(commandLine), aardvarkDirectory))
            {
                processes.Add(processId, ReadProcessStartTime(processDirectory));
            }
        }

        return processes;
    }

    private static string? TryReadProcessName(string processDirectory)
    {
        try
        {
            return ReadFileValue(Path.Combine(processDirectory, "comm"), "process name");
        }
        catch (Exception exception) when (exception is FileNotFoundException
            or DirectoryNotFoundException
            or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static bool ProcessDisappeared(string processDirectory, Exception exception)
    {
        if (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return true;
        }

        try
        {
            return !FileSystemEntryExists(processDirectory);
        }
        catch (Exception pathException) when (pathException is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static uint ReadEffectiveUserId(string statusPath)
    {
        var status = File.ReadAllText(statusPath);
        var uidLine = SplitLines(status).SingleOrDefault(line => line.StartsWith("Uid:", StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"{statusPath} does not contain a Uid field");
        var values = uidLine[4..].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (values.Length < 2 || !uint.TryParse(values[1], NumberStyles.None, CultureInfo.InvariantCulture, out var effectiveUserId))
        {
            throw new InvalidOperationException($"{statusPath} contains an invalid Uid field");
        }

        return effectiveUserId;
    }

    private static ulong ReadProcessStartTime(string processDirectory)
    {
        var statPath = Path.Combine(processDirectory, "stat");
        var stat = File.ReadAllText(statPath).TrimEnd('\r', '\n');
        var commandEnd = stat.LastIndexOf(')');
        if (commandEnd < 0 || commandEnd + 2 >= stat.Length)
        {
            throw new InvalidOperationException($"{statPath} has an invalid process stat record");
        }

        var fields = stat[(commandEnd + 2)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length <= 19 || !ulong.TryParse(fields[19], NumberStyles.None, CultureInfo.InvariantCulture, out var startTime))
        {
            throw new InvalidOperationException($"{statPath} has an invalid process start time");
        }

        return startTime;
    }

    private static List<string> ParseCommandLine(byte[] value)
    {
        if (value.Length == 0)
        {
            return [];
        }

        if (value[^1] != 0)
        {
            throw new InvalidOperationException("aardvark-dns command line is not NUL terminated");
        }

        List<string> arguments = [];
        var start = 0;
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] != 0)
            {
                continue;
            }

            arguments.Add(StrictUtf8.GetString(value.AsSpan(start, index - start)));
            start = index + 1;
        }

        return arguments;
    }

    private static bool UsesConfigDirectory(List<string> arguments, string aardvarkDirectory)
    {
        for (var index = 0; index < arguments.Count; index++)
        {
            if (string.Equals(arguments[index], $"--config={aardvarkDirectory}", StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(arguments[index], "--config", StringComparison.Ordinal)
                && index + 1 < arguments.Count
                && string.Equals(arguments[index + 1], aardvarkDirectory, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string[] ReadConfigEntries(string aardvarkDirectory)
    {
        if (!DirectoryEntryExists(aardvarkDirectory))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(aardvarkDirectory)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(name => !string.IsNullOrEmpty(name) && !string.Equals(name, "aardvark.pid", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static string RemoveInternalSuffix(string configEntry) =>
        configEntry.EndsWith("%int", StringComparison.Ordinal) ? configEntry[..^4] : configEntry;

    private static string ReadFileValue(string path, string description)
    {
        var value = File.ReadAllText(path).TrimEnd('\r', '\n');
        if (value.Length == 0 || value.Contains('\r', StringComparison.Ordinal) || value.Contains('\n', StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{path} contains an invalid {description}");
        }

        return value;
    }

    private static string? ReadOptionalFileValue(string path, string description) =>
        FileSystemEntryExists(path) ? ReadFileValue(path, description) : null;

    private static bool FileSystemEntryExists(string path)
    {
        try
        {
            _ = File.GetAttributes(path);
            return true;
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return false;
        }
    }

    private static bool DirectoryEntryExists(string path)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.Directory) == 0)
            {
                throw new InvalidOperationException($"{path} is not a directory");
            }

            return true;
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return false;
        }
    }

    private static string[] SplitLines(string value) =>
        value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

    private static async Task<string> QuerySingleValue(
        string podmanExecutable,
        IReadOnlyList<string> arguments,
        string failureMessage,
        Func<ProcessStartInfo, CancellationToken, Task<PodmanCommandResult>> runProcess,
        CancellationToken cancellationToken)
    {
        var output = await Query(podmanExecutable, arguments, failureMessage, runProcess, cancellationToken).ConfigureAwait(false);
        var value = output.TrimEnd('\r', '\n');
        if (value.Length == 0 || value.Contains('\r', StringComparison.Ordinal) || value.Contains('\n', StringComparison.Ordinal))
        {
            throw new InvalidOperationException(failureMessage);
        }

        return value;
    }

    private static async Task<bool> NamespaceMatches(
        string statExecutable,
        string activeNamespacePath,
        string processNamespacePath,
        Func<ProcessStartInfo, CancellationToken, Task<PodmanCommandResult>> runProcess,
        CancellationToken cancellationToken)
    {
        Exception? lastError = null;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                var firstActiveIdentity = await QueryNamespaceIdentity(statExecutable, activeNamespacePath, runProcess, cancellationToken).ConfigureAwait(false);
                var processIdentity = await QueryNamespaceIdentity(statExecutable, processNamespacePath, runProcess, cancellationToken).ConfigureAwait(false);
                var secondActiveIdentity = await QueryNamespaceIdentity(statExecutable, activeNamespacePath, runProcess, cancellationToken).ConfigureAwait(false);
                if (string.Equals(firstActiveIdentity, secondActiveIdentity, StringComparison.Ordinal))
                {
                    return string.Equals(firstActiveIdentity, processIdentity, StringComparison.Ordinal);
                }

                lastError = new InvalidOperationException("active rootless network namespace changed during inspection");
            }
            catch (Exception exception) when (exception is IOException
                or InvalidOperationException
                or TimeoutException
                or UnauthorizedAccessException)
            {
                lastError = exception;
            }
        }

        throw new InvalidOperationException("could not compare rootless network namespace identities safely", lastError);
    }

    private static async Task<string> QueryNamespaceIdentity(
        string statExecutable,
        string path,
        Func<ProcessStartInfo, CancellationToken, Task<PodmanCommandResult>> runProcess,
        CancellationToken cancellationToken)
    {
        ProcessStartInfo startInfo = new(statExecutable)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.Environment["LC_ALL"] = "C";
        startInfo.ArgumentList.Add("-L");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("%d:%i");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add(path);

        var result = await runProcess(startInfo, cancellationToken).ConfigureAwait(false);
        var identity = result.StandardOutput.TrimEnd('\r', '\n');
        var identityParts = identity.Split(':');
        if (result.ExitCode != 0
            || identity.Length == 0
            || identity.Contains('\r', StringComparison.Ordinal)
            || identity.Contains('\n', StringComparison.Ordinal)
            || identityParts.Length != 2
            || !ulong.TryParse(identityParts[0], NumberStyles.None, CultureInfo.InvariantCulture, out _)
            || !ulong.TryParse(identityParts[1], NumberStyles.None, CultureInfo.InvariantCulture, out _))
        {
            throw new InvalidOperationException($"could not read namespace identity for {path}");
        }

        return identity;
    }

    private static async Task<string> Query(
        string podmanExecutable,
        IReadOnlyList<string> arguments,
        string failureMessage,
        Func<ProcessStartInfo, CancellationToken, Task<PodmanCommandResult>> runProcess,
        CancellationToken cancellationToken)
    {
        ProcessStartInfo startInfo = new(podmanExecutable)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.Environment.Remove("CONTAINER_CONNECTION");
        startInfo.Environment.Remove("CONTAINER_HOST");
        startInfo.ArgumentList.Add("--remote=false");
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        var result = await runProcess(startInfo, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0 || result.StandardOutput.Length > MaximumPodmanOutputLength)
        {
            throw new InvalidOperationException(failureMessage);
        }

        return result.StandardOutput;
    }

    private static async Task<PodmanCommandResult> RunProcess(ProcessStartInfo startInfo, CancellationToken cancellationToken)
    {
        Process process;
        try
        {
            process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("could not start podman");
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException("could not start podman", exception);
        }

        using (process)
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            timeout.CancelAfter(PodmanTimeout);
            try
            {
                var standardOutput = process.StandardOutput.ReadToEndAsync(timeout.Token);
                var standardError = process.StandardError.ReadToEndAsync(timeout.Token);
                await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
                return new PodmanCommandResult(
                    process.ExitCode,
                    await standardOutput.ConfigureAwait(false),
                    await standardError.ConfigureAwait(false));
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                await Stop(process).ConfigureAwait(false);
                throw new TimeoutException("host inspection command timed out", exception);
            }
            catch (OperationCanceledException)
            {
                await Stop(process).ConfigureAwait(false);
                throw;
            }
        }
    }

    private static async Task Stop(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill();
        }

        using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await process.WaitForExitAsync(cleanupTimeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // The command-owned process was signalled; cleanup must not block the caller indefinitely.
        }
    }

    private static async Task<int> WriteHealthy(
        TextWriter output,
        bool quiet,
        string message,
        CancellationToken cancellationToken)
    {
        if (!quiet)
        {
            await output.WriteLineAsync($"podman dns preflight: {message}".AsMemory(), cancellationToken).ConfigureAwait(false);
        }

        return 0;
    }

    private static async Task<int> WriteFailure(TextWriter error, string message, CancellationToken cancellationToken)
    {
        var escapedMessage = RepoConfigToolApplication.EscapeControlCharacters(message);
        await error.WriteLineAsync($"podman dns preflight: unable to verify safely: {escapedMessage}".AsMemory(), cancellationToken).ConfigureAwait(false);
        return 1;
    }

    private static string Display(string value) =>
        value.Length == 0 ? "empty" : RepoConfigToolApplication.EscapeControlCharacters(value);
}
