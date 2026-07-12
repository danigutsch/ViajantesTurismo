using System.Text.Json;

namespace SharedKernel.RepoConfig.Tool;

internal sealed class RoadmapProject
{
    private readonly Dictionary<string, RoadmapItemSnapshot> _itemsById;

    private RoadmapProject(IReadOnlyList<RoadmapItemSnapshot> items, IReadOnlySet<string> closedStatuses, string? gitHubRepository, bool gitHubEnabled)
    {
        Items = items;
        ClosedStatuses = closedStatuses;
        GitHubRepository = gitHubRepository;
        GitHubEnabled = gitHubEnabled;
        _itemsById = Items.ToDictionary(item => item.Id, StringComparer.Ordinal);
    }

    public IReadOnlyList<RoadmapItemSnapshot> Items { get; }

    public IReadOnlySet<string> ClosedStatuses { get; }

    public string? GitHubRepository { get; }

    public bool GitHubEnabled { get; }

    public static RoadmapProject Load(string rootPath)
    {
        var issues = RepoConfigVerifier.Verify(rootPath);
        if (issues.Count > 0)
        {
            throw new InvalidOperationException("Roadmap config is invalid. Run verify before project queries.");
        }

        var configPath = Path.Combine(rootPath, RepoConfigPaths.Config);
        using var config = JsonDocument.Parse(File.ReadAllText(configPath));
        var closedStatuses = ReadStringArray(config.RootElement, "project", "closedStatuses");
        var gitHubRepository = ReadGitHubRepository(config.RootElement);
        var gitHubEnabled = ReadGitHubEnabled(config.RootElement);
        var items = LoadItems(rootPath);
        return new RoadmapProject(items, new HashSet<string>(closedStatuses, StringComparer.Ordinal), gitHubRepository, gitHubEnabled);
    }

    public IReadOnlyList<RoadmapItemSnapshot> OpenItems(string? type = null) =>
        Items.Where(item => !IsClosed(item) && (type is null || string.Equals(item.Type, type, StringComparison.Ordinal))).ToArray();

    public bool IsClosed(RoadmapItemSnapshot item) =>
        ClosedStatuses.Contains(item.Status);

    public bool IsUnblocked(RoadmapItemSnapshot item) =>
        BlockersOf(item.Id).All(IsClosed);

    public IReadOnlyList<RoadmapItemSnapshot> BlockersOf(string itemId)
    {
        if (!_itemsById.TryGetValue(itemId, out var item))
        {
            return [];
        }

        return item.BlockedBy.Where(_itemsById.ContainsKey).Select(blockerId => _itemsById[blockerId]).ToArray();
    }

    public IEnumerable<KeyValuePair<string, int>> TagCounts() =>
        Items.SelectMany(item => item.Tags).GroupBy(tag => tag, StringComparer.Ordinal).Select(group => new KeyValuePair<string, int>(group.Key, group.Count())).OrderByDescending(item => item.Value).ThenBy(item => item.Key, StringComparer.Ordinal);

    public IEnumerable<KeyValuePair<string, int>> LabelCounts() =>
        Items.SelectMany(item => item.Labels).GroupBy(label => label, StringComparer.Ordinal).Select(group => new KeyValuePair<string, int>(group.Key, group.Count())).OrderByDescending(item => item.Value).ThenBy(item => item.Key, StringComparer.Ordinal);

    private static RoadmapItemSnapshot[] LoadItems(string rootPath) =>
        Directory.EnumerateFiles(Path.Combine(rootPath, RepoConfigPaths.Items), "*.json", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.Ordinal)
            .Select(path => LoadItem(rootPath, path))
            .ToArray();

    private static RoadmapItemSnapshot LoadItem(string rootPath, string itemPath)
    {
        var relativePath = RepoConfigPaths.RelativeTo(rootPath, itemPath);
        using var document = JsonDocument.Parse(File.ReadAllText(itemPath));
        var root = document.RootElement;
        var scoring = root.GetProperty("scoring");
        return new RoadmapItemSnapshot(
            root.GetProperty("id").GetString() ?? string.Empty,
            relativePath,
            root.GetProperty("title").GetString() ?? string.Empty,
            root.GetProperty("type").GetString() ?? string.Empty,
            root.GetProperty("status").GetString() ?? string.Empty,
            root.GetProperty("theme").GetString() ?? string.Empty,
            root.GetProperty("order").GetInt32(),
            root.TryGetProperty("parent", out var parent) && parent.ValueKind == JsonValueKind.String ? parent.GetString() : null,
            scoring.GetProperty("reach").GetDecimal(),
            scoring.GetProperty("impact").GetDecimal(),
            scoring.GetProperty("confidence").GetDecimal(),
            scoring.GetProperty("effort").GetDecimal(),
            ReadStringArray(root, "blockedBy"),
            ReadStringArray(root, "blocks"),
            ReadStringArray(root, "dependencies"),
            ReadStringArray(root, "tags"),
            ReadStringArray(root, "labels"),
            ReadGitHubIssue(root));
    }

    private static string? ReadGitHubRepository(JsonElement root)
    {
        if (!root.TryGetProperty("integrations", out var integrations)
            || !integrations.TryGetProperty("github", out var github)
            || !github.TryGetProperty("repository", out var repository)
            || repository.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return repository.GetString();
    }

    private static bool ReadGitHubEnabled(JsonElement root)
    {
        if (!root.TryGetProperty("integrations", out var integrations)
            || !integrations.TryGetProperty("github", out var github)
            || !github.TryGetProperty("enabled", out var enabled)
            || enabled.ValueKind != JsonValueKind.True)
        {
            return false;
        }

        return true;
    }

    private static int? ReadGitHubIssue(JsonElement root)
    {
        if (!root.TryGetProperty("integrations", out var integrations)
            || !integrations.TryGetProperty("github", out var github)
            || !github.TryGetProperty("issue", out var issue)
            || issue.ValueKind != JsonValueKind.Number
            || !issue.TryGetInt32(out var value))
        {
            return null;
        }

        return value;
    }

    private static string[] ReadStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return array.EnumerateArray()
            .Where(element => element.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(element.GetString()))
            .Select(element => element.GetString() ?? string.Empty)
            .ToArray();
    }

    private static string[] ReadStringArray(JsonElement root, string objectProperty, string arrayProperty)
    {
        if (!root.TryGetProperty(objectProperty, out var parent) || parent.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        return ReadStringArray(parent, arrayProperty);
    }
}
