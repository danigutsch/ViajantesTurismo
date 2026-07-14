using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

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
        BuildPreview(_project.Items.Where(item => item.GitHubIssue is not null || item.CreateGitHubIssue).OrderByPriority().ToArray());

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

        var itemsWithIssues = _project.Items.Where(item => item.GitHubIssue is not null || item.CreateGitHubIssue).OrderByPriority().ToArray();
        if (itemsWithIssues.Length == 0)
        {
            return new GitHubSyncResult(["No roadmap items have GitHub issue mappings."]);
        }

        List<string> messages = [];
        using var ownedHttpClient = _httpClient is null && itemsWithIssues.Length > 0 ? CreateGitHubClient() : null;
        var httpClient = _httpClient ?? ownedHttpClient;
        var projectTarget = _project.GitHubProjectTarget;
        var projectClient = projectTarget is null
            ? null
            : new GitHubProjectClient(httpClient ?? throw new InvalidOperationException("GitHub sync could not create an HTTP client."));
        if (projectClient is not null)
        {
            await RunProjectOperation(async token =>
            {
                await projectClient.VerifyTarget(projectTarget!, token).ConfigureAwait(false);
                return true;
            }, cancellationToken).ConfigureAwait(false);
        }
        foreach (var configuredItem in itemsWithIssues)
        {
            var item = configuredItem;
            if (item.CreateGitHubIssue)
            {
                using var creationLock = AcquireIssueCreationLock(item);
                var mapping = PrepareIssueMapping(item);
                if (TryGetMappedIssueNumber(mapping.GitHub, out var existingIssueNumber))
                {
                    item = item with { GitHubIssue = existingIssueNumber, CreateGitHubIssue = false };
                }
                else
                {
                    EnsureCreateIntent(mapping.GitHub, item.Path);
                    var issueNumber = await RunProjectOperation(token => CreateIssue(httpClient ?? throw new InvalidOperationException("GitHub sync could not create an HTTP client."), repository, item, token), cancellationToken).ConfigureAwait(false);
                    try
                    {
                        PersistIssueMapping(mapping.Path, mapping.Root, issueNumber);
                    }
                    catch (IOException exception)
                    {
                        throw new InvalidOperationException($"GitHub issue #{issueNumber} was created but its roadmap mapping could not be persisted. Set integrations.github.issue to {issueNumber} in {item.Path} before retrying.", exception);
                    }
                    catch (UnauthorizedAccessException exception)
                    {
                        throw new InvalidOperationException($"GitHub issue #{issueNumber} was created but its roadmap mapping could not be persisted. Set integrations.github.issue to {issueNumber} in {item.Path} before retrying.", exception);
                    }

                    item = item with { GitHubIssue = issueNumber, CreateGitHubIssue = false };
                    messages.Add($"created {repository}#{issueNumber} from {item.Id}");
                }
            }

            var extraLabels = await UpdateItem(httpClient ?? throw new InvalidOperationException("GitHub sync could not create an HTTP client."), repository, item, cancellationToken).ConfigureAwait(false);
            if (item.Labels.Count > 0)
            {
                messages.Add($"updated labels for {repository}#{item.GitHubIssue} from {item.Id}");
            }

            foreach (var label in extraLabels)
            {
                messages.Add($"drift: {repository}#{item.GitHubIssue} has extra GitHub label {label}");
            }

            if (projectClient is not null)
            {
                var conflicts = await RunProjectOperation(async token =>
                {
                    var issueId = await projectClient.GetIssueNodeId(repository, item.GitHubIssue!.Value, token).ConfigureAwait(false);
                    var projectItemId = await projectClient.FindItemId(projectTarget!, issueId, token).ConfigureAwait(false)
                        ?? await projectClient.AddIssue(projectTarget!, issueId, token).ConfigureAwait(false);
                    return await ProjectFields(projectClient, projectTarget!, projectItemId, item, token).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);
                messages.Add($"projected {repository}#{item.GitHubIssue} to GitHub Project {projectTarget!.Number}");
                foreach (var conflict in conflicts)
                {
                    messages.Add($"drift: {repository}#{item.GitHubIssue} Project field {conflict} cannot be projected from roadmap source");
                }
            }

            if (item.Labels.Count == 0 && projectClient is null)
            {
                messages.Add($"skipped {repository}#{item.GitHubIssue} from {item.Id} because it has no labels");
            }
        }

        return new GitHubSyncResult(messages);
    }

    private async Task<IReadOnlyList<string>> UpdateItem(HttpClient httpClient, string repository, RoadmapItemSnapshot item, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(GitHubSyncTimeout, _timeProvider);
        using var itemCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        try
        {
            return await UpdateIssue(httpClient, repository, item, itemCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new GitHubSyncTimeoutException();
        }
    }

    private async Task<T> RunProjectOperation<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(GitHubSyncTimeout, _timeProvider);
        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        try
        {
            return await operation(operationCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new GitHubSyncTimeoutException();
        }
    }

    private static async Task<IReadOnlyList<string>> ProjectFields(GitHubProjectClient projectClient, GitHubProjectTarget target, string itemId, RoadmapItemSnapshot item, CancellationToken cancellationToken)
    {
        var fields = await projectClient.GetFields(target, cancellationToken).ConfigureAwait(false);
        var values = await projectClient.GetFieldValues(itemId, cancellationToken).ConfigureAwait(false);
        List<string> conflicts = [];
        string[] requiredFields = ["Roadmap order", "Roadmap status", "Roadmap parent", "Roadmap blocked by", "Roadmap tags", "RICE reach", "RICE impact", "RICE confidence", "RICE effort", "RICE score"];
        conflicts.AddRange(requiredFields.Where(requiredField => !fields.Any(field => string.Equals(field.Name, requiredField, StringComparison.Ordinal))));

        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field.Id))
            {
                continue;
            }

            switch (field.Name)
            {
                case "Roadmap order": await ProjectNumber(projectClient, target, itemId, field, values, item.Order, conflicts, cancellationToken).ConfigureAwait(false); break;
                case "RICE reach": await ProjectNumber(projectClient, target, itemId, field, values, item.Reach, conflicts, cancellationToken).ConfigureAwait(false); break;
                case "RICE impact": await ProjectNumber(projectClient, target, itemId, field, values, item.Impact, conflicts, cancellationToken).ConfigureAwait(false); break;
                case "RICE confidence": await ProjectNumber(projectClient, target, itemId, field, values, item.Confidence, conflicts, cancellationToken).ConfigureAwait(false); break;
                case "RICE effort": await ProjectNumber(projectClient, target, itemId, field, values, item.Effort, conflicts, cancellationToken).ConfigureAwait(false); break;
                case "RICE score": await ProjectNumber(projectClient, target, itemId, field, values, item.Score, conflicts, cancellationToken).ConfigureAwait(false); break;
                case "Roadmap status": await ProjectStatus(projectClient, target, itemId, field, values, item.Status, conflicts, cancellationToken).ConfigureAwait(false); break;
                case "Roadmap parent": await ProjectText(projectClient, target, itemId, field, values, item.Parent ?? string.Empty, conflicts, cancellationToken).ConfigureAwait(false); break;
                case "Roadmap blocked by": await ProjectText(projectClient, target, itemId, field, values, string.Join(", ", item.BlockedBy), conflicts, cancellationToken).ConfigureAwait(false); break;
                case "Roadmap tags": await ProjectText(projectClient, target, itemId, field, values, string.Join(", ", item.Tags), conflicts, cancellationToken).ConfigureAwait(false); break;
            }
        }

        return conflicts;
    }

    private static async Task ProjectNumber(GitHubProjectClient client, GitHubProjectTarget target, string itemId, GitHubProjectResponse.ProjectField field, IReadOnlyList<GitHubProjectItemResponse.FieldValue> values, decimal value, List<string> conflicts, CancellationToken cancellationToken)
    {
        var existing = values.FirstOrDefault(candidate => string.Equals(candidate.Field?.Id, field.Id, StringComparison.Ordinal));
        if (existing?.Number is not null && existing.Number != value)
        {
            conflicts.Add(field.Name ?? field.Id!);
            return;
        }

        if (existing?.Number is null)
        {
            await client.UpdateNumber(target, itemId, field.Id!, value, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task ProjectStatus(GitHubProjectClient client, GitHubProjectTarget target, string itemId, GitHubProjectResponse.ProjectField field, IReadOnlyList<GitHubProjectItemResponse.FieldValue> values, string status, List<string> conflicts, CancellationToken cancellationToken)
    {
        var option = field.Options?.FirstOrDefault(candidate => string.Equals(candidate.Name, status, StringComparison.Ordinal));
        if (string.IsNullOrWhiteSpace(option?.Id))
        {
            conflicts.Add(field.Name ?? field.Id!);
            return;
        }

        var existing = values.FirstOrDefault(candidate => string.Equals(candidate.Field?.Id, field.Id, StringComparison.Ordinal));
        if (!string.IsNullOrWhiteSpace(existing?.OptionId) && !string.Equals(existing.OptionId, option.Id, StringComparison.Ordinal))
        {
            conflicts.Add(field.Name ?? field.Id!);
            return;
        }

        if (string.IsNullOrWhiteSpace(existing?.OptionId))
        {
            await client.UpdateSingleSelect(target, itemId, field.Id!, option.Id, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task ProjectText(GitHubProjectClient client, GitHubProjectTarget target, string itemId, GitHubProjectResponse.ProjectField field, IReadOnlyList<GitHubProjectItemResponse.FieldValue> values, string value, List<string> conflicts, CancellationToken cancellationToken)
    {
        var existing = values.FirstOrDefault(candidate => string.Equals(candidate.Field?.Id, field.Id, StringComparison.Ordinal));
        if (!string.IsNullOrWhiteSpace(existing?.Text) && !string.Equals(existing.Text, value, StringComparison.Ordinal))
        {
            conflicts.Add(field.Name ?? field.Id!);
            return;
        }

        if (string.IsNullOrWhiteSpace(existing?.Text))
        {
            await client.UpdateText(target, itemId, field.Id!, value, cancellationToken).ConfigureAwait(false);
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
            .SelectMany(item => BuildPreviewMessages(item))
            .ToArray());
    }

    private IEnumerable<string> BuildPreviewMessages(RoadmapItemSnapshot item)
    {
        if (item.CreateGitHubIssue)
        {
            yield return $"dry-run: create GitHub issue for {_project.GitHubRepository} from {item.Id}";
            if (_project.GitHubProjectTarget is not null)
            {
                yield return $"dry-run: ensure the created issue from {item.Id} is in GitHub Project {_project.GitHubProjectTarget.Number}";
            }

            yield break;
        }

        if (item.Labels.Count > 0)
        {
            yield return $"dry-run: sync labels for {_project.GitHubRepository}#{item.GitHubIssue} from {item.Id}";
        }
        else if (_project.GitHubProjectTarget is null)
        {
            yield return $"dry-run: skip {_project.GitHubRepository}#{item.GitHubIssue} from {item.Id} because it has no labels";
        }

        if (_project.GitHubProjectTarget is not null)
        {
            yield return $"dry-run: ensure {_project.GitHubRepository}#{item.GitHubIssue} is in GitHub Project {_project.GitHubProjectTarget.Number}";
        }
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

    private static async Task<IReadOnlyList<string>> UpdateIssue(HttpClient httpClient, string repository, RoadmapItemSnapshot item, CancellationToken cancellationToken)
    {
        var issueNumber = item.GitHubIssue ?? throw new InvalidOperationException("GitHub issue mapping is required.");
        var issueUrl = $"https://api.github.com/repos/{repository}/issues/{issueNumber}";
        await VerifyIssueMapping(httpClient, repository, issueNumber, cancellationToken).ConfigureAwait(false);
        var extraLabels = await GetExtraLabels(httpClient, issueUrl, item.Labels, issueNumber, cancellationToken).ConfigureAwait(false);

        if (item.Labels.Count > 0)
        {
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

        return extraLabels;
    }

    private static async Task<IReadOnlyList<string>> GetExtraLabels(HttpClient httpClient, string issueUrl, IReadOnlyList<string> labels, int issueNumber, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, issueUrl);
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw CreateGitHubFailure("label drift check", issueNumber, response.StatusCode);
        }

        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!document.RootElement.TryGetProperty("labels", out var remoteLabels) || remoteLabels.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return remoteLabels.EnumerateArray()
            .Where(label => label.ValueKind == JsonValueKind.Object && label.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
            .Select(label => label.GetProperty("name").GetString())
            .Where(name => !string.IsNullOrWhiteSpace(name) && !labels.Contains(name, StringComparer.OrdinalIgnoreCase))
            .Select(name => name ?? string.Empty)
            .ToArray();
    }

    private static async Task<int> CreateIssue(HttpClient httpClient, string repository, RoadmapItemSnapshot item, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.github.com/repos/{repository}/issues")
        {
            Content = new StringContent(BuildIssuePayload(item), Encoding.UTF8, "application/json")
        };
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw CreateGitHubCreationFailure(response.StatusCode);
        }

        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!document.RootElement.TryGetProperty("number", out var number) || !number.TryGetInt32(out var issueNumber) || issueNumber < 1)
        {
            throw new InvalidOperationException("GitHub issue creation response did not contain a positive issue number.");
        }

        return issueNumber;
    }

    private (string Path, JsonObject Root, JsonObject GitHub) PrepareIssueMapping(RoadmapItemSnapshot item)
    {
        var itemPath = Path.Combine(_project.RootPath, item.Path);
        var root = JsonNode.Parse(File.ReadAllText(itemPath)) as JsonObject
            ?? throw new InvalidOperationException($"Roadmap item root must be a JSON object: {item.Path}.");
        var integrations = GetOrCreateObject(root, "integrations");
        var github = GetOrCreateObject(integrations, "github");
        return (itemPath, root, github);
    }

    private FileStream AcquireIssueCreationLock(RoadmapItemSnapshot item)
    {
        var lockPath = Path.Combine(_project.RootPath, item.Path) + ".lock";
        try
        {
            return new FileStream(lockPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
        }
        catch (IOException exception)
        {
            throw new InvalidOperationException($"GitHub issue creation is already in progress for {item.Id}. If no sync is running, remove stale lock file {lockPath} and retry.", exception);
        }
    }

    private static bool TryGetMappedIssueNumber(JsonObject github, out int issueNumber)
    {
        issueNumber = 0;
        return github["issue"] is JsonValue value && value.TryGetValue(out issueNumber) && issueNumber > 0;
    }

    private static void EnsureCreateIntent(JsonObject github, string itemPath)
    {
        if (github["issue"] is JsonValue value && value.TryGetValue<string>(out var intent) && string.Equals(intent, "create", StringComparison.Ordinal))
        {
            return;
        }

        throw new InvalidOperationException($"GitHub issue mapping changed while synchronizing: {itemPath}.");
    }

    private static void PersistIssueMapping(string itemPath, JsonObject root, int issueNumber)
    {
        var integrations = GetOrCreateObject(root, "integrations");
        var github = GetOrCreateObject(integrations, "github");
        github["issue"] = issueNumber;

        var temporaryPath = itemPath + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
            File.Move(temporaryPath, itemPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static JsonObject GetOrCreateObject(JsonObject parent, string propertyName)
    {
        if (parent[propertyName] is JsonObject existing)
        {
            return existing;
        }

        var created = new JsonObject();
        parent[propertyName] = created;
        return created;
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
        return new InvalidOperationException($"GitHub {operation} failed for #{issueNumber}: HTTP {(int)statusCode}{GetGitHubFailureHint(statusCode)}.");
    }

    private static InvalidOperationException CreateGitHubCreationFailure(HttpStatusCode statusCode) =>
        new($"GitHub issue creation failed: HTTP {(int)statusCode}{GetGitHubFailureHint(statusCode)}.");

    private static string GetGitHubFailureHint(HttpStatusCode statusCode) =>
        statusCode switch
        {
            HttpStatusCode.Unauthorized => " (authentication required)",
            HttpStatusCode.Forbidden => " (access denied or rate limited)",
            HttpStatusCode.NotFound => " (resource not found or inaccessible)",
            HttpStatusCode.UnprocessableEntity => " (request validation failed)",
            HttpStatusCode.TooManyRequests => " (rate limited)",
            _ => string.Empty
        };

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

    private static string BuildIssuePayload(RoadmapItemSnapshot item)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("title", item.Title);
            writer.WriteStartArray("labels");
            foreach (var label in item.Labels)
            {
                writer.WriteStringValue(label);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

}
