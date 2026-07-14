using SharedKernel.Testing.Snapshots;
using ViajantesTurismo.Branding.ContractTests.Infrastructure;

namespace ViajantesTurismo.Branding.ContractTests;

/// <summary>
/// Verifies that canonical Branding OpenAPI artifacts stay aligned with generated boundary artifacts.
/// </summary>
internal static class BrandingOpenApiArtifactDriftGuard
{
    private const string CanonicalArtifactSuffix = ".openapi.json";
    private const string GeneratedArtifactPrefix = "ViajantesTurismo.Branding.ApiService_";
    private const string RefreshHint = "Refresh with `dotnet run --project tools/ViajantesTurismo.OpenApi.Tool -- generate branding --refresh` when the HTTP contract changes intentionally.";

    public static JsonSnapshotArtifactSet CreateSnapshotSet()
    {
        var openApiDirectory = Path.Combine(
            BrandingContractTestRepository.RootPath,
            "src",
            "ViajantesTurismo.Branding.Contracts.Http",
            "OpenApi");
        return new JsonSnapshotArtifactSet(
            openApiDirectory,
            Path.Combine(openApiDirectory, ".generated"),
            CanonicalArtifactSuffix,
            GeneratedArtifactPrefix,
            "Branding OpenAPI",
            RefreshHint,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["v1"] = "ViajantesTurismo.Branding.ApiService.json"
            });
    }
}
