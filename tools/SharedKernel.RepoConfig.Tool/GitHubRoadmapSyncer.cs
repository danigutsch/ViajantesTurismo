using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SharedKernel.RepoConfig.Tool;

internal sealed class GitHubRoadmapSyncer
{
    private const string GitHubLineBreak = "\n";

    private readonly HttpClient? _httpClient;
    private readonly RoadmapProject _project;

    public GitHubRoadmapSyncer(RoadmapProject project)
        : this(project, httpClient: null)
    {
    }

    internal GitHubRoadmapSyncer(RoadmapProject project, HttpClient? httpClient)
    {
        ArgumentNullException.ThrowIfNull(project);

        _project = project;
        _httpClient = httpClient;
    }

    public GitHubSyncResult Sync(bool dryRun)
    {
        List<string> messages = [];
        if (!_project.GitHubEnabled)
        {
            throw new InvalidOperationException("GitHub sync is disabled in roadmap/config.json.");
        }

        if (string.IsNullOrWhiteSpace(_project.GitHubRepository))
        {
            throw new InvalidOperationException("roadmap/config.json must define integrations.github.repository before GitHub sync.");
        }

        var itemsWithIssues = _project.Items.Where(item => item.GitHubIssue is not null).OrderByPriority().ToArray();
        if (itemsWithIssues.Length == 0)
        {
            messages.Add("No roadmap items have GitHub issue mappings.");
            return new GitHubSyncResult(messages);
        }

        if (dryRun)
        {
            foreach (var item in itemsWithIssues)
            {
                messages.Add($"dry-run: update {_project.GitHubRepository}#{item.GitHubIssue} from {item.Id}");
            }

            return new GitHubSyncResult(messages);
        }

        using var ownedHttpClient = _httpClient is null ? CreateGitHubClient() : null;
        var httpClient = (_httpClient ?? ownedHttpClient) ?? throw new InvalidOperationException("GitHub sync could not create an HTTP client.");
        foreach (var item in itemsWithIssues)
        {
            UpdateIssue(httpClient, _project.GitHubRepository, item);
            messages.Add($"updated {_project.GitHubRepository}#{item.GitHubIssue} from {item.Id}");
        }

        return new GitHubSyncResult(messages);
    }

    private static HttpClient CreateGitHubClient()
    {
        var token = Environment.GetEnvironmentVariable("GH_TOKEN") ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Set GH_TOKEN or GITHUB_TOKEN before running sync github --apply.");
        }

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("sharedkernel-repo", "1.0"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return httpClient;
    }

    private static void UpdateIssue(HttpClient httpClient, string repository, RoadmapItemSnapshot item)
    {
        var issueNumber = item.GitHubIssue ?? throw new InvalidOperationException("GitHub issue mapping is required.");
        var url = $"https://api.github.com/repos/{repository}/issues/{issueNumber}";
        var currentBody = ReadCurrentIssueBody(httpClient, url, issueNumber);
        var updatedBody = UpsertManagedSection(currentBody, BuildManagedSection(item));
        var payload = BuildIssueUpdatePayload(updatedBody);

        using var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        using var response = httpClient.Send(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"GitHub issue update failed for #{issueNumber}: {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        if (item.Labels.Count > 0)
        {
            var labelPayload = BuildLabelPayload(item.Labels);
            using var labelRequest = new HttpRequestMessage(HttpMethod.Post, $"{url}/labels")
            {
                Content = new StringContent(labelPayload, Encoding.UTF8, "application/json")
            };
            using var labelResponse = httpClient.Send(labelRequest);
            if (!labelResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"GitHub label sync failed for #{issueNumber}: {(int)labelResponse.StatusCode} {labelResponse.ReasonPhrase}");
            }
        }
    }

    private static string ReadCurrentIssueBody(HttpClient httpClient, string url, int issueNumber)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = httpClient.Send(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"GitHub issue read failed for #{issueNumber}: {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        var responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        using var document = JsonDocument.Parse(responseText);
        if (document.RootElement.TryGetProperty("pull_request", out _))
        {
            throw new InvalidOperationException($"GitHub issue mapping points to a pull request: #{issueNumber}.");
        }

        if (document.RootElement.TryGetProperty("body", out var body) && body.ValueKind == JsonValueKind.String)
        {
            return body.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string BuildIssueUpdatePayload(string body)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("body", body);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string BuildLabelPayload(IReadOnlyList<string> labels)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteStartArray("labels");
            foreach (var label in labels)
            {
                writer.WriteStringValue(label);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    internal static string UpsertManagedSection(string currentBody, string managedSection)
    {
        const string start = "<!-- roadmap:managed:start -->";
        const string end = "<!-- roadmap:managed:end -->";
        var startIndex = currentBody.IndexOf(start, StringComparison.Ordinal);
        var endIndex = currentBody.IndexOf(end, StringComparison.Ordinal);
        var hasStart = startIndex >= 0;
        var hasEnd = endIndex >= 0;
        var hasOneValidPair = hasStart
            && hasEnd
            && startIndex < endIndex
            && currentBody.IndexOf(start, startIndex + start.Length, StringComparison.Ordinal) < 0
            && currentBody.IndexOf(end, endIndex + end.Length, StringComparison.Ordinal) < 0;

        if (hasStart != hasEnd || (hasStart && !hasOneValidPair))
        {
            throw new InvalidOperationException("GitHub issue body has malformed roadmap managed-section markers.");
        }

        if (startIndex >= 0 && endIndex >= startIndex)
        {
            return currentBody[..startIndex] + managedSection + currentBody[(endIndex + end.Length)..];
        }

        return currentBody.Length == 0
            ? managedSection
            : $"{currentBody}{GitHubLineBreak}{GitHubLineBreak}{managedSection}";
    }

    private static string BuildManagedSection(RoadmapItemSnapshot item) =>
        $$"""
        <!-- roadmap:managed:start -->
        ## Roadmap

        - Roadmap ID: `{{item.Id}}`
        - Type: `{{item.Type}}`
        - Status: `{{item.Status}}`
        - Order: `{{item.Order}}`
        - RICE: `{{item.Score.ToString("0.##", CultureInfo.InvariantCulture)}}`
        - Blocked by: {{FormatList(item.BlockedBy)}}
        - Blocks: {{FormatList(item.Blocks)}}
        - Tags: {{FormatList(item.Tags)}}

        {{item.Title}}
        <!-- roadmap:managed:end -->
        """;

    private static string FormatList(IReadOnlyCollection<string> values) =>
        values.Count == 0 ? "none" : string.Join(", ", values.Select(value => $"`{value}`"));
}
