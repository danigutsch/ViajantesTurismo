using System.Text.Json.Nodes;
using SharedKernel.Testing.Snapshots;

namespace ViajantesTurismo.Admin.ContractTests;

/// <summary>
/// Verifies that canonical Admin OpenAPI artifacts stay aligned with generated boundary artifacts.
/// </summary>
internal static class AdminOpenApiArtifactDriftGuard
{
    private const string CanonicalArtifactSuffix = ".openapi.json";
    private const string GeneratedArtifactPrefix = "ViajantesTurismo.Admin.ApiService_";

    /// <summary>
    /// Asserts that every canonical boundary artifact has a matching generated artifact and vice versa.
    /// </summary>
    public static void AssertCanonicalArtifactsMatchGeneratedArtifacts()
    {
        var openApiDirectory = GetOpenApiDirectory();
        var generatedDirectory = Path.Combine(openApiDirectory, ".generated");

        EnsureDirectoryExists(
            openApiDirectory,
            "Canonical Admin OpenAPI artifacts were not found. Ensure the Admin contracts artifacts exist under 'src/ViajantesTurismo.Admin.Contracts/OpenApi/'.");
        EnsureDirectoryExists(
            generatedDirectory,
            "Generated Admin OpenAPI artifacts were not found. Build the solution so the Admin API OpenAPI generation target populates 'src/ViajantesTurismo.Admin.Contracts/OpenApi/.generated/'.");

        var canonicalFiles = Directory.GetFiles(openApiDirectory, $"*{CanonicalArtifactSuffix}", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToArray();
        var generatedFiles = Directory.GetFiles(generatedDirectory, $"{GeneratedArtifactPrefix}*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToArray();
        var failures = new List<string>();
        var expectedGeneratedFiles = new HashSet<string>(StringComparer.Ordinal);

        foreach (var canonicalFile in canonicalFiles)
        {
            var boundaryName = GetBoundaryNameFromCanonicalPath(canonicalFile);
            var generatedFileName = $"{GeneratedArtifactPrefix}{boundaryName}.json";
            var generatedFilePath = Path.Combine(generatedDirectory, generatedFileName);
            expectedGeneratedFiles.Add(generatedFileName);

            if (!File.Exists(generatedFilePath))
            {
                failures.Add($"Missing generated artifact '{generatedFileName}' for canonical artifact '{Path.GetFileName(canonicalFile)}'.");
                continue;
            }

            var canonicalJson = ParseJson(canonicalFile);
            var generatedJson = ParseJson(generatedFilePath);

            if (!JsonNode.DeepEquals(canonicalJson, generatedJson))
            {
                failures.Add(
                    $"Canonical artifact '{Path.GetFileName(canonicalFile)}' drifted from generated artifact '{generatedFileName}'.");
            }
        }

        foreach (var generatedFile in generatedFiles)
        {
            var generatedFileName = Path.GetFileName(generatedFile);
            if (!expectedGeneratedFiles.Contains(generatedFileName))
            {
                failures.Add($"Generated artifact '{generatedFileName}' has no canonical artifact counterpart.");
            }
        }

        if (failures.Count > 0)
        {
            throw new Xunit.Sdk.XunitException(
                "Admin OpenAPI artifact drift detected. Refresh canonical files under 'src/ViajantesTurismo.Admin.Contracts/OpenApi/'."
                + Environment.NewLine
                + string.Join(Environment.NewLine, failures));
        }
    }

    /// <summary>
    /// Asserts that a generated boundary artifact matches its committed canonical snapshot.
    /// </summary>
    /// <param name="boundaryName">The OpenAPI boundary document name.</param>
    public static void AssertGeneratedArtifactMatchesCanonicalSnapshot(string boundaryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(boundaryName);

        var openApiDirectory = GetOpenApiDirectory();
        var generatedDirectory = Path.Combine(openApiDirectory, ".generated");
        var canonicalFileName = $"{boundaryName}{CanonicalArtifactSuffix}";
        var generatedFileName = $"{GeneratedArtifactPrefix}{boundaryName}.json";
        var canonicalFilePath = Path.Combine(openApiDirectory, canonicalFileName);
        var generatedFilePath = Path.Combine(generatedDirectory, generatedFileName);

        EnsureFileExists(
            canonicalFilePath,
            $"Canonical Admin OpenAPI snapshot '{canonicalFileName}' was not found under 'src/ViajantesTurismo.Admin.Contracts/OpenApi/'.");
        EnsureFileExists(
            generatedFilePath,
            $"Generated Admin OpenAPI artifact '{generatedFileName}' was not found. Build the solution so 'src/ViajantesTurismo.Admin.Contracts/OpenApi/.generated/' is populated.");

        var canonicalText = SnapshotText.Normalize(File.ReadAllText(canonicalFilePath));
        var generatedText = SnapshotText.Normalize(File.ReadAllText(generatedFilePath));

        if (!string.Equals(canonicalText, generatedText, StringComparison.Ordinal))
        {
            throw new Xunit.Sdk.XunitException(
                $"Generated Admin OpenAPI artifact '{generatedFileName}' does not match canonical snapshot '{canonicalFileName}'. "
                + "Regenerate canonical OpenAPI artifacts intentionally when the HTTP contract changes.");
        }
    }

    private static JsonNode ParseJson(string documentPath)
    {
        var documentText = File.ReadAllText(documentPath);
        return JsonNode.Parse(documentText)
            ?? throw new InvalidOperationException($"OpenAPI artifact '{documentPath}' could not be parsed.");
    }

    private static string GetBoundaryNameFromCanonicalPath(string canonicalPath)
    {
        var fileName = Path.GetFileName(canonicalPath);
        if (!fileName.EndsWith(CanonicalArtifactSuffix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Canonical artifact '{fileName}' does not use the expected suffix '{CanonicalArtifactSuffix}'.");
        }

        return fileName[..^CanonicalArtifactSuffix.Length];
    }

    private static string GetOpenApiDirectory()
        => Path.Combine(GetRepositoryRoot(), "src", "ViajantesTurismo.Admin.Contracts", "OpenApi");

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

    private static void EnsureDirectoryExists(string directoryPath, string message)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new Xunit.Sdk.XunitException(message);
        }
    }

    private static void EnsureFileExists(string filePath, string message)
    {
        if (!File.Exists(filePath))
        {
            throw new Xunit.Sdk.XunitException(message);
        }
    }
}
