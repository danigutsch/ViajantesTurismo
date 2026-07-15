using System.Text.Json;

namespace SharedKernel.RepoConfig.Tests;

internal static class RoadmapConfigSchemaTestFiles
{
    public static string ReadCheckedInConfig() =>
        File.ReadAllText(Path.Combine(GetRepositoryRoot(), "roadmap", "config.json"));

    public static string ReadCheckedInSchema() =>
        File.ReadAllText(Path.Combine(GetRepositoryRoot(), "roadmap", "schema", "roadmap-config.schema.json"));

    public static string ReadCheckedInItemSchema() =>
        File.ReadAllText(Path.Combine(GetRepositoryRoot(), "roadmap", "schema", "roadmap-item.schema.json"));

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
