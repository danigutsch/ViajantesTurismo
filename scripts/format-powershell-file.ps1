#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string] $Path
)

$ErrorActionPreference = 'Stop'

$resolvedPath = (Resolve-Path -LiteralPath $Path).Path

if (-not (Get-Module -ListAvailable -Name PSScriptAnalyzer)) {
    Write-Warning 'Optional: PSScriptAnalyzer is required for PowerShell autofix. Install it with: Install-Module -Name PSScriptAnalyzer -Scope CurrentUser. Skipping PowerShell formatting.'
    return
}

Import-Module PSScriptAnalyzer

$source = Get-Content -LiteralPath $resolvedPath -Raw
$formatted = Invoke-Formatter -ScriptDefinition $source
$formatted = $formatted -replace "`r`n?", "`n"

if (-not $formatted.EndsWith("`n", [System.StringComparison]::Ordinal)) {
    $formatted = $formatted + "`n"
}

Set-Content -LiteralPath $resolvedPath -Value $formatted -NoNewline -Encoding utf8
