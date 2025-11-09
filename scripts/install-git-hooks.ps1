# Install git hooks for Windows/PowerShell
# This script copies the pre-commit hook to the .git/hooks directory

$hookSource = "scripts/pre-commit"
$hookDest = ".git/hooks/pre-commit"

if (-Not (Test-Path $hookSource)) {
    Write-Host "Error: Hook file not found at $hookSource" -ForegroundColor Red
    exit 1
}

if (-Not (Test-Path ".git")) {
    Write-Host "Error: Not a git repository (no .git directory found)" -ForegroundColor Red
    exit 1
}

# Create hooks directory if it doesn't exist
$hooksDir = ".git/hooks"
if (-Not (Test-Path $hooksDir)) {
    New-Item -Path $hooksDir -ItemType Directory | Out-Null
}

# Copy the hook
Copy-Item -Path $hookSource -Destination $hookDest -Force

Write-Host "✓ Pre-commit hook installed successfully" -ForegroundColor Green
Write-Host "  The hook will lint markdown, shell scripts, PowerShell, and .NET code before each commit" -ForegroundColor Gray
Write-Host "  Use 'git commit --no-verify' to bypass the hook if needed" -ForegroundColor Gray
