#!/usr/bin/env pwsh
# ViajantesTurismo Development Environment Setup Script
# This script verifies required tooling and points to optional local tools

param()

$ErrorActionPreference = "Stop"

function Write-SetupHeader {
    Write-Host "`n🚀 ViajantesTurismo Development Setup" -ForegroundColor Cyan
    Write-Host "====================================`n" -ForegroundColor Cyan
}

function Test-DotNetSdkVersion {
    Write-Host "📦 Checking .NET SDK..." -ForegroundColor Yellow
    $globalJson = Get-Content "global.json" | ConvertFrom-Json
    $requiredVersion = $globalJson.sdk.version
    $installedSdk = dotnet --version 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ .NET SDK installed: $installedSdk" -ForegroundColor Green
        if ($installedSdk -ne $requiredVersion) {
            Write-Output "Install the exact SDK from: https://dotnet.microsoft.com/download/dotnet/10.0"
            Write-Output "   Re-run setup after the exact SDK is installed so locked restore uses the same toolchain as CI."
            Write-Error "Required version: $requiredVersion"
            exit 1
        }
    }
    else {
        Write-Output "Download .NET $requiredVersion from: https://dotnet.microsoft.com/download/dotnet/10.0"
        Write-Error ".NET SDK not found"
        exit 1
    }
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
        Write-Host "   ⚠️ Failed to restore .NET tools" -ForegroundColor Yellow
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
        Write-Host "   ✅ Playwright browser installation can use scripts/install-playwright.sh after build" -ForegroundColor Green
        Write-Host "   💡 After dotnet build, install Playwright browsers with: bash scripts/install-playwright.sh" -ForegroundColor Cyan
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
        Write-Host "      `$env:VT_API_BASE_URL = '<admin-api-url>'; scripts/run-admin-performance-smoke.ps1" -ForegroundColor Cyan
    }
    else {
        Write-Host "   ⚠️ k6 not available - performance/load testing scripts will be skipped locally" -ForegroundColor Yellow
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
    Write-Host "  2. Run tests: dotnet test" -ForegroundColor White
    Write-Host "  3. Install Playwright browsers after build: bash scripts/install-playwright.sh" -ForegroundColor White
    Write-Host "  4. Optional performance smoke run: `$env:VT_API_BASE_URL = '<admin-api-url>'; scripts/run-admin-performance-smoke.ps1" -ForegroundColor White
    Write-Host "  5. Validate a commit message: bash scripts/validate-commit-message.sh /path/to/message.txt" -ForegroundColor White
    Write-Host "     (If Aspire CLI is installed globally or via the official install script, 'aspire run' also works.)" -ForegroundColor DarkGray
    Write-Host "  6. Tool inventory reference: README.md (required local, optional local, CI-only, and devcontainer-provided tools)" -ForegroundColor White
    Write-Host ""
}

Write-SetupHeader
Test-DotNetSdkVersion
Restore-DotNetDependencies
Restore-DotNetLocalTools
Test-AspNetCoreDevelopmentCertificateTrust
Test-PowerShellAndPlaywrightPrerequisites
Test-K6Prerequisites
Write-SetupSummary
