#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [switch]$Install,
    [string]$GlobalJson = (Join-Path (Split-Path -Parent $PSScriptRoot) "global.json"),
    [string]$InstallDir = (Join-Path $HOME ".dotnet")
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if (-not (Test-Path -Path $GlobalJson -PathType Leaf)) {
    throw "global.json not found: $GlobalJson"
}

try {
    $requiredVersion = (Get-Content -Path $GlobalJson -Raw | ConvertFrom-Json).sdk.version
}
catch {
    throw "Unable to read sdk.version from ${GlobalJson}: $($_.Exception.Message)"
}

if ($requiredVersion -isnot [string] -or $requiredVersion -notmatch '^[0-9]+\.[0-9]+\.[0-9]+([-.][0-9A-Za-z.-]+)?$') {
    throw "Invalid sdk.version in ${GlobalJson}: $requiredVersion"
}

function Test-SdkInstalled {
    if (Test-Path -Path (Join-Path $InstallDir "sdk/$requiredVersion/dotnet.dll") -PathType Leaf) {
        return $true
    }

    if ($null -eq (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        return $false
    }

    $installedSdks = @(& dotnet --list-sdks 2> $null)
    $versionPattern = '^' + [regex]::Escape($requiredVersion) + ' \['
    return $null -ne ($installedSdks | Where-Object { $_ -match $versionPattern } | Select-Object -First 1)
}

if (Test-SdkInstalled) {
    Write-Output ".NET SDK $requiredVersion is already installed."
    return
}

if (-not $Install) {
    throw ".NET SDK $requiredVersion is required by $GlobalJson but is not installed. Install it side-by-side with: pwsh -NoProfile -File scripts/ensure-dotnet-sdk.ps1 -Install -GlobalJson `"$GlobalJson`" -InstallDir `"$InstallDir`""
}

function Get-PlatformRid {
    if ($IsWindows) {
        $os = "win"
        $archiveSuffix = ".zip"
    }
    elseif ($IsMacOS) {
        $os = "osx"
        $archiveSuffix = ".tar.gz"
    }
    elseif ($IsLinux) {
        $os = "linux"
        $archiveSuffix = ".tar.gz"
    }
    else {
        throw "Unsupported operating system."
    }

    $architecture = switch ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture) {
        ([System.Runtime.InteropServices.Architecture]::X64) { "x64"; break }
        ([System.Runtime.InteropServices.Architecture]::X86) { "x86"; break }
        ([System.Runtime.InteropServices.Architecture]::Arm64) { "arm64"; break }
        ([System.Runtime.InteropServices.Architecture]::Arm) { "arm"; break }
        default { throw "Unsupported architecture: $([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture)" }
    }

    [pscustomobject]@{
        Rid = "$os-$architecture"
        ArchiveSuffix = $archiveSuffix
    }
}

function Merge-DirectoryWithoutOverwrite {
    param(
        [Parameter(Mandatory)]
        [string]$Source,
        [Parameter(Mandatory)]
        [string]$Destination
    )

    New-Item -ItemType Directory -Path $Destination -ErrorAction SilentlyContinue | Out-Null

    foreach ($directory in Get-ChildItem -Path $Source -Directory -Recurse) {
        $relativePath = [System.IO.Path]::GetRelativePath($Source, $directory.FullName)
        $targetPath = Join-Path $Destination $relativePath
        New-Item -ItemType Directory -Path $targetPath -ErrorAction SilentlyContinue | Out-Null
    }

    foreach ($file in Get-ChildItem -Path $Source -File -Recurse) {
        $relativePath = [System.IO.Path]::GetRelativePath($Source, $file.FullName)
        $targetPath = Join-Path $Destination $relativePath
        if (Test-Path -Path $targetPath) {
            continue
        }
        Copy-Item -Path $file.FullName -Destination $targetPath
    }
}

$platform = Get-PlatformRid
$versionParts = $requiredVersion.Split('.')
$channel = "$($versionParts[0]).$($versionParts[1])"
$metadataUri = [uri]"https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/$channel/releases.json"
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("dotnet-sdk-" + [guid]::NewGuid().ToString("N"))
$metadataPath = Join-Path $tempDir "releases.json"
$archivePath = Join-Path $tempDir ("dotnet-sdk" + $platform.ArchiveSuffix)
$payloadPath = Join-Path $tempDir "payload"

New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    Invoke-WebRequest -UseBasicParsing -Uri $metadataUri -OutFile $metadataPath
    $metadata = Get-Content -Path $metadataPath -Raw | ConvertFrom-Json
    $archiveRecord = $null

    foreach ($release in $metadata.releases) {
        $sdkCandidates = @()
        if ($null -ne $release.PSObject.Properties["sdks"]) {
            $sdkCandidates += @($release.sdks)
        }
        if ($null -ne $release.PSObject.Properties["sdk"]) {
            $sdkCandidates += @($release.sdk)
        }

        foreach ($sdk in $sdkCandidates) {
            if ($sdk.version -ne $requiredVersion) {
                continue
            }
            $archiveRecord = @($sdk.files | Where-Object {
                    $_.rid -eq $platform.Rid -and $_.name.EndsWith($platform.ArchiveSuffix, [System.StringComparison]::Ordinal)
                })[0]
            if ($null -ne $archiveRecord) {
                break
            }
        }
        if ($null -ne $archiveRecord) {
            break
        }
    }

    if ($null -eq $archiveRecord) {
        throw "Official release metadata has no $requiredVersion SDK archive for $($platform.Rid)."
    }

    $archiveUri = [uri]$archiveRecord.url
    $expectedPathPrefix = "/dotnet/Sdk/$requiredVersion/"
    if ($archiveUri.Scheme -ne "https" -or $archiveUri.Host -ne "builds.dotnet.microsoft.com" -or -not $archiveUri.AbsolutePath.StartsWith($expectedPathPrefix, [System.StringComparison]::Ordinal)) {
        throw "Official release metadata returned an unexpected SDK URL: $archiveUri"
    }

    $expectedHash = [string]$archiveRecord.hash
    if ($expectedHash -notmatch '^[0-9A-Fa-f]{128}$') {
        throw "Official release metadata returned an invalid SHA-512 hash."
    }

    Invoke-WebRequest -UseBasicParsing -Uri $archiveUri -OutFile $archivePath
    $actualHash = (Get-FileHash -Path $archivePath -Algorithm SHA512).Hash
    if (-not $actualHash.Equals($expectedHash, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "SHA-512 verification failed for .NET SDK $requiredVersion."
    }

    New-Item -ItemType Directory -Path $payloadPath | Out-Null
    if ($platform.ArchiveSuffix -eq ".zip") {
        try {
            Expand-Archive -Path $archivePath -DestinationPath $payloadPath
        }
        catch {
            throw "Failed to extract the verified .NET SDK archive."
        }
    }
    else {
        & tar -xzf $archivePath -C $payloadPath
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to extract the verified .NET SDK archive."
        }
    }

    if (-not (Test-Path -Path (Join-Path $payloadPath "sdk/$requiredVersion/dotnet.dll") -PathType Leaf)) {
        throw "Verified archive does not contain .NET SDK $requiredVersion."
    }

    Merge-DirectoryWithoutOverwrite -Source $payloadPath -Destination $InstallDir
    if (-not (Test-Path -Path (Join-Path $InstallDir "sdk/$requiredVersion/dotnet.dll") -PathType Leaf)) {
        throw "Failed to install .NET SDK $requiredVersion."
    }

    Write-Output "Installed .NET SDK $requiredVersion side-by-side in $InstallDir."
}
finally {
    Remove-Item -Path $tempDir -Recurse -ErrorAction SilentlyContinue
}
