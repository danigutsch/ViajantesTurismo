using System.Text.Json;

namespace ViajantesTurismo.ArchitectureTests.Repository;

/// <summary>Reads the checked-in roadmap config, schemas, items and reconciliation manifest for schema tests.</summary>
internal static class RoadmapConfigSchemaTestFiles
{
    /// <summary>Reads <c>roadmap/config.json</c> as text.</summary>
    public static string ReadCheckedInConfig() =>
        File.ReadAllText(Path.Combine(GetRepositoryRoot(), "roadmap", "config.json"));

    /// <summary>Reads the roadmap config JSON schema as text.</summary>
    public static string ReadCheckedInSchema() =>
        File.ReadAllText(Path.Combine(GetRepositoryRoot(), "roadmap", "schema", "roadmap-config.schema.json"));

    /// <summary>Reads the roadmap item JSON schema as text.</summary>
    public static string ReadCheckedInItemSchema() =>
        File.ReadAllText(Path.Combine(GetRepositoryRoot(), "roadmap", "schema", "roadmap-item.schema.json"));

    /// <summary>Reads every roadmap item JSON file, ordered by file name.</summary>
    public static string[] ReadCheckedInItems() =>
        Directory.EnumerateFiles(Path.Combine(GetRepositoryRoot(), "roadmap", "items"), "*.json", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.Ordinal)
            .Select(File.ReadAllText)
            .ToArray();

    /// <summary>Reads the single open-issues reconciliation manifest, failing when there is not exactly one.</summary>
    public static string ReadCheckedInReconciliation()
    {
        var reconciliationFiles = Directory.EnumerateFiles(Path.Combine(GetRepositoryRoot(), "roadmap", "reconciliation"), "open-issues-*.json", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.Ordinal)
            .ToArray();
        return reconciliationFiles.Length == 1
            ? File.ReadAllText(reconciliationFiles[0])
            : throw new InvalidOperationException("Expected exactly one checked-in open-issue reconciliation manifest.");
    }

    /// <summary>Asserts every property in <paramref name="config"/> (recursively, ignoring <c>$schema</c>) is declared in <paramref name="schema"/>.</summary>
    public static void ShouldDeclareConfigProperties(JsonElement config, JsonElement schema)
    {
        var schemaProperties = schema.GetProperty("properties");
        foreach (var configProperty in config.EnumerateObject())
        {
            if (configProperty.NameEquals("$schema"))
            {
                continue;
            }

            schemaProperties.TryGetProperty(configProperty.Name, out var schemaProperty).ShouldBeTrue();
            if (configProperty.Value.ValueKind == JsonValueKind.Object)
            {
                ShouldDeclareConfigProperties(configProperty.Value, schemaProperty);
            }
        }
    }

    private static string GetRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ViajantesTurismo.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
