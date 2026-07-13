using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SharedKernel.RepoConfig.Tool;

internal sealed class GitHubRoadmapSyncer
{
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
        using var ownedHttpClient = _httpClient is null && itemsWithIssues.Any(item => item.Labels.Count > 0) ? CreateGitHubClient() : null;
        var httpClient = _httpClient ?? ownedHttpClient;
        foreach (var item in itemsWithIssues)
        {
            if (item.Labels.Count == 0)
            {
                messages.Add($"skipped {repository}#{item.GitHubIssue} from {item.Id} because it has no labels");
                continue;
            }

            await UpdateItem(httpClient ?? throw new InvalidOperationException("GitHub sync could not create an HTTP client."), repository, item, cancellationToken).ConfigureAwait(false);
            messages.Add($"updated labels for {repository}#{item.GitHubIssue} from {item.Id}");
        }

        return new GitHubSyncResult(messages);
    }

    private async Task UpdateItem(HttpClient httpClient, string repository, RoadmapItemSnapshot item, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(GitHubSyncTimeout, _timeProvider);
        using var itemCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        try
        {
            await UpdateIssue(httpClient, repository, item, itemCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new GitHubSyncTimeoutException();
        }
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
            .Select(item => item.Labels.Count == 0
                ? $"dry-run: skip {_project.GitHubRepository}#{item.GitHubIssue} from {item.Id} because it has no labels"
                : $"dry-run: sync labels for {_project.GitHubRepository}#{item.GitHubIssue} from {item.Id}")
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
        var issueUrl = $"https://api.github.com/repos/{repository}/issues/{issueNumber}";
        await VerifyIssueMapping(httpClient, repository, issueNumber, cancellationToken).ConfigureAwait(false);

        var labelPayload = BuildLabelPayload(item.Labels);
        using var labelRequest = new HttpRequestMessage(HttpMethod.Post, $"{issueUrl}/labels")
        {
            Content = new StringContent(labelPayload, Encoding.UTF8, "application/json")
        };
        using var labelResponse = await httpClient.SendAsync(labelRequest, cancellationToken).ConfigureAwait(false);
        if (!labelResponse.IsSuccessStatusCode)
        {
            throw CreateGitHubFailure("label sync", issueNumber, labelResponse.StatusCode);
        }
    }

    private static async Task VerifyIssueMapping(HttpClient httpClient, string repository, int issueNumber, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{repository}/pulls/{issueNumber}");
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        if (response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"GitHub issue mapping points to a pull request: #{issueNumber}.");
        }

        throw CreateGitHubFailure("pull request check", issueNumber, response.StatusCode);
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

}
