param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $K6Arguments
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($env:VT_API_BASE_URL)) {
    Write-Error 'VT_API_BASE_URL is required, for example http://127.0.0.1:5510'
}

$scriptDirectory = Split-Path -Parent $PSCommandPath
$repositoryRoot = (Resolve-Path (Join-Path $scriptDirectory '..')).Path
$profile = if ([string]::IsNullOrWhiteSpace($env:VT_K6_PROFILE)) { 'smoke' } else { $env:VT_K6_PROFILE }
$useDocker = if ([string]::IsNullOrWhiteSpace($env:VT_K6_USE_DOCKER)) { 'auto' } else { $env:VT_K6_USE_DOCKER }
$apiBaseUrl = $env:VT_API_BASE_URL.TrimEnd('/')
$dockerK6Image = if ([string]::IsNullOrWhiteSpace($env:VT_K6_DOCKER_IMAGE)) { 'grafana/k6:0.49.0' } else { $env:VT_K6_DOCKER_IMAGE }
$resultsDirectory = if ([string]::IsNullOrWhiteSpace($env:VT_K6_RESULTS_DIR)) { 'tests/performance/results' } else { $env:VT_K6_RESULTS_DIR }

if ([System.IO.Path]::IsPathRooted($resultsDirectory)) {
    Write-Error 'VT_K6_RESULTS_DIR must be relative to the repository root.'
}

$normalizedResultsDirectory = $resultsDirectory.Replace('\', '/').TrimEnd('/')
if ($normalizedResultsDirectory.Contains(':')) {
    Write-Error 'VT_K6_RESULTS_DIR must be relative to the repository root.'
}

if ([string]::IsNullOrWhiteSpace($normalizedResultsDirectory) -or $normalizedResultsDirectory.Contains('..') -or $normalizedResultsDirectory.Contains('//')) {
    Write-Error 'VT_K6_RESULTS_DIR must stay inside the repository root and must not contain .. segments.'
}

if ($profile -notmatch '^[A-Za-z0-9_-]+$') {
    Write-Error 'VT_K6_PROFILE may contain only letters, numbers, underscores, and hyphens.'
}

$timestamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
$summaryFile = "$normalizedResultsDirectory/admin-smoke-$profile-$timestamp.json"
New-Item -ItemType Directory -Force -Path (Join-Path $repositoryRoot $resultsDirectory) | Out-Null
Set-Location $repositoryRoot

function Test-CommandAvailable([string] $CommandName) {
    $null -ne (Get-Command $CommandName -ErrorAction SilentlyContinue)
}

if ($useDocker -eq 'auto') {
    if (Test-CommandAvailable 'k6') {
        $useDocker = '0'
    }
    elseif (Test-CommandAvailable 'docker') {
        $useDocker = '1'
    }
    else {
        Write-Error 'k6 is required but was not found on PATH, and docker is unavailable.'
    }
}

if ($useDocker -ne '0' -and $useDocker -ne '1') {
    Write-Error 'VT_K6_USE_DOCKER must be auto, 0, or 1.'
}

$apiHealthUrl = "$apiBaseUrl/health"
for ($attempt = 1; $attempt -le 30; $attempt++) {
    try {
        Invoke-WebRequest -Uri $apiHealthUrl -UseBasicParsing | Out-Null
        break
    }
    catch {
        Start-Sleep -Seconds 1
    }
}

try {
    Invoke-WebRequest -Uri $apiHealthUrl -UseBasicParsing | Out-Null
}
catch {
    Write-Error "Admin API is not reachable at $apiHealthUrl"
}

if ($useDocker -eq '0') {
    if (-not (Test-CommandAvailable 'k6')) {
        Write-Error 'k6 is required when VT_K6_USE_DOCKER=0.'
    }

    & k6 run `
        --summary-export $summaryFile `
        -e "VT_API_BASE_URL=$apiBaseUrl" `
        -e "VT_K6_PROFILE=$profile" `
        @K6Arguments `
        tests/performance/k6/scenarios/admin-smoke.js
    exit $LASTEXITCODE
}

if (-not (Test-CommandAvailable 'docker')) {
    Write-Error 'docker is required when VT_K6_USE_DOCKER=1.'
}

$dockerApiBaseUrl = $apiBaseUrl
$dockerAddHostArguments = @()
if ($dockerApiBaseUrl -match '^https?://(127\.0\.0\.1|localhost)(:|/|$)') {
    $dockerApiBaseUrl = $dockerApiBaseUrl.Replace('127.0.0.1', 'host.docker.internal').Replace('localhost', 'host.docker.internal')
    $dockerAddHostArguments += @('--add-host', 'host.docker.internal:host-gateway')
}

$dockerEnvironmentArguments = @(
    '-e', "VT_API_BASE_URL=$dockerApiBaseUrl",
    '-e', "VT_K6_PROFILE=$profile"
)

if (-not [string]::IsNullOrWhiteSpace($env:VT_K6_VUS)) {
    $dockerEnvironmentArguments += @('-e', "VT_K6_VUS=$env:VT_K6_VUS")
}

if (-not [string]::IsNullOrWhiteSpace($env:VT_K6_DURATION)) {
    $dockerEnvironmentArguments += @('-e', "VT_K6_DURATION=$env:VT_K6_DURATION")
}

& docker run --rm `
    @dockerAddHostArguments `
    -v "${repositoryRoot}:/work" `
    -w /work `
    $dockerK6Image run `
    --summary-export $summaryFile `
    @dockerEnvironmentArguments `
    @K6Arguments `
    tests/performance/k6/scenarios/admin-smoke.js
exit $LASTEXITCODE
