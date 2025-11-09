#!/bin/bash

set -e

echo "🚀 Running post-create setup..."

# Restore .NET tools
echo "📦 Restoring .NET tools..."
dotnet tool restore

# Restore NuGet packages
echo "📦 Restoring NuGet packages..."
dotnet restore ViajantesTurismo.slnx

# Install npm packages
echo "📦 Installing npm packages..."
npm install

# Install git hooks
echo "🪝 Installing git hooks..."
if [ -f "scripts/install-git-hooks.sh" ]; then
    bash scripts/install-git-hooks.sh
fi

# Build the solution to verify everything works
echo "🔨 Building solution..."
dotnet build ViajantesTurismo.slnx --no-restore

# Run database migrations if needed
# Uncomment when you're ready to apply migrations automatically
# echo "🗄️ Applying database migrations..."
# dotnet ef database update --project src/ViajantesTurismo.Admin.Infrastructure

echo "✅ Post-create setup completed successfully!"
echo ""
echo "🎉 Your development environment is ready!"
echo ""
echo "Next steps:"
echo "  - Run 'dotnet run --project src/ViajantesTurismo.AppHost' to start the Aspire app"
echo "  - Run 'dotnet test' to execute all tests"
echo "  - Run 'dotnet watch --project src/ViajantesTurismo.Admin.Web' for hot reload"
echo ""
