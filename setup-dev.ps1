#!/usr/bin/env pwsh
# ViajantesTurismo Development Environment Setup Script
# This script installs all required and optional tools for development

param(
    [switch]$SkipGitHook,
    [switch]$SkipNpm
)

$ErrorActionPreference = "Stop"

Write-Host "`n🚀 ViajantesTurismo Development Setup" -ForegroundColor Cyan
Write-Host "====================================`n" -ForegroundColor Cyan

# Check .NET SDK version
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
} else {
    Write-Host "   ❌ .NET SDK not found" -ForegroundColor Red
    Write-Host "   💡 Download .NET $requiredVersion from: https://dotnet.microsoft.com/download/dotnet/10.0" -ForegroundColor Cyan
    exit 1
}

# Restore .NET dependencies
Write-Host "`n📦 Restoring .NET dependencies..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ .NET dependencies restored" -ForegroundColor Green
} else {
    Write-Host "   ❌ Failed to restore .NET dependencies" -ForegroundColor Red
    exit 1
}

# Restore .NET local tools
Write-Host "`n🔧 Restoring .NET local tools..." -ForegroundColor Yellow
dotnet tool restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ .NET tools restored (dotnet-ef, reportgenerator, Aspire CLI)" -ForegroundColor Green
} else {
    Write-Host "   ⚠️ Failed to restore .NET tools" -ForegroundColor Yellow
}

# Install PSScriptAnalyzer for PowerShell linting
Write-Host "`n🔍 Checking PSScriptAnalyzer..." -ForegroundColor Yellow
$psaInstalled = Get-Module -ListAvailable -Name PSScriptAnalyzer
if ($psaInstalled) {
    Write-Host "   ✅ PSScriptAnalyzer already installed" -ForegroundColor Green
} else {
    Write-Host "   📦 Installing PSScriptAnalyzer..." -ForegroundColor Yellow
    try {
        Install-Module -Name PSScriptAnalyzer -Scope CurrentUser -Force -SkipPublisherCheck
        Write-Host "   ✅ PSScriptAnalyzer installed" -ForegroundColor Green
    } catch {
        Write-Host "   ⚠️ Failed to install PSScriptAnalyzer: $_" -ForegroundColor Yellow
    }
}

# Check Node.js and npm
if (-not $SkipNpm) {
    Write-Host "`n📦 Checking Node.js and npm..." -ForegroundColor Yellow
    
    # Read required Node.js version from .nvmrc
    $requiredNodeVersion = ""
    if (Test-Path ".nvmrc") {
        $requiredNodeVersion = (Get-Content ".nvmrc" -Raw).Trim()
    } else {
        Write-Host "   ⚠️ .nvmrc file not found" -ForegroundColor Yellow
    }
    
    $nodeVersion = node --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Node.js installed: $nodeVersion" -ForegroundColor Green
        
        # Validate version matches .nvmrc
        if ($requiredNodeVersion) {
            if ($nodeVersion -like "v$requiredNodeVersion*") {
                Write-Host "   ✅ Version matches .nvmrc ($requiredNodeVersion)" -ForegroundColor Green
            } else {
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
            } else {
                Write-Host "   ❌ Failed to install npm dependencies" -ForegroundColor Red
                exit 1
            }
        } else {
            Write-Host "   ⏭️ Skipping linter installation" -ForegroundColor Yellow
            Write-Host "   💡 Install later with: npm install" -ForegroundColor Cyan
        }
    } else {
        Write-Host "   ⚠️ Node.js not found - code quality linters will not be available" -ForegroundColor Yellow
        if ($requiredNodeVersion) {
            Write-Host "      Expected version: v$requiredNodeVersion (from .nvmrc)" -ForegroundColor Cyan
        }
        Write-Host "   💡 Download from: https://nodejs.org/" -ForegroundColor Cyan
    }
}

# Install pre-commit hook (optional)
if (-not $SkipGitHook) {
    Write-Host "`n🪝 Setting up git pre-commit hook..." -ForegroundColor Yellow
    if (Test-Path ".git/hooks") {
        if (Test-Path "scripts/pre-commit") {
            Copy-Item "scripts/pre-commit" ".git/hooks/pre-commit" -Force
            Write-Host "   ✅ Pre-commit hook installed" -ForegroundColor Green
            Write-Host "   💡 Bypass with: git commit --no-verify" -ForegroundColor Cyan
        } else {
            Write-Host "   ⚠️ Pre-commit hook script not found at scripts/pre-commit" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   ⚠️ Not a git repository - skipping hook installation" -ForegroundColor Yellow
    }
}

# Summary
Write-Host "`n✨ Setup Complete!" -ForegroundColor Green
Write-Host "==================`n" -ForegroundColor Green

Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Run the application: dotnet run --project src/ViajantesTurismo.AppHost" -ForegroundColor White
Write-Host "  2. Run tests: dotnet test" -ForegroundColor White
Write-Host "  3. Check markdown: npm run lint:md" -ForegroundColor White
Write-Host ""
