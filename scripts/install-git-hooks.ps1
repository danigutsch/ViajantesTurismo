# Install git hooks for Windows/PowerShell
# This script copies the repository git hooks to the configured hooks directory.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$hooks = @(
    @{ Source = "scripts/pre-commit"; Name = "pre-commit" },
    @{ Source = "scripts/commit-msg"; Name = "commit-msg" }
)

try {
    $hooksDir = (& git rev-parse --git-path hooks).Trim()
}
catch {
    Write-Host "Error: Not a git repository" -ForegroundColor Red
    exit 1
}

if (-Not (Test-Path $hooksDir)) {
    New-Item -Path $hooksDir -ItemType Directory | Out-Null
}

foreach ($hook in $hooks) {
    if (-Not (Test-Path $hook.Source)) {
        Write-Host "Error: Hook file not found at $($hook.Source)" -ForegroundColor Red
        exit 1
    }

    $destinationPath = Join-Path $hooksDir $hook.Name
    Copy-Item -Path $hook.Source -Destination $destinationPath -Force
}

Write-Host "✓ Git hooks installed successfully" -ForegroundColor Green
Write-Host "  pre-commit lints and formats staged files before each commit" -ForegroundColor Gray
Write-Host "  commit-msg validates Conventional Commits with commitlint" -ForegroundColor Gray
Write-Host "  Use 'git commit --no-verify' to bypass hooks if needed" -ForegroundColor Gray
