#!/usr/bin/env pwsh
# ViajantesTurismo Development Environment Setup Script
# This script verifies required tooling and points to optional local tools

param(
    [switch]$InstallSdk,
    [string]$LibraryRoot,
    [string]$Feed
)

$ErrorActionPreference = "Stop"
$repoRoot = $PSScriptRoot
if ($LibraryRoot) {
    $LibraryRoot = (Resolve-Path -LiteralPath $LibraryRoot).ProviderPath
}
if ($Feed) {
    $Feed = (Resolve-Path -LiteralPath $Feed).ProviderPath
}

function Write-SetupHeader {
    Write-Host "`n🚀 ViajantesTurismo Development Setup" -ForegroundColor Cyan
    Write-Host "====================================`n" -ForegroundColor Cyan
}

function Test-DotNetSdkVersion {
    param([bool]$InstallMissingSdk)

    Write-Host "📦 Checking .NET SDK..." -ForegroundColor Yellow
    $globalJsonPath = Join-Path $repoRoot "global.json"
    $globalJson = Get-Content $globalJsonPath | ConvertFrom-Json
    $requiredVersion = $globalJson.sdk.version
    $installDir = Join-Path $HOME ".dotnet"
    $ensureParameters = @{
        GlobalJson = $globalJsonPath
        InstallDir = $installDir
    }
    if ($InstallMissingSdk) {
        $ensureParameters.Install = $true
    }

    & (Join-Path $repoRoot "scripts/ensure-dotnet-sdk.ps1") @ensureParameters

    if (Test-Path -Path (Join-Path $installDir "sdk/$requiredVersion/dotnet.dll") -PathType Leaf) {
        $env:DOTNET_ROOT = $installDir
        $env:PATH = "$installDir$([System.IO.Path]::PathSeparator)$env:PATH"
    }

    $installedSdk = dotnet --version 2>&1
    if ($LASTEXITCODE -ne 0 -or $installedSdk -ne $requiredVersion) {
        Write-Error "Resolved .NET SDK does not match required $requiredVersion"
        exit 1
    }

    Write-Host "   ✅ .NET SDK installed: $installedSdk" -ForegroundColor Green
}

function Invoke-SharedKernelCandidateStaging {
    Write-Host "`n📦 Staging the pinned SharedKernel candidate..." -ForegroundColor Yellow
    $revision = [string](Get-Content -LiteralPath .config/sharedkernel-candidate.json -Raw | ConvertFrom-Json).libraryRevision
    if ($revision -cnotmatch '^[0-9a-f]{40}$') {
        Write-Error "The pinned libraryRevision must be 40 lowercase hexadecimal characters"
        exit 1
    }

    $sourceDir = $LibraryRoot
    if (-not $sourceDir) {
        $sourceDir = Join-Path $repoRoot ".local/sharedkernel-source/SharedKernel.CI.Tool"
        $clone = -not (Test-Path -LiteralPath $sourceDir)
        if (-not $clone) {
            $cdup = git -C $sourceDir rev-parse --show-cdup 2>$null
            $isTopLevel = $LASTEXITCODE -eq 0 -and -not $cdup
            $promisor = git -C $sourceDir config --get remote.origin.promisor
            $sparse = (git -C $sourceDir sparse-checkout list) -join "`n"
            if (-not $isTopLevel -or $promisor -cne 'true' -or $sparse -cne "tools/Shared`ntools/SharedKernel.CI.Tool") {
                Write-Error "$sourceDir is not the expected sparse SharedKernel clone; move it away and rerun"
                exit 1
            }
        }
        git -C $sourceDir cat-file -e "$revision^{commit}" 2>$null
        $fetch = $clone -or $LASTEXITCODE -ne 0
    }

    $PSNativeCommandUseErrorActionPreference = $true
    if (-not $LibraryRoot) {
        if ($clone) {
            git clone --filter=blob:none --no-checkout https://github.com/danigutsch/PandaBear.SharedKernel.git $sourceDir
            git -C $sourceDir sparse-checkout set --cone tools/SharedKernel.CI.Tool tools/Shared
        }
        if ($fetch) {
            git -C $sourceDir fetch --filter=blob:none origin $revision
        }
        git -C $sourceDir checkout --quiet --detach $revision
    }

    Push-Location -LiteralPath $sourceDir
    try {
        dotnet restore tools/SharedKernel.CI.Tool --locked-mode
        dotnet build tools/SharedKernel.CI.Tool -c Release --no-restore
    }
    finally {
        Pop-Location
    }

    $stageSource = '--repository', 'danigutsch/PandaBear.SharedKernel'
    if ($Feed) {
        $stageSource = '--feed', $Feed
    }
    dotnet run --project (Join-Path $sourceDir "tools/SharedKernel.CI.Tool") -c Release --no-build -- `
        stage-candidate --pin .config/sharedkernel-candidate.json --library-root $sourceDir `
        --output .local/sharedkernel-feed @stageSource
    Write-Host "   ✅ SharedKernel candidate staged" -ForegroundColor Green
}

function Restore-DotNetDependencies {
    Write-Host "`n📦 Restoring .NET dependencies..." -ForegroundColor Yellow
    dotnet restore --locked-mode
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ .NET dependencies restored" -ForegroundColor Green
    }
    else {
        Write-Error "Failed to restore .NET dependencies in locked mode"
        exit 1
    }
}

function Restore-DotNetLocalTools {
    Write-Host "`n🔧 Restoring .NET local tools..." -ForegroundColor Yellow
    dotnet tool restore
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ .NET tools restored (dotnet-ef, reportgenerator, Aspire CLI)" -ForegroundColor Green
        Write-Host "   💡 Run the repo-pinned Aspire CLI with: dotnet tool run aspire run" -ForegroundColor Cyan
    }
    else {
        Write-Error "Failed to restore .NET tools"
        exit 1
    }
}

function Test-AspNetCoreDevelopmentCertificateTrust {
    if (-not $IsLinux) {
        return
    }

    Write-Host "`n🔐 Checking ASP.NET Core development certificate trust..." -ForegroundColor Yellow

    $devCertTrustDir = Join-Path $HOME ".aspnet/dev-certs/trust"
    dotnet dev-certs https --check --trust *> $null

    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ ASP.NET Core development certificate is trusted" -ForegroundColor Green
    }
    else {
        Write-Host "   ⚠️ ASP.NET Core development certificate is not trusted" -ForegroundColor Yellow
        Write-Host "   💡 Trust it with:" -ForegroundColor Cyan
        Write-Host "      dotnet dev-certs https --clean" -ForegroundColor Cyan
        Write-Host "      dotnet dev-certs https --trust" -ForegroundColor Cyan
    }

    $sslCertDirEntries = @()
    if (-not [string]::IsNullOrWhiteSpace($env:SSL_CERT_DIR)) {
        $sslCertDirEntries = $env:SSL_CERT_DIR.Split(':', [System.StringSplitOptions]::RemoveEmptyEntries)
    }

    if ($sslCertDirEntries -contains $devCertTrustDir) {
        Write-Host "   ✅ SSL_CERT_DIR includes Aspire development certificate trust path" -ForegroundColor Green
    }
    else {
        Write-Host "   ⚠️ SSL_CERT_DIR does not include $devCertTrustDir" -ForegroundColor Yellow
        Write-Host "   💡 Add this to ~/.zshrc or ~/.bashrc:" -ForegroundColor Cyan
        Write-Host '      if [ -z "$SSL_CERT_DIR" ]; then' -ForegroundColor Cyan
        Write-Host "        export SSL_CERT_DIR=\"/usr/lib/ssl/certs:$devCertTrustDir\"" -ForegroundColor Cyan
        Write-Host '      else' -ForegroundColor Cyan
        Write-Host "        export SSL_CERT_DIR=\"`$SSL_CERT_DIR:$devCertTrustDir\"" -ForegroundColor Cyan
        Write-Host '      fi' -ForegroundColor Cyan
        Write-Host "      Then restart your shell before running Aspire or E2E tests." -ForegroundColor Cyan
    }
}

function Test-PowerShellAndPlaywrightPrerequisites {
    Write-Host "`n🔍 Checking pwsh (PowerShell 7+) and Playwright prerequisites..." -ForegroundColor Yellow
    $pwshCommand = Get-Command pwsh -ErrorAction SilentlyContinue
    if ($pwshCommand) {
        Write-Host "   ✅ pwsh (PowerShell 7+) installed: $($pwshCommand.Source)" -ForegroundColor Green
        Write-Host "   ✅ Playwright browser installation can use sharedkernel-ci install-playwright after build" -ForegroundColor Green
        Write-Host "   💡 After dotnet build, install Playwright browsers with: dotnet tool run sharedkernel-ci -- install-playwright --project tests/ViajantesTurismo.HostedTests/ViajantesTurismo.HostedTests.csproj --browser chromium" -ForegroundColor Cyan
    }
    else {
        Write-Host "   ⚠️ pwsh (PowerShell 7+) not available - PowerShell script linting and Playwright browser installation will be skipped" -ForegroundColor Yellow
        Write-Host "   💡 Install pwsh (PowerShell 7+) from: https://github.com/PowerShell/PowerShell" -ForegroundColor Cyan
        return
    }

    $psaInstalled = Get-Module -ListAvailable -Name PSScriptAnalyzer
    if ($psaInstalled) {
        Write-Host "   ✅ PSScriptAnalyzer already installed" -ForegroundColor Green
    }
    else {
        Write-Host "   ⚠️ PSScriptAnalyzer not available - local PowerShell linting will be skipped" -ForegroundColor Yellow
        Write-Host "   💡 Install it only if you plan to lint PowerShell scripts:" -ForegroundColor Cyan
        Write-Host "      Install-Module -Name PSScriptAnalyzer -Scope CurrentUser -Force" -ForegroundColor Cyan
    }
}

function Test-K6Prerequisites {
    Write-Host "`n📈 Checking optional performance testing tooling..." -ForegroundColor Yellow
    $k6Command = Get-Command k6 -ErrorAction SilentlyContinue

    if ($k6Command) {
        Write-Host "   ✅ k6 installed: $($k6Command.Source)" -ForegroundColor Green
        Write-Host "   💡 Run the Admin smoke scenario with:" -ForegroundColor Cyan
        Write-Host "      `$env:VT_API_BASE_URL = '<admin-api-url>'; dotnet tool run sharedkernel-k6 -- run --scenario admin-smoke" -ForegroundColor Cyan
    }
    else {
        Write-Host "   ⚠️ k6 not available - performance/load testing will be skipped locally" -ForegroundColor Yellow
        Write-Host "   💡 Install k6 only if you plan to run tests under tests/performance/:" -ForegroundColor Cyan
        Write-Host "      Linux: https://grafana.com/docs/k6/latest/set-up/install-k6/" -ForegroundColor Cyan
        Write-Host "      macOS: brew install k6" -ForegroundColor Cyan
        Write-Host "      Windows: winget install k6 --source winget" -ForegroundColor Cyan
    }
}

function Write-SetupSummary {
    Write-Host "`n✨ Setup Complete!" -ForegroundColor Green
    Write-Host "==================`n" -ForegroundColor Green

    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Run the application: dotnet tool run aspire run" -ForegroundColor White
    Write-Host "  2. Generate OpenAPI artifacts (once per clone or worktree, needed by contract tests): dotnet tool run sharedkernel-ci -- openapi refresh`n     Then run tests: dotnet test" -ForegroundColor White
    Write-Host "  3. Install Playwright browsers after build: dotnet tool run sharedkernel-ci -- install-playwright --project tests/ViajantesTurismo.HostedTests/ViajantesTurismo.HostedTests.csproj --browser chromium" -ForegroundColor White
    Write-Host "  4. Optional performance smoke run: `$env:VT_API_BASE_URL = '<admin-api-url>'; dotnet tool run sharedkernel-k6 -- run --scenario admin-smoke" -ForegroundColor White
    Write-Host "  5. Validate a commit message: dotnet tool run sharedkernel-ci -- verify-commit-message --file /path/to/message.txt" -ForegroundColor White
    Write-Host "     (If Aspire CLI is installed globally or via the official install script, 'aspire run' also works.)" -ForegroundColor DarkGray
    Write-Host "  6. Tool inventory reference: README.md (required local, optional local, CI-only, and devcontainer-provided tools)" -ForegroundColor White
    Write-Host ""
}

Write-SetupHeader
Test-DotNetSdkVersion -InstallMissingSdk $InstallSdk
Set-Location -LiteralPath $repoRoot
Invoke-SharedKernelCandidateStaging
Restore-DotNetDependencies
Restore-DotNetLocalTools
Test-AspNetCoreDevelopmentCertificateTrust
Test-PowerShellAndPlaywrightPrerequisites
Test-K6Prerequisites
Write-SetupSummary
