using PandaBear.Testing;
using PandaBear.Testing.Snapshots;

namespace ViajantesTurismo.Catalog.ContractTests;

/// <summary>Locates the Catalog OpenAPI snapshot artifacts and compares canonical files with generated ones.</summary>
internal static class CatalogOpenApiSnapshots
{
    private const string CanonicalArtifactSuffix = ".openapi.json";
    private const string GeneratedArtifactPrefix = "ViajantesTurismo.Catalog.Api_";
    private const string RefreshHint = "Generate with `dotnet ci openapi generate --target catalog` in a fresh clone or worktree; refresh with `dotnet ci openapi refresh --target catalog` after intentional HTTP contract changes.";

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
                ["v1"] = "ViajantesTurismo.Catalog.Api.json"
            });
    }

    private static string GetOpenApiDirectory()
        => Path.Combine(RepositoryRoot.Locate("ViajantesTurismo.slnx"), "src", "ViajantesTurismo.Catalog.Contracts.Http", "OpenApi");
}
