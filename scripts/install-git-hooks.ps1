# Install git hooks for Windows/PowerShell
# This script copies the repository git hooks to the .git/hooks directory

$hooks = @(
    @{ Source = "scripts/pre-commit"; Destination = ".git/hooks/pre-commit" },
    @{ Source = "scripts/commit-msg"; Destination = ".git/hooks/commit-msg" }
)

if (-Not (Test-Path ".git")) {
    Write-Host "Error: Not a git repository (no .git directory found)" -ForegroundColor Red
    exit 1
}

# Create hooks directory if it doesn't exist
$hooksDir = ".git/hooks"
if (-Not (Test-Path $hooksDir)) {
    New-Item -Path $hooksDir -ItemType Directory | Out-Null
}

foreach ($hook in $hooks) {
    if (-Not (Test-Path $hook.Source)) {
        Write-Host "Error: Hook file not found at $($hook.Source)" -ForegroundColor Red
        exit 1
    }

    Copy-Item -Path $hook.Source -Destination $hook.Destination -Force
}

Write-Host "✓ Git hooks installed successfully" -ForegroundColor Green
Write-Host "  pre-commit lints and formats staged files before each commit" -ForegroundColor Gray
Write-Host "  commit-msg validates Conventional Commits with commitlint" -ForegroundColor Gray
Write-Host "  Use 'git commit --no-verify' to bypass hooks if needed" -ForegroundColor Gray
