using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ViajantesTurismo.OpenApi.Tool;

internal static class OpenApiGenerationCommand
{
    private const string OpenApiGenerationEnvironment = "OpenApiGeneration";
    private const string NuGetPackagesEnvironmentVariable = "NUGET_PACKAGES";
    private static readonly string[] UnixPreservedEnvironmentVariables = ["HOME", "TMPDIR"];
    private static readonly string[] WindowsPreservedEnvironmentVariables = ["TEMP", "TMP", "USERPROFILE", "APPDATA", "LOCALAPPDATA"];

    public static ProcessStartInfo CreateStartInfo(OpenApiGenerationOptions options)
    {
        return CreateStartInfo(options, GetDotnetHostPath());
    }

    internal static ProcessStartInfo CreateStartInfo(OpenApiGenerationOptions options, string dotnetHostPath)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(dotnetHostPath);

        if (!Path.IsPathFullyQualified(dotnetHostPath))
        {
            throw new ArgumentException("The dotnet host path must be absolute.", nameof(dotnetHostPath));
        }

        var (project, properties, useParallelGraph) = options.Target switch
        {
            OpenApiTarget.Admin => (
                "src/ViajantesTurismo.Admin.ApiService/ViajantesTurismo.Admin.ApiService.csproj",
                new[] { options.Refresh ? "RefreshAdminOpenApiArtifacts" : "GenerateAdminOpenApiArtifacts" },
                false),
            OpenApiTarget.Catalog => (
                "src/ViajantesTurismo.Catalog.ApiService/ViajantesTurismo.Catalog.ApiService.csproj",
                new[] { options.Refresh ? "RefreshCatalogOpenApiArtifacts" : "GenerateCatalogOpenApiArtifacts" },
                false),
            OpenApiTarget.Branding => (
                "src/ViajantesTurismo.Branding.ApiService/ViajantesTurismo.Branding.ApiService.csproj",
                new[] { options.Refresh ? "RefreshBrandingOpenApiArtifacts" : "GenerateBrandingOpenApiArtifacts" },
                false),
            OpenApiTarget.All => (
                "tools/ViajantesTurismo.OpenApi.Tool/OpenApiGeneration.slnx",
                options.Refresh
                    ? new[]
                    {
                        "RefreshAdminOpenApiArtifacts",
                        "RefreshCatalogOpenApiArtifacts",
                        "RefreshBrandingOpenApiArtifacts"
                    }
                    : new[]
                    {
                        "GenerateAdminOpenApiArtifacts",
                        "GenerateCatalogOpenApiArtifacts",
                        "GenerateBrandingOpenApiArtifacts"
                    },
                true),
            _ => throw new ArgumentOutOfRangeException(
                nameof(options),
                options.Target,
                "The OpenAPI target is not supported.")
        };

        var startInfo = new ProcessStartInfo(dotnetHostPath)
        {
            UseShellExecute = false,
            WorkingDirectory = options.RepositoryRoot
        };
        startInfo.ArgumentList.Add("build");
        startInfo.ArgumentList.Add("--no-restore");
        startInfo.ArgumentList.Add(project);
        foreach (var property in properties)
        {
            startInfo.ArgumentList.Add($"-p:{property}=true");
        }

        if (useParallelGraph)
        {
            startInfo.ArgumentList.Add("-maxcpucount");
        }

        ApplyGenerationEnvironment(startInfo);

        return startInfo;
    }

    internal static void ApplyGenerationEnvironment(ProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        var isWindows = OperatingSystem.IsWindows();
        var preservedVariables = isWindows
            ? WindowsPreservedEnvironmentVariables
            : UnixPreservedEnvironmentVariables;
        var preservedEnvironment = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var variable in preservedVariables)
        {
            if (startInfo.Environment.TryGetValue(variable, out var value)
                && !string.IsNullOrWhiteSpace(value))
            {
                preservedEnvironment[variable] = value;
            }
        }

        var repositoryNuGetPackagesPath = Path.Combine(startInfo.WorkingDirectory, ".nuget", "packages");
        var normalizedRepositoryNuGetPackagesPath = Path.TrimEndingDirectorySeparator(
            repositoryNuGetPackagesPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
        if (startInfo.Environment.TryGetValue(NuGetPackagesEnvironmentVariable, out var nugetPackagesPath)
            && !string.IsNullOrWhiteSpace(nugetPackagesPath))
        {
            var normalizedNuGetPackagesPath = Path.TrimEndingDirectorySeparator(
                nugetPackagesPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
            if (string.Equals(
                normalizedNuGetPackagesPath,
                normalizedRepositoryNuGetPackagesPath,
                isWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                preservedEnvironment[NuGetPackagesEnvironmentVariable] = repositoryNuGetPackagesPath;
            }
        }

        startInfo.Environment.Clear();

        foreach (var (variable, value) in preservedEnvironment)
        {
            startInfo.Environment[variable] = value;
        }

        var dotnetDirectory = Path.GetDirectoryName(startInfo.FileName)
            ?? throw new InvalidOperationException("Could not determine the dotnet host directory.");

        if (isWindows)
        {
            var systemRoot = Path.GetDirectoryName(Environment.SystemDirectory)
                ?? throw new InvalidOperationException("Could not determine the Windows system root.");
            startInfo.Environment["SystemRoot"] = systemRoot;
            startInfo.Environment["ComSpec"] = Path.Combine(Environment.SystemDirectory, "cmd.exe");
            startInfo.Environment["PATH"] = string.Join(Path.PathSeparator, dotnetDirectory, Environment.SystemDirectory);
        }
        else
        {
            startInfo.Environment["PATH"] = string.Join(Path.PathSeparator, dotnetDirectory, "/usr/bin", "/bin");
        }

        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = OpenApiGenerationEnvironment;
        startInfo.Environment["DOTNET_ENVIRONMENT"] = OpenApiGenerationEnvironment;
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.Environment["DOTNET_MULTILEVEL_LOOKUP"] = "0";
        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        startInfo.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
        startInfo.Environment["OTEL_LOGS_EXPORTER"] = "none";
        startInfo.Environment["OTEL_METRICS_EXPORTER"] = "none";
        startInfo.Environment["OTEL_SDK_DISABLED"] = "true";
        startInfo.Environment["OTEL_TRACES_EXPORTER"] = "none";
    }

    private static string GetDotnetHostPath()
    {
        var dotnetHostPath = Path.GetFullPath(Path.Combine(
            RuntimeEnvironment.GetRuntimeDirectory(),
            "..",
            "..",
            "..",
            OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet"));

        if (!File.Exists(dotnetHostPath)
            || !string.Equals(Path.GetFileNameWithoutExtension(dotnetHostPath), "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The OpenAPI tool must run through an absolute dotnet host path.");
        }

        return dotnetHostPath;
    }
}
