using SharedKernel.Testing.Snapshots;

namespace ViajantesTurismo.Catalog.ContractTests;

internal static class CatalogOpenApiSnapshots
{
    private const string CanonicalArtifactSuffix = ".openapi.json";
    private const string GeneratedArtifactPrefix = "ViajantesTurismo.Catalog.ApiService_";
    private const string RefreshHint = "Refresh with `dotnet build src/ViajantesTurismo.Catalog.ApiService/ViajantesTurismo.Catalog.ApiService.csproj -p:RefreshCatalogOpenApiArtifacts=true` when the HTTP contract changes intentionally.";

    public static void AssertCanonicalArtifactsMatchGeneratedArtifacts()
    {
        CreateSnapshotSet().AssertCanonicalArtifactsMatchGeneratedArtifacts();
    }

    public static JsonSnapshotArtifactSet CreateSnapshotSet()
    {
        var openApiDirectory = GetOpenApiDirectory();
        return new JsonSnapshotArtifactSet(
            openApiDirectory,
            Path.Combine(openApiDirectory, ".generated"),
            CanonicalArtifactSuffix,
            GeneratedArtifactPrefix,
            "Catalog OpenAPI",
            RefreshHint);
    }

    private static string GetOpenApiDirectory()
        => Path.Combine(GetRepositoryRoot(), "src", "ViajantesTurismo.Catalog.Contracts", "OpenApi");

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
