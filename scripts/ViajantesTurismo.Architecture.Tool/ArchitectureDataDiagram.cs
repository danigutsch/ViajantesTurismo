using System.Net;
using System.Text.Json;

using PandaBear.Documentation;

namespace ViajantesTurismo.ArchitectureDocumentation;

/// <summary>Adapts the application's architecture facts to the pinned documentation renderer.</summary>
internal static class ArchitectureDataDiagram
{
    private const string DocsPath = "docs/architecture";
    private const string MetadataPath = DocsPath + "/architecture-metadata.json";
    private static readonly JsonSerializerOptions MetadataOptions = new(JsonSerializerOptions.Web)
    {
        RespectRequiredConstructorParameters = true,
    };

    /// <summary>Regenerates the data ownership diagram or reports drift in check mode.</summary>
    /// <exception cref="InvalidOperationException">The metadata could not be adapted to the diagram.</exception>
    internal static DocumentationGenerationResult Run(string root, bool checkOnly)
    {
        var metadata = File.ReadAllText(Path.Combine(root, MetadataPath));
        var catalog = JsonSerializer.Deserialize<ArchitectureCatalog>(metadata, MetadataOptions)
            ?? throw new InvalidOperationException($"Missing architecture metadata in {MetadataPath}.");
        Validate(catalog);

        var config = new
        {
            docsPath = DocsPath,
            generatorName = "ViajantesTurismo.Architecture.Tool",
            blocks = new[]
            {
                new
                {
                    name = "data-ownership",
                    targetPath = "data-ownership.md",
                    kind = "mermaid-flowchart",
                    flowchart = "flowchart LR",
                    lines = DiagramLines(catalog),
                },
            },
        };
        var configPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(configPath, JsonSerializer.Serialize(config, JsonSerializerOptions.Web));
            return DocumentationGenerator.Run(root, configPath, checkOnly);
        }
        finally
        {
            File.Delete(configPath);
        }
    }

    private static void Validate(ArchitectureCatalog catalog)
    {
        if (catalog.SchemaVersion != 1)
        {
            throw new InvalidOperationException($"Unrecognized architecture schema version {catalog.SchemaVersion}.");
        }

        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var service in catalog.Services)
        {
            ValidateNode(catalog, nodeIds, service.Id, service.Boundary, service.State);
            if (service.Audiences.Any(audience => !catalog.Boundaries.ContainsKey(audience)))
            {
                throw new InvalidOperationException($"An unknown audience is assigned to '{service.Id}'.");
            }
        }

        var serviceIds = nodeIds.ToHashSet(StringComparer.Ordinal);
        foreach (var store in catalog.Stores)
        {
            ValidateNode(catalog, nodeIds, store.Id, store.Boundary, store.State);
            if (store.Consumers.Any(consumer => !serviceIds.Contains(consumer)))
            {
                throw new InvalidOperationException($"An unknown application consumes store '{store.Id}'.");
            }

            foreach (var table in store.Tables)
            {
                if (!catalog.TableRoles.ContainsKey(table.Role)
                    || table.DataClasses.Any(dataClass => !catalog.DataClasses.ContainsKey(dataClass)))
                {
                    throw new InvalidOperationException($"Unknown role or data class for '{store.Id}/{table.Schema}.{table.Name}'.");
                }
            }
        }
    }

    private static void ValidateNode(ArchitectureCatalog catalog, HashSet<string> nodeIds, string id, string boundary, string state)
    {
        if (!IsIdentifier(id) || !nodeIds.Add(id)
            || !IsIdentifier(boundary) || !catalog.Boundaries.ContainsKey(boundary)
            || !catalog.Lifecycle.ContainsKey(state))
        {
            throw new InvalidOperationException($"Invalid identifier, trust boundary or state for architecture node '{id}'.");
        }
    }

    private static bool IsIdentifier(string value) =>
        value.Length > 0 && char.IsAsciiLetter(value[0])
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character == '_');

    private static string[] DiagramLines(ArchitectureCatalog catalog)
    {
        var lines = new List<string>();
        foreach (var (boundary, label) in catalog.Boundaries.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            var services = catalog.Services.Where(service => service.Boundary == boundary)
                .OrderBy(service => service.Id, StringComparer.Ordinal).ToArray();
            var stores = catalog.Stores.Where(store => store.Boundary == boundary)
                .OrderBy(store => store.Id, StringComparer.Ordinal).ToArray();
            if (services.Length == 0 && stores.Length == 0)
            {
                continue;
            }

            lines.Add($"    subgraph boundary_{boundary}[\"{WebUtility.HtmlEncode(label)}\"]");
            lines.AddRange(services.Select(service => Node(service.Id,
                [service.Label, $"Audience: {string.Join(", ", service.Audiences.Order(StringComparer.Ordinal))}", catalog.Lifecycle[service.State]])));
            lines.AddRange(stores.Select(store => Node(store.Id, StoreLabels(catalog, store))));
            lines.Add("    end");
        }

        foreach (var store in catalog.Stores.OrderBy(store => store.Id, StringComparer.Ordinal))
        {
            lines.AddRange(store.Consumers.Order(StringComparer.Ordinal)
                .Select(consumer => $"    {consumer} -->|\"{WebUtility.HtmlEncode(store.Access)}\"| {store.Id}"));
        }

        return [.. lines];
    }

    private static IEnumerable<string> StoreLabels(ArchitectureCatalog catalog, ArchitectureStore store)
    {
        yield return store.DbContext ?? store.Id;
        yield return $"{store.Owner}, {store.Database}, {catalog.Lifecycle[store.State]}";
        foreach (var table in store.Tables.OrderBy(table => table.Schema, StringComparer.Ordinal)
            .ThenBy(table => table.Name, StringComparer.Ordinal))
        {
            var dataClasses = table.DataClasses.Order(StringComparer.Ordinal).Select(dataClass => catalog.DataClasses[dataClass]);
            yield return $"{table.Schema}.{table.Name}, {catalog.TableRoles[table.Role]}, {string.Join(", ", dataClasses)}";
        }
    }

    private static string Node(string id, IEnumerable<string> labels) =>
        $"        {id}[\"{string.Join("<br/>", labels.Select(WebUtility.HtmlEncode))}\"]";
}
