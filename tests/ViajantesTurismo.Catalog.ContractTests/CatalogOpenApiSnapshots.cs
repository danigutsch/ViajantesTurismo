using PandaBear.Testing.Snapshots;

namespace ViajantesTurismo.Catalog.ContractTests;

/// <summary>Locates the Catalog OpenAPI snapshot artifacts and compares canonical files with generated ones.</summary>
internal static class CatalogOpenApiSnapshots
{
    private const string CanonicalArtifactSuffix = ".openapi.json";
    private const string GeneratedArtifactPrefix = "ViajantesTurismo.Catalog.ApiService_";
    private const string RefreshHint = "Generate or refresh with `dotnet tool run sharedkernel-ci -- openapi refresh --target catalog` (needed once in a fresh clone or worktree, and after intentional HTTP contract changes).";

    /// <summary>Asserts the canonical Catalog OpenAPI artifacts match the generated artifacts.</summary>
    public static void AssertCanonicalArtifactsMatchGeneratedArtifacts()
    {
        CreateSnapshotSet().AssertCanonicalArtifactsMatchGeneratedArtifacts();
    }

    /// <summary>Creates the snapshot set pairing canonical artifacts with the generated v1 document.</summary>
    public static JsonSnapshotArtifactSet CreateSnapshotSet()
    {
        var openApiDirectory = GetOpenApiDirectory();
        return new JsonSnapshotArtifactSet(
            openApiDirectory,
            Path.Combine(openApiDirectory, ".generated"),
            CanonicalArtifactSuffix,
            GeneratedArtifactPrefix,
            "Catalog OpenAPI",
            RefreshHint,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["v1"] = "ViajantesTurismo.Catalog.ApiService.json"
            });
    }

    private static string GetOpenApiDirectory()
        => Path.Combine(GetRepositoryRoot(), "src", "ViajantesTurismo.Catalog.Contracts.Http", "OpenApi");

    private static string GetRepositoryRoot()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            var candidatePath = Path.Combine(currentDirectory.FullName, "ViajantesTurismo.slnx");
            if (File.Exists(candidatePath))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root for contract test artifacts.");
    }
}
