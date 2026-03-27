#!/usr/bin/env pwsh
# ViajantesTurismo Development Environment Setup Script
# This script installs all required and optional tools for development

param(
    [switch]$SkipGitHook,
    [switch]$SkipNpm
)

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
            Write-Host "   ⚠️ Required version: $requiredVersion" -ForegroundColor Yellow
            Write-Host "   💡 Download from: https://dotnet.microsoft.com/download/dotnet/10.0" -ForegroundColor Cyan
        }
    }
    else {
        Write-Host "   ❌ .NET SDK not found" -ForegroundColor Red
        Write-Host "   💡 Download .NET $requiredVersion from: https://dotnet.microsoft.com/download/dotnet/10.0" -ForegroundColor Cyan
        exit 1
    }
}

function Restore-DotNetDependencies {
    Write-Host "`n📦 Restoring .NET dependencies..." -ForegroundColor Yellow
    dotnet restore
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ .NET dependencies restored" -ForegroundColor Green
    }
    else {
        Write-Host "   ❌ Failed to restore .NET dependencies" -ForegroundColor Red
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
        Write-Host "   📦 Installing PSScriptAnalyzer..." -ForegroundColor Yellow
        try {
            Install-Module -Name PSScriptAnalyzer -Scope CurrentUser -Force -SkipPublisherCheck
            Write-Host "   ✅ PSScriptAnalyzer installed" -ForegroundColor Green
        }
        catch {
            Write-Host "   ⚠️ Failed to install PSScriptAnalyzer: $_" -ForegroundColor Yellow
        }
    }
}

function Test-NodeJsAndNpm {
    if ($SkipNpm) {
        return
    }

    Write-Host "`n📦 Checking Node.js and npm..." -ForegroundColor Yellow

    $requiredNodeVersion = ""
    if (Test-Path ".nvmrc") {
        $requiredNodeVersion = (Get-Content ".nvmrc" -Raw).Trim()
    }
    else {
        Write-Host "   ⚠️ .nvmrc file not found" -ForegroundColor Yellow
    }

    $nodeVersion = node --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Node.js installed: $nodeVersion" -ForegroundColor Green

        if ($requiredNodeVersion) {
            if ($nodeVersion -like "v$requiredNodeVersion*") {
                Write-Host "   ✅ Version matches .nvmrc ($requiredNodeVersion)" -ForegroundColor Green
            }
            else {
                Write-Host "   ⚠️ Version mismatch!" -ForegroundColor Yellow
                Write-Host "      Required: v$requiredNodeVersion (from .nvmrc)" -ForegroundColor Yellow
                Write-Host "      Installed: $nodeVersion" -ForegroundColor Yellow
                Write-Host "   💡 Switch with: nvm use" -ForegroundColor Cyan
            }
        }

        Write-Host ""
        Write-Host "Code quality linters available (optional):" -ForegroundColor Cyan
        Write-Host "  • markdownlint-cli - Markdown file linting" -ForegroundColor White
        Write-Host "  • shellcheck - Shell script linting" -ForegroundColor White
        Write-Host "  • shfmt - Shell script formatting" -ForegroundColor White
        Write-Host "  • gherkin-lint - BDD feature file linting" -ForegroundColor White
        Write-Host "  • ESLint - JSON file linting" -ForegroundColor White
        Write-Host ""
        $installLinters = Read-Host "Install code quality linters? (y/N)"

        if ($installLinters -eq 'y' -or $installLinters -eq 'Y') {
            Write-Host "`n📦 Installing npm dependencies..." -ForegroundColor Yellow
            npm install
            if ($LASTEXITCODE -eq 0) {
                Write-Host "   ✅ npm dependencies installed (markdownlint-cli, shellcheck, shfmt, gherkin-lint, ESLint)" -ForegroundColor Green
            }
            else {
                Write-Host "   ❌ Failed to install npm dependencies" -ForegroundColor Red
                exit 1
            }
        }
        else {
            Write-Host "   ⏭️ Skipping linter installation" -ForegroundColor Yellow
            Write-Host "   💡 Install later with: npm install" -ForegroundColor Cyan
        }
    }
    else {
        Write-Host "   ⚠️ Node.js not found - code quality linters will not be available" -ForegroundColor Yellow
        if ($requiredNodeVersion) {
            Write-Host "      Expected version: v$requiredNodeVersion (from .nvmrc)" -ForegroundColor Cyan
        }
        Write-Host "   💡 Download from: https://nodejs.org/" -ForegroundColor Cyan
    }
}

function Install-PreCommitHook {
    if ($SkipGitHook) {
        return
    }

    Write-Host "`n🪝 Setting up git hooks..." -ForegroundColor Yellow
    if (Test-Path ".git/hooks") {
        if ((Test-Path "scripts/pre-commit") -and (Test-Path "scripts/commit-msg")) {
            Copy-Item "scripts/pre-commit" ".git/hooks/pre-commit" -Force
            Copy-Item "scripts/commit-msg" ".git/hooks/commit-msg" -Force
            Write-Host "   ✅ Git hooks installed (pre-commit, commit-msg)" -ForegroundColor Green
            Write-Host "   💡 Bypass with: git commit --no-verify" -ForegroundColor Cyan
        }
        else {
            Write-Host "   ⚠️ Hook scripts not found at scripts/pre-commit and scripts/commit-msg" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "   ⚠️ Not a git repository - skipping hook installation" -ForegroundColor Yellow
    }
}

function Write-SetupSummary {
    Write-Host "`n✨ Setup Complete!" -ForegroundColor Green
    Write-Host "==================`n" -ForegroundColor Green

    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Run the application: dotnet tool run aspire run" -ForegroundColor White
    Write-Host "  2. Run tests: dotnet test" -ForegroundColor White
    Write-Host "  3. Install Playwright browsers after build: bash scripts/install-playwright.sh" -ForegroundColor White
    Write-Host "  4. Check markdown: npm run lint:md" -ForegroundColor White
    Write-Host "     (If Aspire CLI is installed globally or via the official install script, 'aspire run' also works.)" -ForegroundColor DarkGray
    Write-Host ""
}

Write-SetupHeader
Test-DotNetSdkVersion
Restore-DotNetDependencies
Restore-DotNetLocalTools
Test-AspNetCoreDevelopmentCertificateTrust
Test-PowerShellAndPlaywrightPrerequisites
Test-NodeJsAndNpm
Install-PreCommitHook
Write-SetupSummary
