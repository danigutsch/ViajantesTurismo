#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string] $Path
)

$ErrorActionPreference = 'Stop'

$resolvedPath = (Resolve-Path -LiteralPath $Path).Path

if (-not (Get-Module -ListAvailable -Name PSScriptAnalyzer)) {
    throw 'PSScriptAnalyzer is required for PowerShell formatting. Install it with: Install-Module -Name PSScriptAnalyzer -Scope CurrentUser -Force'
}

Import-Module PSScriptAnalyzer

$source = Get-Content -LiteralPath $resolvedPath -Raw
$formatted = Invoke-Formatter -ScriptDefinition $source

if (-not $formatted.EndsWith("`n", [System.StringComparison]::Ordinal)) {
    $formatted = $formatted + "`n"
}

Set-Content -LiteralPath $resolvedPath -Value $formatted -NoNewline
