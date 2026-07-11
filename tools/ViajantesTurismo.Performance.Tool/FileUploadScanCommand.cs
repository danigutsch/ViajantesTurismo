using System.Diagnostics;
using System.Globalization;
using System.Net;

namespace ViajantesTurismo.Performance.Tool;

internal static class FileUploadScanCommand
{
    private const string ReadyFileVariable = "VT_UPLOAD_BENCHMARK_READY_FILE";

    private const string DefaultProfile = "smoke";

    private const string DefaultUseDocker = "0";

    private const string DefaultDockerK6Image = "grafana/k6:0.49.0@sha256:8cd78f9d0de5f50bc8821cceecf356d5d9e839e6611c226a3fcf13c591080fbd";

    private const string DefaultPayloadBytes = "262144";

    private const long MaxPayloadBytes = 16_777_216;

    private const string DefaultResultsDirectory = "tests/performance/results";

    private const string UploadBenchmarkProject = "benchmarks/ViajantesTurismo.FileUpload.BenchmarkHost/ViajantesTurismo.FileUpload.BenchmarkHost.csproj";

    private const string K6ScenarioPath = "tests/performance/k6/scenarios/file-upload-scan.js";

    private const string DockerK6ScenarioPath = "/k6/scenarios/file-upload-scan.js";

    private const string DockerK6SummaryPath = "/results/k6-summary.json";

    private static readonly string[] ControlledEnvironmentVariables =
    [
        "VT_UPLOAD_BASE_URL",
        "VT_K6_PROFILE",
        "VT_K6_VUS",
        "VT_K6_DURATION",
        "VT_UPLOAD_PAYLOAD_BYTES",
        "K6_NO_USAGE_REPORT",
    ];

    public static async Task<int> Run(IReadOnlyCollection<string> k6Arguments, TextWriter output)
    {
        var repositoryRoot = GetRepositoryRoot();
        var profile = GetEnvironmentValue("VT_K6_PROFILE", DefaultProfile);
        var useDocker = GetEnvironmentValue("VT_K6_USE_DOCKER", DefaultUseDocker);
        var dockerK6Image = GetEnvironmentValue("VT_K6_DOCKER_IMAGE", DefaultDockerK6Image);
        var payloadBytes = GetEnvironmentValue("VT_UPLOAD_PAYLOAD_BYTES", DefaultPayloadBytes);
        var resultsDirectory = NormalizeResultsDirectory(GetEnvironmentValue("VT_K6_RESULTS_DIR", DefaultResultsDirectory));
        var resolvedUseDocker = ResolveUseDocker(useDocker);

        ValidateProfile(profile);
        ValidatePayloadBytes(payloadBytes);
        ValidateK6Arguments(k6Arguments.ToArray());
        ValidateDockerImage(dockerK6Image, resolvedUseDocker);
        var runnerPath = ResolveRunnerPath(resolvedUseDocker);
        var dotnetPath = FindCommandPath("dotnet")
            ?? throw new InvalidOperationException("dotnet is required to start the file upload benchmark host.");

        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var runDirectory = Path.Combine(resultsDirectory, "file-upload-scan-" + timestamp);
        var absoluteRunDirectory = Path.Combine(repositoryRoot, ToPlatformPath(runDirectory));
        var relativeReadyFile = Path.Combine(runDirectory, "host.url");
        var absoluteReadyFile = Path.Combine(repositoryRoot, ToPlatformPath(relativeReadyFile));
        var hostOutputLog = Path.Combine(absoluteRunDirectory, "host.stdout.log");
        var hostErrorLog = Path.Combine(absoluteRunDirectory, "host.stderr.log");
        var summaryFile = Path.Combine(runDirectory, "k6-summary.json").Replace(Path.DirectorySeparatorChar, '/');

        Directory.CreateDirectory(absoluteRunDirectory);

        await output.WriteLineAsync($"Starting file upload benchmark host. Results: {runDirectory.Replace(Path.DirectorySeparatorChar, '/')}").ConfigureAwait(false);

        using var hostProcess = StartHost(dotnetPath, repositoryRoot, absoluteReadyFile, resolvedUseDocker);
        var hostOutputTask = CopyToFile(hostProcess.StandardOutput, hostOutputLog);
        var hostErrorTask = CopyToFile(hostProcess.StandardError, hostErrorLog);

        try
        {
            var rawBaseUrl = await WaitForReadyFile(hostProcess, absoluteReadyFile, hostOutputLog, hostErrorLog).ConfigureAwait(false);
            var hostBaseUrl = ToHostUrl(rawBaseUrl);
            await WaitForHealth(hostBaseUrl, hostOutputLog, hostErrorLog).ConfigureAwait(false);

            await output.WriteLineAsync($"Host ready: {hostBaseUrl}").ConfigureAwait(false);

            return resolvedUseDocker == "0"
                ? await RunLocalK6(runnerPath, repositoryRoot, hostBaseUrl, profile, payloadBytes, summaryFile, k6Arguments).ConfigureAwait(false)
                : await RunDockerK6(runnerPath, repositoryRoot, ToDockerUrl(rawBaseUrl), profile, payloadBytes, absoluteRunDirectory, dockerK6Image, k6Arguments).ConfigureAwait(false);
        }
        finally
        {
            await StopHost(hostProcess).ConfigureAwait(false);
            await hostOutputTask.ConfigureAwait(false);
            await hostErrorTask.ConfigureAwait(false);
        }
    }

    private static Process StartHost(string dotnetPath, string repositoryRoot, string readyFile, string useDocker)
    {
        var startInfo = new ProcessStartInfo(dotnetPath)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = repositoryRoot,
        };

        var hostName = useDocker == "1" ? IPAddress.Any.ToString() : IPAddress.Loopback.ToString();
        var hostUrl = Uri.UriSchemeHttp + "://" + hostName + ":0";

        startInfo.Environment[ReadyFileVariable] = readyFile;
        AddArguments(
            startInfo,
            "run",
            "--project",
            UploadBenchmarkProject,
            "-c",
            "Release",
            "--",
            "--urls",
            hostUrl);

        return StartProcess(startInfo, dotnetPath);
    }

    private static async Task<string> WaitForReadyFile(Process hostProcess, string readyFile, string outputLog, string errorLog)
    {
        for (var attempt = 0; attempt < 60; attempt++)
        {
            if (File.Exists(readyFile))
            {
                var value = (await File.ReadAllTextAsync(readyFile).ConfigureAwait(false)).Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            if (hostProcess.HasExited)
            {
                throw new InvalidOperationException($"File upload benchmark host exited before it became ready. See {outputLog} and {errorLog}.");
            }

            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }

        throw new InvalidOperationException($"File upload benchmark host did not write {readyFile}. See {outputLog} and {errorLog}.");
    }

    private static async Task WaitForHealth(string hostBaseUrl, string outputLog, string errorLog)
    {
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        var healthUrl = new Uri(hostBaseUrl.TrimEnd('/') + "/health", UriKind.Absolute);
        for (var attempt = 0; attempt < 30; attempt++)
        {
            try
            {
                using var response = await client.GetAsync(healthUrl).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                // Retry while Kestrel finishes binding the randomized port.
            }

            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }

        throw new InvalidOperationException($"File upload benchmark host is not reachable at {healthUrl}. See {outputLog} and {errorLog}.");
    }

    private static async Task<int> RunLocalK6(
        string k6Path,
        string repositoryRoot,
        string hostBaseUrl,
        string profile,
        string payloadBytes,
        string summaryFile,
        IReadOnlyCollection<string> k6Arguments)
    {
        var startInfo = new ProcessStartInfo(k6Path)
        {
            UseShellExecute = false,
            WorkingDirectory = repositoryRoot,
        };
        ApplyMinimalK6Environment(startInfo);

        AddK6RunArguments(startInfo, summaryFile, hostBaseUrl, profile, payloadBytes, k6Arguments);
        return await RunProcess(startInfo, k6Path).ConfigureAwait(false);
    }

    private static async Task<int> RunDockerK6(
        string dockerPath,
        string repositoryRoot,
        string dockerBaseUrl,
        string profile,
        string payloadBytes,
        string absoluteRunDirectory,
        string dockerK6Image,
        IReadOnlyCollection<string> k6Arguments)
    {
        var startInfo = new ProcessStartInfo(dockerPath)
        {
            UseShellExecute = false,
            WorkingDirectory = repositoryRoot,
        };

        AddArguments(
            startInfo,
            "run",
            "--rm",
            "--pull=never",
            "--add-host",
            "host.docker.internal:host-gateway",
            "--mount",
            "type=bind,source=" + Path.Combine(repositoryRoot, "tests", "performance", "k6") + ",target=/k6,readonly",
            "--mount",
            "type=bind,source=" + absoluteRunDirectory + ",target=/results",
            "-w",
            "/k6",
            dockerK6Image,
            "run",
            "--no-usage-report",
            "--summary-export",
            DockerK6SummaryPath,
            "-e",
            "VT_UPLOAD_BASE_URL=" + dockerBaseUrl,
            "-e",
            "VT_K6_PROFILE=" + profile,
            "-e",
            "VT_UPLOAD_PAYLOAD_BYTES=" + payloadBytes,
            "-e",
            "K6_NO_USAGE_REPORT=true");

        AddOptionalEnvironment(startInfo, "VT_K6_VUS");
        AddOptionalEnvironment(startInfo, "VT_K6_DURATION");

        foreach (var argument in k6Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.ArgumentList.Add(DockerK6ScenarioPath);

        return await RunProcess(startInfo, dockerPath).ConfigureAwait(false);
    }

    private static void AddK6RunArguments(
        ProcessStartInfo startInfo,
        string summaryFile,
        string hostBaseUrl,
        string profile,
        string payloadBytes,
        IReadOnlyCollection<string> k6Arguments)
    {
        AddArguments(
            startInfo,
            "run",
            "--no-usage-report",
            "--summary-export",
            summaryFile,
            "-e",
            "VT_UPLOAD_BASE_URL=" + hostBaseUrl,
            "-e",
            "VT_K6_PROFILE=" + profile,
            "-e",
            "VT_UPLOAD_PAYLOAD_BYTES=" + payloadBytes,
            "-e",
            "K6_NO_USAGE_REPORT=true");

        AddOptionalEnvironment(startInfo, "VT_K6_VUS");
        AddOptionalEnvironment(startInfo, "VT_K6_DURATION");

        foreach (var argument in k6Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.ArgumentList.Add(K6ScenarioPath);
    }

    private static void AddOptionalEnvironment(ProcessStartInfo startInfo, string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (!string.IsNullOrWhiteSpace(value))
        {
            AddArguments(startInfo, "-e", name + "=" + value);
        }
    }

    private static void ApplyMinimalK6Environment(ProcessStartInfo startInfo)
    {
        var pathExtensions = Environment.GetEnvironmentVariable("PATHEXT");
        var systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
        var windowsDirectory = Environment.GetEnvironmentVariable("WINDIR");

        startInfo.Environment.Clear();

        if (!string.IsNullOrWhiteSpace(pathExtensions))
        {
            startInfo.Environment["PATHEXT"] = pathExtensions;
        }

        if (!string.IsNullOrWhiteSpace(systemRoot))
        {
            startInfo.Environment["SystemRoot"] = systemRoot;
        }

        if (!string.IsNullOrWhiteSpace(windowsDirectory))
        {
            startInfo.Environment["WINDIR"] = windowsDirectory;
        }
    }

    private static string ResolveUseDocker(string useDocker)
    {
        if (useDocker is "0" or "1")
        {
            return useDocker;
        }

        throw new ArgumentException("VT_K6_USE_DOCKER must be 0 or 1. Docker mode is explicit opt-in only.");
    }

    private static string ResolveRunnerPath(string useDocker)
    {
        if (useDocker == "0")
        {
            return FindCommandPath("k6")
                ?? throw new InvalidOperationException("k6 is required. Install k6 locally or explicitly opt into Docker with VT_K6_USE_DOCKER=1.");
        }

        return FindCommandPath("docker")
            ?? throw new InvalidOperationException("docker is required when VT_K6_USE_DOCKER=1.");
    }

    private static async Task<int> RunProcess(ProcessStartInfo startInfo, string fileName)
    {
        using var process = StartProcess(startInfo, fileName);
        await process.WaitForExitAsync().ConfigureAwait(false);
        return process.ExitCode;
    }

    private static Process StartProcess(ProcessStartInfo startInfo, string fileName)
    {
        try
        {
            return Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start {fileName}.");
        }
        catch (Exception exception) when (exception is not InvalidOperationException)
        {
            throw new InvalidOperationException($"Failed to start {fileName}.", exception);
        }
    }

    private static async Task StopHost(Process hostProcess)
    {
        if (hostProcess.HasExited)
        {
            return;
        }

        hostProcess.Kill(entireProcessTree: true);
        await hostProcess.WaitForExitAsync().ConfigureAwait(false);
    }

    private static async Task CopyToFile(TextReader reader, string filePath)
    {
        using var stream = File.Create(filePath);
        using var writer = new StreamWriter(stream);
        var buffer = new char[8192];

        while (true)
        {
            var read = await reader.ReadAsync(buffer).ConfigureAwait(false);
            if (read == 0)
            {
                return;
            }

            await writer.WriteAsync(buffer.AsMemory(0, read)).ConfigureAwait(false);
        }
    }

    private static void AddArguments(ProcessStartInfo startInfo, params string[] arguments)
    {
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ViajantesTurismo.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root containing ViajantesTurismo.slnx.");
    }

    private static string GetEnvironmentValue(string name, string defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    private static string NormalizeResultsDirectory(string resultsDirectory)
    {
        if (Path.IsPathRooted(resultsDirectory) || resultsDirectory.Contains(':', StringComparison.Ordinal))
        {
            throw new ArgumentException("VT_K6_RESULTS_DIR must be relative to the repository root.");
        }

        var normalized = resultsDirectory.Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Contains("//", StringComparison.Ordinal))
        {
            throw new ArgumentException("VT_K6_RESULTS_DIR must stay inside the repository root and must not be empty.");
        }

        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(static segment => segment == ".."))
        {
            throw new ArgumentException("VT_K6_RESULTS_DIR must stay inside the repository root and must not contain .. segments.");
        }

        if (normalized != DefaultResultsDirectory
            && !normalized.StartsWith(DefaultResultsDirectory + "/", StringComparison.Ordinal))
        {
            throw new ArgumentException("VT_K6_RESULTS_DIR must stay under tests/performance/results.");
        }

        return normalized;
    }

    private static void ValidateProfile(string profile)
    {
        if (profile.Length == 0 || profile.Any(static value => !char.IsAsciiLetterOrDigit(value) && value is not '_' and not '-'))
        {
            throw new ArgumentException("VT_K6_PROFILE may contain only letters, numbers, underscores, and hyphens.");
        }
    }

    private static void ValidatePayloadBytes(string payloadBytes)
    {
        if (!long.TryParse(payloadBytes, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            || value <= 0
            || value > MaxPayloadBytes)
        {
            throw new ArgumentException("VT_UPLOAD_PAYLOAD_BYTES must be between 1 and 16777216.");
        }
    }

    private static void ValidateDockerImage(string dockerK6Image, string useDocker)
    {
        if (useDocker == "0")
        {
            return;
        }

        if (!dockerK6Image.Contains("@sha256:", StringComparison.Ordinal))
        {
            throw new ArgumentException("VT_K6_DOCKER_IMAGE must be pinned by digest, for example grafana/k6:0.49.0@sha256:<digest>.");
        }
    }

    internal static void ValidateK6Arguments(string[] arguments)
    {
        var allowRemoteOutput = IsEnabled("VT_K6_ALLOW_REMOTE_OUTPUT");
        var allowHttpDebug = IsEnabled("VT_K6_ALLOW_HTTP_DEBUG");

        for (var index = 0; index < arguments.Length; index++)
        {
            var argument = arguments[index];
            if (IsBlockedFlag(argument, "--include-system-env-vars"))
            {
                throw new ArgumentException("--include-system-env-vars is not allowed for repository k6 runs.");
            }

            if (!allowHttpDebug && IsBlockedFlag(argument, "--http-debug"))
            {
                throw new ArgumentException("--http-debug can expose request and response data. Set VT_K6_ALLOW_HTTP_DEBUG=1 for explicit local debugging.");
            }

            if (!allowRemoteOutput && (IsBlockedFlag(argument, "--out") || argument == "-o" || argument.StartsWith("-o=", StringComparison.Ordinal)))
            {
                throw new ArgumentException("Custom k6 outputs are disabled by default. Set VT_K6_ALLOW_REMOTE_OUTPUT=1 after reviewing output destination and credentials.");
            }

            if (IsBlockedFlag(argument, "--insecure-skip-tls-verify"))
            {
                throw new ArgumentException("--insecure-skip-tls-verify is not allowed for repository k6 runs.");
            }

            if (DefinesControlledEnvironment(argument, index, arguments))
            {
                throw new ArgumentException("Do not override repository-controlled k6 environment through -e/--env. Use documented VT_* environment variables before launching the tool.");
            }
        }
    }

    private static bool IsBlockedFlag(string argument, string flag)
    {
        return string.Equals(argument, flag, StringComparison.Ordinal)
            || argument.StartsWith(flag + "=", StringComparison.Ordinal);
    }

    private static bool DefinesControlledEnvironment(string argument, int index, string[] arguments)
    {
        if (argument is "-e" or "--env")
        {
            return index + 1 < arguments.Length && IsControlledEnvironmentVariable(arguments[index + 1]);
        }

        if (argument.StartsWith("-e=", StringComparison.Ordinal))
        {
            return IsControlledEnvironmentVariable(argument[3..]);
        }

        if (argument.StartsWith("--env=", StringComparison.Ordinal))
        {
            return IsControlledEnvironmentVariable(argument[6..]);
        }

        return false;
    }

    private static bool IsControlledEnvironmentVariable(string value)
    {
        var separatorIndex = value.IndexOf('=', StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            return false;
        }

        var name = value[..separatorIndex];
        return ControlledEnvironmentVariables.Contains(name, StringComparer.Ordinal);
    }

    private static bool IsEnabled(string name)
    {
        return string.Equals(Environment.GetEnvironmentVariable(name), "1", StringComparison.Ordinal);
    }

    private static string? FindCommandPath(string commandName)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var candidates = GetExecutableCandidates(commandName);
        return path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .SelectMany(directory => candidates.Select(candidate => Path.GetFullPath(Path.Combine(directory, candidate))))
            .FirstOrDefault(File.Exists);
    }

    private static string[] GetExecutableCandidates(string commandName)
    {
        if (!OperatingSystem.IsWindows())
        {
            return [commandName];
        }

        var pathExtensions = Environment.GetEnvironmentVariable("PATHEXT")?.Split(';', StringSplitOptions.RemoveEmptyEntries)
            ?? [".exe", ".cmd", ".bat"];

        return pathExtensions.Select(extension => commandName + extension).Prepend(commandName).ToArray();
    }

    private static string ToHostUrl(string url)
    {
        return url.Replace("0.0.0.0", "127.0.0.1", StringComparison.OrdinalIgnoreCase)
            .Replace("[::]", "127.0.0.1", StringComparison.OrdinalIgnoreCase);
    }

    private static string ToDockerUrl(string url)
    {
        return url.Replace("0.0.0.0", "host.docker.internal", StringComparison.OrdinalIgnoreCase)
            .Replace("127.0.0.1", "host.docker.internal", StringComparison.OrdinalIgnoreCase)
            .Replace("localhost", "host.docker.internal", StringComparison.OrdinalIgnoreCase)
            .Replace("[::]", "host.docker.internal", StringComparison.OrdinalIgnoreCase);
    }

    private static string ToPlatformPath(string relativePath)
    {
        return relativePath.Replace('/', Path.DirectorySeparatorChar);
    }
}
