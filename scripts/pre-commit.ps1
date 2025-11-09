# Pre-commit hook to lint markdown files

$changedMdFiles = git diff --cached --name-only --diff-filter=ACM | Where-Object { $_ -match '\.md$' }

if ($changedMdFiles) {
    Write-Host "Linting markdown files..." -ForegroundColor Cyan
    
    npx markdownlint-cli $changedMdFiles --config .markdownlint.json
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`n❌ Markdown linting failed. Fix errors or use --no-verify to skip." -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ Markdown linting passed" -ForegroundColor Green
}

exit 0