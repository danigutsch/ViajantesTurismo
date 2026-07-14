using System.Diagnostics;
using SharedKernel.AspNetCore;

namespace ViajantesTurismo.OpenApi.Tool;

internal static class OpenApiGenerationCommand
{
    private const string OpenApiGenerationEnvironment = "OpenApiGeneration";

    public static ProcessStartInfo CreateStartInfo(OpenApiGenerationOptions options, bool isCi)
    {
        ArgumentNullException.ThrowIfNull(options);

        var (project, property) = options.Target switch
        {
            OpenApiTarget.Admin => (
                "src/ViajantesTurismo.Admin.ApiService/ViajantesTurismo.Admin.ApiService.csproj",
                options.Refresh ? "RefreshAdminOpenApiArtifacts" : "GenerateAdminOpenApiArtifacts"),
            OpenApiTarget.Catalog => (
                "src/ViajantesTurismo.Catalog.ApiService/ViajantesTurismo.Catalog.ApiService.csproj",
                options.Refresh ? "RefreshCatalogOpenApiArtifacts" : "GenerateCatalogOpenApiArtifacts"),
            OpenApiTarget.Branding => (
                "src/ViajantesTurismo.Branding.ApiService/ViajantesTurismo.Branding.ApiService.csproj",
                options.Refresh ? "RefreshBrandingOpenApiArtifacts" : "GenerateBrandingOpenApiArtifacts"),
            _ => throw new ArgumentOutOfRangeException(nameof(options))
        };

        var startInfo = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            WorkingDirectory = options.RepositoryRoot
        };
        startInfo.ArgumentList.Add("build");
        if (isCi)
        {
            startInfo.ArgumentList.Add("--no-restore");
        }

        startInfo.ArgumentList.Add(project);
        startInfo.ArgumentList.Add($"-p:{property}=true");
        startInfo.Environment[OpenApiBuildGeneration.EnvironmentVariableName] = "true";
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = OpenApiGenerationEnvironment;
        startInfo.Environment["DOTNET_ENVIRONMENT"] = OpenApiGenerationEnvironment;

        return startInfo;
    }
}
