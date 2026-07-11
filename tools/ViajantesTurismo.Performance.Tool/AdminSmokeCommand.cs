using System.Diagnostics;
using System.Globalization;

namespace ViajantesTurismo.Performance.Tool;

internal static class AdminSmokeCommand
{
    private const string DefaultProfile = "smoke";

    private const string DefaultUseDocker = "0";

    private const string DefaultDockerK6Image = "grafana/k6:0.49.0@sha256:8cd78f9d0de5f50bc8821cceecf356d5d9e839e6611c226a3fcf13c591080fbd";

    private const string DefaultResultsDirectory = "tests/performance/results";

    private const string K6ScenarioPath = "tests/performance/k6/scenarios/admin-smoke.js";

    private const string DockerK6ScenarioPath = "/k6/scenarios/admin-smoke.js";

    private const string DockerK6SummaryPath = "/results/k6-summary.json";

    private static readonly string[] ControlledEnvironmentVariables =
    [
        "VT_API_BASE_URL",
        "VT_K6_PROFILE",
        "VT_K6_VUS",
        "VT_K6_DURATION",
        "K6_NO_USAGE_REPORT",
    ];

    public static async Task<int> Run(IReadOnlyCollection<string> k6Arguments)
    {
        var repositoryRoot = GetRepositoryRoot();
        var profile = GetEnvironmentValue("VT_K6_PROFILE", DefaultProfile);
        var useDocker = GetEnvironmentValue("VT_K6_USE_DOCKER", DefaultUseDocker);
        var dockerK6Image = GetEnvironmentValue("VT_K6_DOCKER_IMAGE", DefaultDockerK6Image);
        var apiBaseUrl = NormalizeApiBaseUrl(GetRequiredEnvironmentValue("VT_API_BASE_URL"));
        var resultsDirectory = NormalizeResultsDirectory(GetEnvironmentValue("VT_K6_RESULTS_DIR", DefaultResultsDirectory));
        var resolvedUseDocker = ResolveUseDocker(useDocker);

        ValidateProfile(profile);
        ValidateTarget(apiBaseUrl);
        ValidateK6Arguments(k6Arguments.ToArray());
        ValidateDockerImage(dockerK6Image, resolvedUseDocker);
        var runnerPath = ResolveRunnerPath(resolvedUseDocker);

        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var runId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..8];
        var runDirectory = Path.Combine(resultsDirectory, "admin-smoke-" + profile + "-" + timestamp + "-" + runId);
        var absoluteRunDirectory = Path.Combine(repositoryRoot, ToPlatformPath(runDirectory));
        var summaryFile = Path.Combine(runDirectory, "k6-summary.json").Replace(Path.DirectorySeparatorChar, '/');

        Directory.CreateDirectory(absoluteRunDirectory);

        var runnerBaseUrl = resolvedUseDocker == "1" ? ToDockerUrl(apiBaseUrl) : apiBaseUrl;
        return resolvedUseDocker == "0"
            ? await RunLocalK6(runnerPath, repositoryRoot, runnerBaseUrl, profile, summaryFile, k6Arguments).ConfigureAwait(false)
            : await RunDockerK6(runnerPath, repositoryRoot, runnerBaseUrl, profile, absoluteRunDirectory, dockerK6Image, k6Arguments).ConfigureAwait(false);
    }

    private static async Task<int> RunLocalK6(
        string k6Path,
        string repositoryRoot,
        string apiBaseUrl,
        string profile,
        string summaryFile,
        IReadOnlyCollection<string> k6Arguments)
    {
        var startInfo = new ProcessStartInfo(k6Path)
        {
            UseShellExecute = false,
            WorkingDirectory = repositoryRoot,
        };
        ApplyMinimalK6Environment(startInfo);
        AddK6RunArguments(startInfo, summaryFile, apiBaseUrl, profile, k6Arguments, K6ScenarioPath);

        return await RunProcess(startInfo, k6Path).ConfigureAwait(false);
    }

    private static async Task<int> RunDockerK6(
        string dockerPath,
        string repositoryRoot,
        string apiBaseUrl,
        string profile,
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
            dockerK6Image);

        AddK6RunArguments(startInfo, DockerK6SummaryPath, apiBaseUrl, profile, k6Arguments, DockerK6ScenarioPath);

        return await RunProcess(startInfo, dockerPath).ConfigureAwait(false);
    }

    private static void AddK6RunArguments(
        ProcessStartInfo startInfo,
        string summaryFile,
        string apiBaseUrl,
        string profile,
        IReadOnlyCollection<string> k6Arguments,
        string scenarioPath)
    {
        AddArguments(
            startInfo,
            "run",
            "--no-usage-report",
            "--summary-export",
            summaryFile,
            "-e",
            "VT_API_BASE_URL=" + apiBaseUrl,
            "-e",
            "VT_K6_PROFILE=" + profile,
            "-e",
            "K6_NO_USAGE_REPORT=true");

        AddOptionalEnvironment(startInfo, "VT_K6_VUS");
        AddOptionalEnvironment(startInfo, "VT_K6_DURATION");

        foreach (var argument in k6Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.ArgumentList.Add(scenarioPath);
    }

    private static string NormalizeApiBaseUrl(string value)
    {
        return value.Trim().TrimEnd('/');
    }

    internal static void ValidateTarget(string apiBaseUrl)
    {
        if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("VT_API_BASE_URL must start with http:// or https://.");
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException("VT_API_BASE_URL must not include user information.");
        }

        if (IsEnabled("VT_K6_ALLOW_EXTERNAL") || IsLocalTarget(uri))
        {
            return;
        }

        throw new ArgumentException("VT_API_BASE_URL must target localhost, 127.0.0.1, [::1], or host.docker.internal unless VT_K6_ALLOW_EXTERNAL=1 is set.");
    }

    private static bool IsLocalTarget(Uri uri)
    {
        return string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Host, "::1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Host, "[::1]", StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Host, "host.docker.internal", StringComparison.OrdinalIgnoreCase);
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

    internal static string ResolveUseDocker(string useDocker)
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

    private static string GetRequiredEnvironmentValue(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{name} is required.");
        }

        return value;
    }

    internal static string NormalizeResultsDirectory(string resultsDirectory)
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

    internal static void ValidateProfile(string profile)
    {
        if (profile.Length == 0 || profile.Any(static value => !char.IsAsciiLetterOrDigit(value) && value is not '_' and not '-'))
        {
            throw new ArgumentException("VT_K6_PROFILE may contain only letters, numbers, underscores, and hyphens.");
        }
    }

    internal static void ValidateDockerImage(string dockerK6Image, string useDocker)
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

            if (IsBlockedFlag(argument, "--summary-export"))
            {
                throw new ArgumentException("--summary-export is controlled by the repository runner. Use VT_K6_RESULTS_DIR to choose the results folder.");
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

    internal static string ToDockerUrl(string url)
    {
        return url.Replace("0.0.0.0", "host.docker.internal", StringComparison.OrdinalIgnoreCase)
            .Replace("127.0.0.1", "host.docker.internal", StringComparison.OrdinalIgnoreCase)
            .Replace("localhost", "host.docker.internal", StringComparison.OrdinalIgnoreCase)
            .Replace("[::1]", "host.docker.internal", StringComparison.OrdinalIgnoreCase)
            .Replace("[::]", "host.docker.internal", StringComparison.OrdinalIgnoreCase);
    }

    private static string ToPlatformPath(string relativePath)
    {
        return relativePath.Replace('/', Path.DirectorySeparatorChar);
    }
}
