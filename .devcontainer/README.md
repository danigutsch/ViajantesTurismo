# Dev Container Configuration

This directory contains the configuration for developing ViajantesTurismo in a containerized environment using
 VS Code Dev Containers.

For contributor-facing usage guidance, see [../docs/DEVCONTAINERS.md](../docs/DEVCONTAINERS.md).

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop) installed and running
- [Visual Studio Code](https://code.visualstudio.com/) with the
 [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)

## What's Included

### Development Container

Ubuntu-based container with the following tools installed via features:

- **.NET 10 SDK**: For building and running the application
- **Node.js 24**: For npm scripts and development tools
- **Git**: Version control
- **Docker-in-Docker**: Run a Docker daemon inside the dev container for Aspire-managed resources

### VS Code Extensions

Essential extensions automatically installed:

- **C# Dev Kit** (or ReSharper - see customization below)
- **C# Language Support**
- **.NET Aspire**: For managing distributed applications
- **Docker Tools**: Container management
- **EditorConfig**: Code style consistency
- **ShellCheck**: Shell script linting
- **PowerShell**: PowerShell script support
- **Markdownlint**: Markdown formatting

### Infrastructure

**Aspire manages all infrastructure** including:

- PostgreSQL database
- Redis cache
- Service discovery and orchestration

No additional database containers are configured in the dev container - Aspire handles everything
 when you run the AppHost.

## Getting Started

1. **Open in Dev Container**:
   - Open VS Code
   - Press `F1` and select "Dev Containers: Reopen in Container"
   - Wait for the container to build and initialize (first time may take several minutes)

2. **Verify Setup**:

   ```bash
   dotnet --version    # Should show 10.x
   node --version      # Should show v24.x
   git --version
   ```

3. **Run the Application**:

   ```bash
   # Preferred when using the repo-pinned local .NET tool manifest
   dotnet tool run aspire run

   # Alternative using only the .NET SDK
   dotnet run --project src/ViajantesTurismo.AppHost

   # If you installed Aspire CLI globally or via the install script
   aspire run
   ```

   The repository pins `aspire.cli` as a local .NET tool, so `dotnet tool run aspire run` works reliably inside a new
   dev container even when a standalone `aspire` command is not on `PATH`.

   The Aspire dashboard will open automatically, showing all running services and their dynamically assigned ports.

4. **Run Tests**:

   ```bash
   dotnet test
   ```

5. **Run the Shared Smoke Validation (Optional)**:

   ```bash
   bash scripts/run-devcontainer-smoke.sh
   ```

   This runs the same non-interactive devcontainer smoke path used by
   `.github/workflows/devcontainer-smoke.yml` and writes logs to
   `TestResults/devcontainer-smoke`.

   If you also want the temporary container to run the full solution test suite,
   use:

   ```bash
   bash scripts/run-devcontainer-smoke.sh --run-tests
   ```

## Port Forwarding

Aspire assigns all ports dynamically. VS Code will automatically detect and forward ports as services start. You can
 view forwarded ports in the "Ports" panel.

## Configuration Files

- **`devcontainer.json`**: Main dev container configuration with features, extensions, and settings
- **`post-create.sh`**: Automated setup script that runs after container creation
- **`README.md`**: This file

## Customization

### Adding Extensions

Edit `.devcontainer/devcontainer.json` and add extension IDs to the `extensions` array:

```json
"extensions": [
  "publisher.extension-id"
]
```

### Environment Variables

Environment variables are pre-configured in `devcontainer.json`:

- `DOTNET_CLI_TELEMETRY_OPTOUT=1`: Disable telemetry
- `DOTNET_NOLOGO=1`: Suppress .NET welcome message
- `ASPNETCORE_ENVIRONMENT=Development`: Set environment
- `DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1`: Enable hot reload for breaking changes

## Performance Optimizations

### NuGet Package Cache

The dev container mounts your local NuGet packages folder to avoid re-downloading packages:

- Windows: `C:\Users\<username>\.nuget\packages` → `/home/vscode/.nuget/packages`
- Also mounts `.aspnet` folder for ASP.NET settings persistence

### Post-Create Script

On first container creation, the `post-create.sh` script automatically:

1. Restores .NET tools
2. Restores NuGet packages
3. Installs npm packages
4. Sets up git hooks
5. Builds the solution to verify everything works

## Troubleshooting

### Container Build Fails

1. Ensure your container runtime is running
2. Try rebuilding without cache: `F1` → "Dev Containers: Rebuild Container Without Cache"

### Aspire Services Won't Start

- Ensure Docker access from the dev container is working: run `docker ps` in the dev container terminal
- Check your container runtime has enough resources allocated
- Rebuild the dev container after changing `.devcontainer/devcontainer.json` so the inner Docker daemon is reprovisioned

### Performance Issues on Windows

- Allocate more resources to your container runtime
- The NuGet package mount significantly improves restore performance

## Working Outside the Container

The dev container is optional. You can develop on your host machine if you have:

- .NET 10 SDK installed
- Node.js 24 installed
- A container runtime running (for Aspire)

Aspire will work the same way whether inside or outside the container.

## Additional Resources

- [Dev Containers Documentation](https://code.visualstudio.com/docs/devcontainers/containers)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Docker Documentation](https://docs.docker.com/)
