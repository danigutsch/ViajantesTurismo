#!/bin/bash

set -euo pipefail

# Error handler with line number
trap 'echo "❌ Setup failed at line $LINENO with exit code $?" >&2; exit 1' ERR

echo "🚀 Running post-create setup..."

# Restore NuGet packages
echo "📦 Restoring NuGet packages..."
dotnet restore ViajantesTurismo.slnx

# Build the solution to verify everything works (optional)
if [[ "${DEVCONTAINER_VERIFY_BUILD:-1}" == "1" ]]; then
    echo "🔨 Building solution..."
    if ! dotnet build ViajantesTurismo.slnx --no-restore; then
        echo "❌ Build failed. Please check the error messages above." >&2
        exit 1
    fi
else
    echo "⏭️ Skipping build verification (DEVCONTAINER_VERIFY_BUILD=${DEVCONTAINER_VERIFY_BUILD:-0})"
fi

# Run database migrations if needed
# Uncomment when you're ready to apply migrations automatically
# echo "🗄️ Applying database migrations..."
# dotnet ef database update --project src/ViajantesTurismo.Admin.Infrastructure

echo "✅ Post-create setup completed successfully!"
echo ""
echo "🎉 Your development environment is ready!"
echo ""
echo "Next steps:"
echo "  - Run 'dotnet tool run aspire run' to start the Aspire app"
echo "    (If Aspire CLI is installed globally or via the official install script, 'aspire run' also works.)"
echo "  - Run 'dotnet test' to execute all tests"
echo "  - Run 'dotnet watch --project src/ViajantesTurismo.Admin.Web' for hot reload"
echo ""
