#!/usr/bin/env pwsh

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$subject = Join-Path $repoRoot "scripts/ensure-dotnet-sdk.ps1"
$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("dotnet-sdk-bootstrap-" + [guid]::NewGuid().ToString("N"))

function Assert-True {
    param(
        [Parameter(Mandatory)]
        [bool]$Condition,
        [Parameter(Mandatory)]
        [string]$Message
    )

    if (-not $Condition) {
        throw "FAIL: $Message"
    }
}

function Assert-Substring {
    param(
        [AllowEmptyString()]
        [string]$Value,
        [Parameter(Mandatory)]
        [string]$Expected,
        [Parameter(Mandatory)]
        [string]$Message
    )

    Assert-True -Condition $Value.Contains($Expected, [System.StringComparison]::Ordinal) -Message "$Message; expected '$Expected' in '$Value'"
}

function Write-GlobalJson {
    param(
        [Parameter(Mandatory)]
        [string]$Path,
        [Parameter(Mandatory)]
        [string]$Version
    )

    @{
        sdk = @{
            version = $Version
            rollForward = "disable"
        }
    } | ConvertTo-Json -Depth 3 | Set-Content -Path $Path
}

function Write-ReleaseManifest {
    param(
        [Parameter(Mandatory)]
        [string]$Path,
        [Parameter(Mandatory)]
        [string]$Hash,
        [Parameter(Mandatory)]
        [string]$Rid,
        [Parameter(Mandatory)]
        [string]$ArchiveName,
        [Parameter(Mandatory)]
        [string]$ArchiveSuffix
    )

    @{
        releases = @(
            @{
                sdks = @(
                    @{
                        version = "10.0.303"
                        files = @(
                            @{
                                name = $ArchiveName
                                rid = $Rid
                                url = "https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.303/dotnet-sdk-10.0.303-$Rid$archiveSuffix"
                                hash = $Hash
                            }
                        )
                    }
                )
            }
        )
    } | ConvertTo-Json -Depth 8 | Set-Content -Path $Path
}

function Invoke-Subject {
    param([hashtable]$Parameters)

    (& $subject @Parameters 2>&1 | Out-String).Trim()
}

New-Item -ItemType Directory -Path $testRoot | Out-Null

try {
    $architecture = switch ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture) {
        ([System.Runtime.InteropServices.Architecture]::X64) { "x64"; break }
        ([System.Runtime.InteropServices.Architecture]::X86) { "x86"; break }
        ([System.Runtime.InteropServices.Architecture]::Arm64) { "arm64"; break }
        ([System.Runtime.InteropServices.Architecture]::Arm) { "arm"; break }
        default { throw "Unsupported test architecture." }
    }
    if ($IsWindows) {
        $testRid = "win-$architecture"
        $archiveSuffix = ".zip"
    }
    elseif ($IsMacOS) {
        $testRid = "osx-$architecture"
        $archiveSuffix = ".tar.gz"
    }
    else {
        $testRid = "linux-$architecture"
        $archiveSuffix = ".tar.gz"
    }
    $archiveName = "dotnet-sdk-$testRid$archiveSuffix"

    $fakeBin = Join-Path $testRoot "bin"
    $sdkList = Join-Path $testRoot "sdks.txt"
    $webLog = Join-Path $testRoot "web.log"
    $metadataFile = Join-Path $testRoot "releases.json"
    $archiveFile = Join-Path $testRoot ("dotnet-sdk-10.0.303-$testRid$archiveSuffix")
    $archiveRoot = Join-Path $testRoot "archive-root"
    $installDir = Join-Path $testRoot "dotnet"
    $globalJson = Join-Path $testRoot "global.json"

    New-Item -ItemType Directory -Path $fakeBin | Out-Null
    if ($IsWindows) {
        @'
@echo off
if "%1"=="--list-sdks" (
    type "%FAKE_DOTNET_SDKS_FILE%"
    exit /b 0
)
exit /b 64
'@ | Set-Content -Path (Join-Path $fakeBin "dotnet.cmd")
    }
    else {
        @'
#!/usr/bin/env bash
set -euo pipefail

if [[ "${1:-}" == "--list-sdks" ]]; then
    cat "${FAKE_DOTNET_SDKS_FILE}"
    exit 0
fi

exit 64
'@ | Set-Content -Path (Join-Path $fakeBin "dotnet") -NoNewline
        & chmod +x (Join-Path $fakeBin "dotnet")
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to make fake dotnet executable."
        }
    }

    Write-GlobalJson -Path $globalJson -Version "10.0.303"
    New-Item -ItemType Directory -Path (Join-Path $archiveRoot "sdk/10.0.303") | Out-Null
    "new sdk" | Set-Content -Path (Join-Path $archiveRoot "sdk/10.0.303/marker")
    "fixture" | Set-Content -Path (Join-Path $archiveRoot "sdk/10.0.303/dotnet.dll")
    if ($IsWindows) {
        Compress-Archive -Path (Join-Path $archiveRoot "*") -DestinationPath $archiveFile
    }
    else {
        & tar -czf $archiveFile -C $archiveRoot .
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to create SDK fixture archive."
        }
    }
    $archiveHash = (Get-FileHash -Path $archiveFile -Algorithm SHA512).Hash.ToLowerInvariant()

    $originalPath = $env:PATH
    $env:PATH = "$fakeBin$([System.IO.Path]::PathSeparator)$originalPath"
    $env:FAKE_DOTNET_SDKS_FILE = $sdkList
    $env:FAKE_WEB_LOG = $webLog
    $env:FAKE_METADATA_FILE = $metadataFile
    $env:FAKE_ARCHIVE_FILE = $archiveFile

    function global:Invoke-WebRequest {
        param(
            [Parameter(Mandatory)]
            [uri]$Uri,
            [Parameter(Mandatory)]
            [string]$OutFile,
            [switch]$UseBasicParsing
        )

        $null = $UseBasicParsing
        $Uri.AbsoluteUri | Add-Content -Path $env:FAKE_WEB_LOG
        if ($Uri.Host -eq "dotnetcli.blob.core.windows.net") {
            Copy-Item -Path $env:FAKE_METADATA_FILE -Destination $OutFile
            return
        }
        if ($Uri.Host -eq "builds.dotnet.microsoft.com") {
            Copy-Item -Path $env:FAKE_ARCHIVE_FILE -Destination $OutFile
            return
        }
        throw "Unexpected URI: $Uri"
    }

    "10.0.302 [$installDir/sdk]`n10.0.303 [$installDir/sdk]" | Set-Content -Path $sdkList
    Remove-Item -Path $webLog -ErrorAction SilentlyContinue
    $output = Invoke-Subject -Parameters @{ GlobalJson = $globalJson; InstallDir = $installDir }
    Assert-Substring -Value $output -Expected "10.0.303 is already installed" -Message "installed SDK result"
    Assert-True -Condition (-not (Test-Path $webLog)) -Message "installed SDK must not access the network"

    "10.0.302 [$installDir/sdk]" | Set-Content -Path $sdkList
    Remove-Item -Path $webLog -ErrorAction SilentlyContinue
    try {
        $null = Invoke-Subject -Parameters @{ GlobalJson = $globalJson; InstallDir = $installDir }
        throw "FAIL: missing SDK without opt-in should fail"
    }
    catch {
        Assert-Substring -Value $_.Exception.Message -Expected "-Install" -Message "missing SDK guidance"
    }
    Assert-True -Condition (-not (Test-Path $webLog)) -Message "missing SDK without opt-in must not access the network"

    New-Item -ItemType Directory -Path (Join-Path $installDir "sdk/10.0.302") | Out-Null
    "old sdk" | Set-Content -Path (Join-Path $installDir "sdk/10.0.302/marker")
    Write-ReleaseManifest -Path $metadataFile -Hash $archiveHash -Rid $testRid -ArchiveName $archiveName -ArchiveSuffix $archiveSuffix
    Remove-Item -Path $webLog -ErrorAction SilentlyContinue
    $output = Invoke-Subject -Parameters @{ Install = $true; GlobalJson = $globalJson; InstallDir = $installDir }
    Assert-Substring -Value $output -Expected "Installed .NET SDK 10.0.303" -Message "successful install result"
    Assert-True -Condition (Test-Path (Join-Path $installDir "sdk/10.0.303/marker")) -Message "exact SDK was not installed"
    Assert-True -Condition (Test-Path (Join-Path $installDir "sdk/10.0.302/marker")) -Message "existing SDK was removed"
    Assert-Substring -Value (Get-Content -Path $webLog -Raw) -Expected "/Sdk/10.0.303/" -Message "exact SDK download"

    Remove-Item -Path (Join-Path $installDir "sdk/10.0.303") -Recurse
    Write-ReleaseManifest -Path $metadataFile -Hash ("0" * 128) -Rid $testRid -ArchiveName $archiveName -ArchiveSuffix $archiveSuffix
    try {
        $null = Invoke-Subject -Parameters @{ Install = $true; GlobalJson = $globalJson; InstallDir = $installDir }
        throw "FAIL: checksum mismatch should fail"
    }
    catch {
        Assert-Substring -Value $_.Exception.Message -Expected "SHA-512 verification failed" -Message "checksum failure"
    }
    Assert-True -Condition (-not (Test-Path (Join-Path $installDir "sdk/10.0.303"))) -Message "unverified SDK must not be installed"

    "not an SDK archive" | Set-Content -Path $archiveFile
    $invalidArchiveHash = (Get-FileHash -Path $archiveFile -Algorithm SHA512).Hash.ToLowerInvariant()
    Write-ReleaseManifest -Path $metadataFile -Hash $invalidArchiveHash -Rid $testRid -ArchiveName $archiveName -ArchiveSuffix $archiveSuffix
    try {
        $null = Invoke-Subject -Parameters @{ Install = $true; GlobalJson = $globalJson; InstallDir = $installDir }
        throw "FAIL: invalid verified archive should fail"
    }
    catch {
        Assert-Substring -Value $_.Exception.Message -Expected "Failed to extract the verified .NET SDK archive" -Message "extraction failure"
    }
    Assert-True -Condition (-not (Test-Path (Join-Path $installDir "sdk/10.0.303"))) -Message "invalid SDK archive must not be installed"

    $alternateGlobalJson = Join-Path $testRoot "alternate-global.json"
    Write-GlobalJson -Path $alternateGlobalJson -Version "10.0.304"
    "10.0.304 [$installDir/sdk]" | Set-Content -Path $sdkList
    Remove-Item -Path $webLog -ErrorAction SilentlyContinue
    $output = Invoke-Subject -Parameters @{ GlobalJson = $alternateGlobalJson; InstallDir = $installDir }
    Assert-Substring -Value $output -Expected "10.0.304 is already installed" -Message "selected global.json result"
    Assert-True -Condition (-not (Test-Path $webLog)) -Message "selected installed SDK must not access the network"

    Write-Output "PowerShell dotnet SDK bootstrap tests passed"
}
finally {
    Remove-Item -Path Function:\Invoke-WebRequest -ErrorAction SilentlyContinue
    if (Test-Path Variable:\originalPath) {
        $env:PATH = $originalPath
    }
    Remove-Item -Path $testRoot -Recurse -ErrorAction SilentlyContinue
}
