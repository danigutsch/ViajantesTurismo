using PandaBear.Testing.Snapshots;
using ViajantesTurismo.Branding.ContractTests.Infrastructure;

namespace ViajantesTurismo.Branding.ContractTests;

/// <summary>
/// Verifies that canonical Branding OpenAPI artifacts stay aligned with generated boundary artifacts.
/// </summary>
internal static class BrandingOpenApiArtifactDriftGuard
{
    private const string CanonicalArtifactSuffix = ".openapi.json";
    private const string GeneratedArtifactPrefix = "ViajantesTurismo.Catalog.ApiService_";
    private const string RefreshHint = "Generate or refresh with `dotnet tool run sharedkernel-ci -- openapi refresh --target branding` (needed once in a fresh clone or worktree, and after intentional HTTP contract changes).";

    /// <summary>Creates the snapshot set that compares canonical Branding OpenAPI artifacts with the generated ones.</summary>
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
            new Dictionary<string, string>(StringComparer.Ordinal));
    }
}
