using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SharedKernel.RepoConfig.Tool;

internal sealed class GitHubRoadmapSyncer
{
    private const string GitHubLineBreak = "\n";
    private static readonly TimeSpan GitHubSyncTimeout = TimeSpan.FromSeconds(30);

    private readonly HttpClient? _httpClient;
    private readonly RoadmapProject _project;
    private readonly TimeProvider _timeProvider;

    public GitHubRoadmapSyncer(RoadmapProject project)
        : this(project, httpClient: null, timeProvider: null)
    {
    }

    internal GitHubRoadmapSyncer(RoadmapProject project, HttpClient? httpClient, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(project);

        _project = project;
        _httpClient = httpClient;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public GitHubSyncResult Preview() =>
        BuildPreview(_project.Items.Where(item => item.GitHubIssue is not null).OrderByPriority().ToArray());

    public async Task<GitHubSyncResult> Apply(CancellationToken cancellationToken)
    {
        if (!_project.GitHubEnabled)
        {
            throw new InvalidOperationException("GitHub sync is disabled in roadmap/config.json.");
        }

        var repository = _project.GitHubRepository;
        if (string.IsNullOrWhiteSpace(repository))
        {
            throw new InvalidOperationException("roadmap/config.json must define integrations.github.repository before GitHub sync.");
        }

        var itemsWithIssues = _project.Items.Where(item => item.GitHubIssue is not null).OrderByPriority().ToArray();
        if (itemsWithIssues.Length == 0)
        {
            return new GitHubSyncResult(["No roadmap items have GitHub issue mappings."]);
        }

        List<string> messages = [];
        using var ownedHttpClient = _httpClient is null ? CreateGitHubClient() : null;
        var httpClient = (_httpClient ?? ownedHttpClient) ?? throw new InvalidOperationException("GitHub sync could not create an HTTP client.");
        foreach (var item in itemsWithIssues)
        {
            await UpdateItem(httpClient, repository, item, cancellationToken).ConfigureAwait(false);
            messages.Add($"updated {repository}#{item.GitHubIssue} from {item.Id}");
        }

        return new GitHubSyncResult(messages);
    }

    private async Task UpdateItem(HttpClient httpClient, string repository, RoadmapItemSnapshot item, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(GitHubSyncTimeout, _timeProvider);
        using var itemCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        await UpdateIssue(httpClient, repository, item, itemCancellation.Token).ConfigureAwait(false);
    }

    private GitHubSyncResult BuildPreview(RoadmapItemSnapshot[] itemsWithIssues)
    {
        if (!_project.GitHubEnabled)
        {
            throw new InvalidOperationException("GitHub sync is disabled in roadmap/config.json.");
        }

        if (string.IsNullOrWhiteSpace(_project.GitHubRepository))
        {
            throw new InvalidOperationException("roadmap/config.json must define integrations.github.repository before GitHub sync.");
        }

        if (itemsWithIssues.Length == 0)
        {
            return new GitHubSyncResult(["No roadmap items have GitHub issue mappings."]);
        }

        return new GitHubSyncResult(itemsWithIssues
            .Select(item => $"dry-run: update {_project.GitHubRepository}#{item.GitHubIssue} from {item.Id}")
            .ToArray());
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

    private static async Task UpdateIssue(HttpClient httpClient, string repository, RoadmapItemSnapshot item, CancellationToken cancellationToken)
    {
        var issueNumber = item.GitHubIssue ?? throw new InvalidOperationException("GitHub issue mapping is required.");
        var url = $"https://api.github.com/repos/{repository}/issues/{issueNumber}";
        var managedSection = BuildManagedSection(item);
        var currentBody = await ReadCurrentIssueBody(httpClient, url, issueNumber, cancellationToken).ConfigureAwait(false);
        // GitHub does not document conditional issue-body updates; re-read before PATCH to avoid
        // overwriting body edits made while this process prepared the managed section.
        var confirmedBody = await ReadCurrentIssueBody(httpClient, url, issueNumber, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(currentBody, confirmedBody, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"GitHub issue body changed before update for #{issueNumber}; retry sync github --apply.");
        }

        var updatedBody = UpsertManagedSection(confirmedBody, managedSection);
        var payload = BuildIssueUpdatePayload(updatedBody);

        using var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw CreateGitHubFailure("issue update", issueNumber, response.StatusCode);
        }

        if (item.Labels.Count > 0)
        {
            var labelPayload = BuildLabelPayload(item.Labels);
            using var labelRequest = new HttpRequestMessage(HttpMethod.Post, $"{url}/labels")
            {
                Content = new StringContent(labelPayload, Encoding.UTF8, "application/json")
            };
            using var labelResponse = await httpClient.SendAsync(labelRequest, cancellationToken).ConfigureAwait(false);
            if (!labelResponse.IsSuccessStatusCode)
            {
                throw CreateGitHubFailure("label sync", issueNumber, labelResponse.StatusCode);
            }
        }
    }

    private static async Task<string> ReadCurrentIssueBody(HttpClient httpClient, string url, int issueNumber, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw CreateGitHubFailure("issue read", issueNumber, response.StatusCode);
        }

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
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

    private static InvalidOperationException CreateGitHubFailure(string operation, int issueNumber, HttpStatusCode statusCode)
    {
        var hint = statusCode switch
        {
            HttpStatusCode.Unauthorized => " (authentication required)",
            HttpStatusCode.Forbidden => " (access denied or rate limited)",
            HttpStatusCode.NotFound => " (resource not found or inaccessible)",
            HttpStatusCode.UnprocessableEntity => " (request validation failed)",
            HttpStatusCode.TooManyRequests => " (rate limited)",
            _ => string.Empty
        };

        return new InvalidOperationException($"GitHub {operation} failed for #{issueNumber}: HTTP {(int)statusCode}{hint}.");
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
