using SharedKernel.Testing.Snapshots;

using ViajantesTurismo.Admin.ContractTests.Infrastructure;

namespace ViajantesTurismo.Admin.ContractTests;

/// <summary>
/// Verifies that canonical Admin OpenAPI artifacts stay aligned with generated boundary artifacts.
/// </summary>
internal static class AdminOpenApiArtifactDriftGuard
{
    private const string CanonicalArtifactSuffix = ".openapi.json";
    private const string GeneratedArtifactPrefix = "ViajantesTurismo.Admin.ApiService_";
    private const string RefreshHint = "Refresh with `dotnet run --project tools/ViajantesTurismo.OpenApi.Tool --no-restore -- generate admin --refresh` when the HTTP contract changes intentionally.";

    /// <summary>
    /// Asserts that every canonical boundary artifact has a matching generated artifact and vice versa.
    /// </summary>
    public static void AssertCanonicalArtifactsMatchGeneratedArtifacts()
    {
        CreateSnapshotSet().AssertCanonicalArtifactsMatchGeneratedArtifacts();
    }

    private static string GetOpenApiDirectory()
        => Path.Combine(ContractTestRepository.RootPath, "src", "ViajantesTurismo.Admin.Contracts.Http", "OpenApi");

    public static JsonSnapshotArtifactSet CreateSnapshotSet()
    {
        var openApiDirectory = GetOpenApiDirectory();
        return new JsonSnapshotArtifactSet(
            openApiDirectory,
            Path.Combine(openApiDirectory, ".generated"),
            CanonicalArtifactSuffix,
            GeneratedArtifactPrefix,
            "Admin OpenAPI",
            RefreshHint,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["v1"] = "ViajantesTurismo.Admin.ApiService.json"
            });
    }
}
