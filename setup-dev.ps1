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
        Write-Host "   ⚠️  Required version: $requiredVersion" -ForegroundColor Yellow
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
    Write-Host "   ⚠️  Failed to restore .NET tools" -ForegroundColor Yellow
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
        Write-Host "   ⚠️  Failed to install PSScriptAnalyzer: $_" -ForegroundColor Yellow
    }
}

# Check Node.js and npm
if (-not $SkipNpm) {
    Write-Host "`n📦 Checking Node.js and npm..." -ForegroundColor Yellow
    $nodeVersion = node --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Node.js installed: $nodeVersion" -ForegroundColor Green
        
        Write-Host "`n📦 Installing npm dependencies..." -ForegroundColor Yellow
        npm install
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   ✅ npm dependencies installed (markdownlint-cli, shellcheck, shfmt)" -ForegroundColor Green
        } else {
            Write-Host "   ❌ Failed to install npm dependencies" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "   ⚠️  Node.js not found - markdown linting will not be available" -ForegroundColor Yellow
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
            Write-Host "   ⚠️  Pre-commit hook script not found at scripts/pre-commit" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   ⚠️  Not a git repository - skipping hook installation" -ForegroundColor Yellow
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
