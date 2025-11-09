#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generates invariant coverage report and validates 100% coverage requirement.

.DESCRIPTION
    This script runs the behavior tests and checks that all documented domain invariants
    have corresponding test coverage. The script will fail if coverage is below the
    required threshold, making it suitable for CI/CD pipelines.

.PARAMETER MinimumCoverage
    Minimum required coverage percentage (default: 100.0)

.PARAMETER TestProject
    Path to the behavior tests project (default: tests/ViajantesTurismo.Admin.BehaviorTests)

.PARAMETER ReportPath
    Path where the coverage report will be generated (default: TestResults/InvariantCoverage.md)

.PARAMETER FailOnUncovered
    Whether to fail the build if coverage is below minimum (default: true)

.EXAMPLE
    ./scripts/generate-invariant-coverage.ps1
    Runs with default settings (100% coverage required)

.EXAMPLE
    ./scripts/generate-invariant-coverage.ps1 -MinimumCoverage 95.0 -FailOnUncovered:$false
    Runs with 95% threshold and warning-only mode
#>

[CmdletBinding()]
param(
    [Parameter()]
    [double]$MinimumCoverage = 100.0,

    [Parameter()]
    [string]$TestProject = "tests/ViajantesTurismo.Admin.BehaviorTests",

    [Parameter()]
    [string]$ReportPath = "TestResults/InvariantCoverage.json",

    [Parameter()]
    [bool]$FailOnUncovered = $true
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Colors for output
$ColorSuccess = "Green"
$ColorWarning = "Yellow"
$ColorError = "Red"
$ColorInfo = "Cyan"

Write-Host "`n========================================" -ForegroundColor $ColorInfo
Write-Host "  Invariant Coverage Validation" -ForegroundColor $ColorInfo
Write-Host "========================================`n" -ForegroundColor $ColorInfo

# Step 1: Run behavior tests
Write-Host "Running behavior tests..." -ForegroundColor $ColorInfo
$testCommand = "dotnet test `"$TestProject`" --verbosity quiet --nologo"

try {
    $testOutput = Invoke-Expression $testCommand 2>&1
    $testExitCode = $LASTEXITCODE

    if ($testExitCode -ne 0) {
        Write-Host "`n❌ Tests failed with exit code: $testExitCode" -ForegroundColor $ColorError
        Write-Host $testOutput
        exit $testExitCode
    }

    Write-Host "✓ Tests completed successfully" -ForegroundColor $ColorSuccess
}
catch {
    Write-Host "`n❌ Error running tests: $_" -ForegroundColor $ColorError
    exit 1
}

# Step 2: Check if coverage report was generated
if (-not (Test-Path $ReportPath)) {
    Write-Host "`n❌ Coverage report not found at: $ReportPath" -ForegroundColor $ColorError
    Write-Host "Make sure InvariantCoverageHooks is configured correctly." -ForegroundColor $ColorWarning
    exit 1
}

Write-Host "✓ Coverage report found" -ForegroundColor $ColorSuccess

# Step 3: Parse coverage report from JSON
Write-Host "`nParsing coverage report..." -ForegroundColor $ColorInfo
$report = Get-Content $ReportPath -Raw | ConvertFrom-Json

$totalInvariants = $report.summary.totalInvariants
$coveredInvariants = $report.summary.coveredInvariants
$uncoveredCount = $report.summary.uncoveredCount
$coveragePercentage = $report.summary.coveragePercentage

# Step 4: Display coverage summary
Write-Host "`n========================================" -ForegroundColor $ColorInfo
Write-Host "  Coverage Summary" -ForegroundColor $ColorInfo
Write-Host "========================================" -ForegroundColor $ColorInfo
Write-Host "Total Invariants:    $totalInvariants" -ForegroundColor White
Write-Host "Covered:             $coveredInvariants" -ForegroundColor $ColorSuccess
Write-Host "Uncovered:           $uncoveredCount" -ForegroundColor $(if ($uncoveredCount -eq 0) { $ColorSuccess } else { $ColorWarning })
Write-Host "Coverage:            $coveragePercentage%" -ForegroundColor $(if ($coveragePercentage -ge $MinimumCoverage) { $ColorSuccess } else { $ColorError })
Write-Host "Required:            $MinimumCoverage%" -ForegroundColor White

# Step 5: Extract uncovered invariants if any
if ($uncoveredCount -gt 0) {
    Write-Host "`n⚠️  Uncovered Invariants:" -ForegroundColor $ColorWarning

    foreach ($inv in $report.uncoveredInvariants) {
        Write-Host "  • $inv" -ForegroundColor $ColorWarning
    }
}

# Step 6: Validate coverage threshold
Write-Host "`n========================================" -ForegroundColor $ColorInfo

if ($coveragePercentage -ge $MinimumCoverage) {
    Write-Host "✅ Coverage requirement met!" -ForegroundColor $ColorSuccess
    Write-Host "========================================`n" -ForegroundColor $ColorInfo
    exit 0
}
else {
    $deficit = $MinimumCoverage - $coveragePercentage
    Write-Host "❌ Coverage below required threshold" -ForegroundColor $ColorError
    Write-Host "   Gap: $($deficit.ToString('F1'))%" -ForegroundColor $ColorError
    Write-Host "========================================`n" -ForegroundColor $ColorInfo

    if ($FailOnUncovered) {
        Write-Host "Build failed: Invariant coverage requirement not met." -ForegroundColor $ColorError
        Write-Host "All documented business rules must have behavior test coverage.`n" -ForegroundColor $ColorWarning
        exit 1
    }
    else {
        Write-Host "⚠️  WARNING: Coverage requirement not met (enforcing disabled)`n" -ForegroundColor $ColorWarning
        exit 0
    }
}
