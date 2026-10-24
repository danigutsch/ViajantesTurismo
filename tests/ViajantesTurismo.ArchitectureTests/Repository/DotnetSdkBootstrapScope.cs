using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace ViajantesTurismo.ArchitectureTests.Repository;

internal sealed class DotnetSdkBootstrapScope : IDisposable
{
    private readonly TemporaryWorkspace _workspace = new();
    private readonly bool _powershell;
    private readonly string _repositoryRoot = RepositoryTestProcess.GetRepositoryRoot();
    private readonly string _rid = $"{(OperatingSystem.IsMacOS() ? "osx" : "linux")}-{(RuntimeInformation.OSArchitecture switch { Architecture.X64 => "x64", Architecture.Arm64 => "arm64", Architecture.Arm => "arm", _ => throw new PlatformNotSupportedException("Unsupported SDK fixture architecture.") })}";

    public DotnetSdkBootstrapScope(bool powershell)
    {
        _powershell = powershell;
        _workspace.WriteFile("global.json", "{\"sdk\":{\"version\":\"10.0.303\",\"rollForward\":\"disable\"}}");
        _workspace.WriteFile("sdks.txt", string.Empty);
        _workspace.WriteFile("payload/sdk/10.0.303/dotnet.dll", "fixture");
        _workspace.WriteFile("payload/dotnet", "new host");
        _workspace.WriteFile("install/dotnet", "existing host");
        _workspace.WriteFile("install/sdk/10.0.302/marker", "old sdk");
        _workspace.WriteFile("bin/dotnet", "#!/usr/bin/env bash\ncat \"$FAKE_DOTNET_SDKS_FILE\"\n");
        _workspace.WriteFile("bin/python3", "#!/usr/bin/env bash\necho 'SDK bootstrap must not invoke Python' >&2\nexit 99\n");
        _workspace.WriteFile("bin/curl", """
            #!/usr/bin/env bash
            set -euo pipefail
            output=""
            url=""
            while [[ $# -gt 0 ]]; do
                case "$1" in
                    --output) shift; output="$1" ;;
                    https://*) url="$1" ;;
                esac
                shift
            done
            printf '%s\n' "$url" >> "$FAKE_WEB_LOG"
            case "$url" in
                https://dotnetcli.blob.core.windows.net/*) cp "$FAKE_METADATA_FILE" "$output" ;;
                https://builds.dotnet.microsoft.com/*) cp "$FAKE_ARCHIVE_FILE" "$output" ;;
                *) exit 65 ;;
            esac
            """);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(PathFor("bin/dotnet"), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            File.SetUnixFileMode(PathFor("bin/python3"), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            File.SetUnixFileMode(PathFor("bin/curl"), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        _workspace.WriteFile("invoke.ps1", """
            $ErrorActionPreference = "Stop"
            function global:Invoke-WebRequest {
                param([uri]$Uri, [string]$OutFile, [switch]$UseBasicParsing)
                $null = $UseBasicParsing
                Add-Content -Path $env:FAKE_WEB_LOG -Value $Uri.AbsoluteUri
                if ($Uri.Host -eq "dotnetcli.blob.core.windows.net") {
                    Copy-Item $env:FAKE_METADATA_FILE $OutFile
                } elseif ($Uri.Host -eq "builds.dotnet.microsoft.com") {
                    Copy-Item $env:FAKE_ARCHIVE_FILE $OutFile
                } else { throw "Unexpected URI: $Uri" }
            }
            & $env:BOOTSTRAP_SUBJECT -GlobalJson $env:BOOTSTRAP_GLOBAL_JSON -InstallDir $env:BOOTSTRAP_INSTALL_DIR -Install:($env:BOOTSTRAP_INSTALL -eq "true")
            """);
        using (var archive = File.Create(PathFor("sdk.tar.gz")))
        using (var compressed = new GZipStream(archive, CompressionMode.Compress))
        {
            TarFile.CreateFromDirectory(PathFor("payload"), compressed, includeBaseDirectory: false);
        }

        WriteMetadata();
    }

    public string PathFor(string path) => Path.Combine(_workspace.RootPath, path);

    public bool FileExists(string path) => _workspace.FileExists(path);

    public void SetInstalledVersion(string version) =>
        _workspace.WriteFile("sdks.txt", $"{version} [{PathFor("install/sdk")}]\n");

    public void WriteAlternateGlobalJson() =>
        _workspace.WriteFile("alternate.json", "{\"sdk\":{\"version\":\"10.0.304\",\"rollForward\":\"disable\"}}");

    public void CorruptArchive() => _workspace.WriteFile("sdk.tar.gz", "not an SDK archive");

    public void UseCopyWithoutUpdateArgument()
    {
        _workspace.WriteFile("bin/cp", """
            #!/usr/bin/env bash
            if [[ "$1" == "--help" ]]; then
                printf '%s\n' '  -u, --update  copy only newer source files'
                exit 0
            fi
            for argument in "$@"; do
                case "$argument" in
                    --update=*) echo "cp: option '--update' does not allow an argument" >&2; exit 1 ;;
                esac
            done
            exec /bin/cp "$@"
            """);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(PathFor("bin/cp"), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    public void WriteMetadata(bool invalidHash = false)
    {
        var hash = invalidHash ? new string('0', 128) : Convert.ToHexString(SHA512.HashData(File.ReadAllBytes(PathFor("sdk.tar.gz"))));
        var metadata = new
        {
            releases = new[]
            {
                new
                {
                    sdks = new[]
                    {
                        new
                        {
                            version = "10.0.303",
                            files = new[]
                            {
                                new
                                {
                                    name = $"dotnet-sdk-{_rid}.tar.gz",
                                    rid = _rid,
                                    url = $"https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.303/dotnet-sdk-{_rid}.tar.gz",
                                    hash
                                }
                            }
                        }
                    }
                }
            }
        };
        _workspace.WriteFile("metadata.json", JsonSerializer.Serialize(metadata));
    }

    public Task<RepositoryTestProcess.ProcessResult> Run(bool install, CancellationToken ct, string globalJson = "global.json")
    {
        var subject = Path.Combine(_repositoryRoot, "scripts", _powershell ? "ensure-dotnet-sdk.ps1" : "ensure-dotnet-sdk.sh");
        var environment = new Dictionary<string, string?>
        {
            ["PATH"] = $"{PathFor("bin")}{Path.PathSeparator}{Environment.GetEnvironmentVariable("PATH")}",
            ["FAKE_DOTNET_SDKS_FILE"] = PathFor("sdks.txt"),
            ["FAKE_WEB_LOG"] = PathFor("web.log"),
            ["FAKE_METADATA_FILE"] = PathFor("metadata.json"),
            ["FAKE_ARCHIVE_FILE"] = PathFor("sdk.tar.gz"),
            ["BOOTSTRAP_SUBJECT"] = subject,
            ["BOOTSTRAP_GLOBAL_JSON"] = PathFor(globalJson),
            ["BOOTSTRAP_INSTALL_DIR"] = PathFor("install"),
            ["BOOTSTRAP_INSTALL"] = install ? "true" : "false"
        };
        List<string> arguments = _powershell
            ? ["-NoLogo", "-NoProfile", "-File", PathFor("invoke.ps1")]
            : [subject, "--global-json", PathFor(globalJson), "--install-dir", PathFor("install")];
        if (install && !_powershell)
        {
            arguments.Add("--install");
        }

        return RepositoryTestProcess.Run(_powershell ? "pwsh" : "bash", _workspace.RootPath, arguments, ct, environment);
    }

    public void Dispose() => _workspace.Dispose();
}
