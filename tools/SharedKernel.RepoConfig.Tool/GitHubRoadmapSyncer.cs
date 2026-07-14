using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SharedKernel.RepoConfig.Tool;

internal sealed class GitHubRoadmapSyncer
{
    private static readonly TimeSpan GitHubSyncTimeout = TimeSpan.FromSeconds(30);
    private const decimal ProjectNumberTolerance = 0.000001m;
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
        var repository = GetGitHubRepository();
        var itemsWithIssues = GetItemsToSync();
        if (itemsWithIssues.Length == 0)
        {
            return EmptySyncResult();
        }

        List<string> messages = [];
        using var ownedHttpClient = _httpClient is null ? CreateGitHubClient() : null;
        var httpClient = _httpClient ?? ownedHttpClient;
        var client = httpClient ?? throw new InvalidOperationException("GitHub sync could not create an HTTP client.");
        var projectClient = CreateProjectClient(client);
        await VerifyProjectTarget(projectClient, cancellationToken).ConfigureAwait(false);

        foreach (var configuredItem in itemsWithIssues)
        {
            await SyncItem(client, repository, projectClient, configuredItem, messages, cancellationToken).ConfigureAwait(false);
        }

        return new GitHubSyncResult(messages);
    }

    private string GetGitHubRepository()
    {
        if (!_project.GitHubEnabled)
        {
            throw new InvalidOperationException("GitHub sync is disabled in roadmap/config.json.");
        }

        return string.IsNullOrWhiteSpace(_project.GitHubRepository)
            ? throw new InvalidOperationException("roadmap/config.json must define integrations.github.repository before GitHub sync.")
            : _project.GitHubRepository;
    }

    private RoadmapItemSnapshot[] GetItemsToSync() =>
        _project.Items.Where(item => item.GitHubIssue is not null || item.CreateGitHubIssue).OrderByPriority().ToArray();

    private static GitHubSyncResult EmptySyncResult() =>
        new(["No roadmap items have GitHub issue mappings or explicit creation requests."]);

    private GitHubProjectClient? CreateProjectClient(HttpClient httpClient) =>
        _project.GitHubProjectTarget is null ? null : new GitHubProjectClient(httpClient);

    private async Task VerifyProjectTarget(GitHubProjectClient? projectClient, CancellationToken cancellationToken)
    {
        if (projectClient is null)
        {
            return;
        }

        var target = _project.GitHubProjectTarget ?? throw new InvalidOperationException("GitHub Project target is required.");
        await RunProjectOperation(async token =>
        {
            await projectClient.VerifyTarget(target, token).ConfigureAwait(false);
            return true;
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task SyncItem(
        HttpClient httpClient,
        string repository,
        GitHubProjectClient? projectClient,
        RoadmapItemSnapshot configuredItem,
        List<string> messages,
        CancellationToken cancellationToken)
    {
        var item = await CreateOrAdoptIssue(httpClient, repository, configuredItem, messages, cancellationToken).ConfigureAwait(false);
        await SyncLabels(httpClient, repository, item, messages, cancellationToken).ConfigureAwait(false);
        await ProjectItem(projectClient, repository, item, messages, cancellationToken).ConfigureAwait(false);

        if (item.Labels.Count == 0 && projectClient is null)
        {
            messages.Add($"skipped {repository}#{item.GitHubIssue} from {item.Id} because it has no labels");
        }
    }

    private async Task<RoadmapItemSnapshot> CreateOrAdoptIssue(
        HttpClient httpClient,
        string repository,
        RoadmapItemSnapshot item,
        List<string> messages,
        CancellationToken cancellationToken)
    {
        if (!item.CreateGitHubIssue)
        {
            return item;
        }

        var creationLock = AcquireIssueCreationLock(item);
        var retainCreationLock = false;
        var creationRequestStarted = false;
        var issueCreated = false;
        try
        {
            var mapping = PrepareIssueMapping(item);
            if (TryGetMappedIssueNumber(mapping.GitHub, out var existingIssueNumber))
            {
                return item with { GitHubIssue = existingIssueNumber, CreateGitHubIssue = false };
            }

            EnsureCreateIntent(mapping.GitHub, item.Path);
            creationRequestStarted = true;
            var creation = await RunProjectOperation(token => CreateIssue(httpClient, repository, item, token), cancellationToken).ConfigureAwait(false);
            if (creation.FailureStatus is HttpStatusCode failureStatus)
            {
                retainCreationLock = (int)failureStatus >= 500;
                throw CreateGitHubFailure("issue creation", failureStatus);
            }

            var issueNumber = creation.IssueNumber ?? throw new JsonException("GitHub issue creation response did not contain a positive issue number.");
            issueCreated = true;
            PersistCreatedIssueMapping(item, issueNumber);
            messages.Add($"created {repository}#{issueNumber} from {item.Id}");
            return item with { GitHubIssue = issueNumber, CreateGitHubIssue = false };
        }
        catch (Exception exception) when (issueCreated || (creationRequestStarted && IsAmbiguousIssueCreationFailure(exception)))
        {
            retainCreationLock = true;
            throw;
        }
        finally
        {
            var lockPath = creationLock.Name;
            await creationLock.DisposeAsync().ConfigureAwait(false);
            if (!retainCreationLock)
            {
                File.Delete(lockPath);
            }
        }
    }

    private void PersistCreatedIssueMapping(RoadmapItemSnapshot item, int issueNumber)
    {
        var mapping = PrepareIssueMapping(item);
        try
        {
            EnsureCreateIntent(mapping.GitHub, item.Path);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidOperationException($"GitHub issue #{issueNumber} was created but its roadmap mapping changed while synchronizing: {item.Path}. Verify the mapping before retrying.", exception);
        }

        try
        {
            PersistIssueMapping(mapping.Path, mapping.Root, issueNumber);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"GitHub issue #{issueNumber} was created but its roadmap mapping could not be persisted. Set integrations.github.issue to {issueNumber} in {item.Path} before retrying.", exception);
        }
    }

    private static bool IsAmbiguousIssueCreationFailure(Exception exception)
    {
        return exception is GitHubSyncTimeoutException or OperationCanceledException or HttpRequestException or IOException or JsonException;
    }

    private async Task SyncLabels(
        HttpClient httpClient,
        string repository,
        RoadmapItemSnapshot item,
        List<string> messages,
        CancellationToken cancellationToken)
    {
        var extraLabels = await UpdateItem(httpClient, repository, item, cancellationToken).ConfigureAwait(false);
        if (item.Labels.Count > 0)
        {
            messages.Add($"updated labels for {repository}#{item.GitHubIssue} from {item.Id}");
        }

        foreach (var label in extraLabels)
        {
            messages.Add($"drift: {repository}#{item.GitHubIssue} has extra GitHub label {label}");
        }
    }

    private async Task ProjectItem(
        GitHubProjectClient? projectClient,
        string repository,
        RoadmapItemSnapshot item,
        List<string> messages,
        CancellationToken cancellationToken)
    {
        if (projectClient is null)
        {
            return;
        }

        var target = _project.GitHubProjectTarget ?? throw new InvalidOperationException("GitHub Project target is required.");
        var conflicts = await RunProjectOperation(token => EnsureProjectMembershipAndProjectFields(projectClient, target, repository, item, token), cancellationToken).ConfigureAwait(false);
        messages.Add($"projected {repository}#{item.GitHubIssue} to GitHub Project {target.Number}");
        foreach (var conflict in conflicts)
        {
            messages.Add($"drift: {repository}#{item.GitHubIssue} Project field {conflict} cannot be projected from roadmap source");
        }
    }

    private static async Task<IReadOnlyList<string>> EnsureProjectMembershipAndProjectFields(
        GitHubProjectClient projectClient,
        GitHubProjectTarget target,
        string repository,
        RoadmapItemSnapshot item,
        CancellationToken cancellationToken)
    {
        var issueNumber = item.GitHubIssue ?? throw new InvalidOperationException("GitHub issue mapping is required.");
        var issueId = await projectClient.GetIssueNodeId(repository, issueNumber, cancellationToken).ConfigureAwait(false);
        var projectItemId = await projectClient.FindItemId(target, issueId, cancellationToken).ConfigureAwait(false)
            ?? await projectClient.AddIssue(target, issueId, cancellationToken).ConfigureAwait(false);
        return await ProjectFields(projectClient, target, projectItemId, item, cancellationToken).ConfigureAwait(false);
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
        var projection = new ProjectFieldProjection(projectClient, target, itemId, fields, values, conflicts, cancellationToken);
        await projection.ProjectNumber("Roadmap order", item.Order).ConfigureAwait(false);
        await projection.ProjectNumber("RICE reach", item.Reach).ConfigureAwait(false);
        await projection.ProjectNumber("RICE impact", item.Impact).ConfigureAwait(false);
        await projection.ProjectNumber("RICE confidence", item.Confidence).ConfigureAwait(false);
        await projection.ProjectNumber("RICE effort", item.Effort).ConfigureAwait(false);
        await projection.ProjectNumber("RICE score", item.Score).ConfigureAwait(false);
        await projection.ProjectStatus(item.Status).ConfigureAwait(false);
        await projection.ProjectText("Roadmap parent", item.Parent ?? string.Empty).ConfigureAwait(false);
        await projection.ProjectText("Roadmap blocked by", string.Join(", ", item.BlockedBy)).ConfigureAwait(false);
        await projection.ProjectText("Roadmap tags", string.Join(", ", item.Tags)).ConfigureAwait(false);

        return conflicts;
    }

    private sealed class ProjectFieldProjection(
        GitHubProjectClient projectClient,
        GitHubProjectTarget target,
        string itemId,
        IReadOnlyList<GitHubProjectResponse.ProjectField> fields,
        IReadOnlyList<GitHubProjectItemResponse.FieldValue> values,
        List<string> conflicts,
        CancellationToken cancellationToken)
    {
        public async Task ProjectNumber(string fieldName, decimal value)
        {
            var field = FindCompatibleField(fieldName, "NUMBER");
            if (field is null)
            {
                return;
            }

            var fieldId = field.Id ?? throw new InvalidOperationException("GitHub Project field identifier is required.");
            var existing = values.FirstOrDefault(candidate => string.Equals(candidate.Field?.Id, field.Id, StringComparison.Ordinal));
            if (existing?.Number is decimal existingNumber && Math.Abs(existingNumber - value) > ProjectNumberTolerance)
            {
                conflicts.Add(fieldName);
                return;
            }

            if (existing?.Number is null)
            {
                await projectClient.UpdateNumber(target, itemId, fieldId, value, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task ProjectStatus(string status)
        {
            var field = FindCompatibleField("Roadmap status", "SINGLE_SELECT");
            if (field is null)
            {
                return;
            }

            var fieldId = field.Id ?? throw new InvalidOperationException("GitHub Project field identifier is required.");
            var option = field.Options?.FirstOrDefault(candidate => string.Equals(candidate.Name, status, StringComparison.Ordinal));
            if (string.IsNullOrWhiteSpace(option?.Id))
            {
                conflicts.Add("Roadmap status");
                return;
            }

            var existing = values.FirstOrDefault(candidate => string.Equals(candidate.Field?.Id, field.Id, StringComparison.Ordinal));
            if (!string.IsNullOrWhiteSpace(existing?.OptionId) && !string.Equals(existing.OptionId, option.Id, StringComparison.Ordinal))
            {
                conflicts.Add("Roadmap status");
                return;
            }

            if (string.IsNullOrWhiteSpace(existing?.OptionId))
            {
                await projectClient.UpdateSingleSelect(target, itemId, fieldId, option.Id, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task ProjectText(string fieldName, string value)
        {
            var field = FindCompatibleField(fieldName, "TEXT");
            if (field is null)
            {
                return;
            }

            var fieldId = field.Id ?? throw new InvalidOperationException("GitHub Project field identifier is required.");
            var existing = values.FirstOrDefault(candidate => string.Equals(candidate.Field?.Id, field.Id, StringComparison.Ordinal));
            if (!string.IsNullOrWhiteSpace(existing?.Text) && !string.Equals(existing.Text, value, StringComparison.Ordinal))
            {
                conflicts.Add(fieldName);
                return;
            }

            if (string.IsNullOrWhiteSpace(existing?.Text))
            {
                await projectClient.UpdateText(target, itemId, fieldId, value, cancellationToken).ConfigureAwait(false);
            }
        }

        private GitHubProjectResponse.ProjectField? FindCompatibleField(string fieldName, string dataType)
        {
            var candidates = fields.Where(field => string.Equals(field.Name, fieldName, StringComparison.Ordinal)).ToArray();
            if (candidates.Length != 1
                || string.IsNullOrWhiteSpace(candidates[0].Id)
                || !string.Equals(candidates[0].DataType, dataType, StringComparison.Ordinal))
            {
                conflicts.Add(fieldName);
                return null;
            }

            return candidates[0];
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
            return EmptySyncResult();
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
                throw CreateGitHubFailure("label sync", labelResponse.StatusCode, issueNumber);
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
            throw CreateGitHubFailure("label drift check", response.StatusCode, issueNumber);
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

    private static async Task<(int? IssueNumber, HttpStatusCode? FailureStatus)> CreateIssue(HttpClient httpClient, string repository, RoadmapItemSnapshot item, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.github.com/repos/{repository}/issues")
        {
            Content = new StringContent(BuildIssuePayload(item), Encoding.UTF8, "application/json")
        };
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return (null, response.StatusCode);
        }

        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!document.RootElement.TryGetProperty("number", out var number) || !number.TryGetInt32(out var issueNumber) || issueNumber < 1)
        {
            throw new JsonException("GitHub issue creation response did not contain a positive issue number.");
        }

        return (issueNumber, null);
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
            return new FileStream(lockPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.None);
        }
        catch (IOException exception)
        {
            throw new InvalidOperationException($"GitHub issue creation is already in progress for {item.Id}. If no sync is running, verify whether an issue was created, then remove stale lock file {lockPath} and retry.", exception);
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

        throw CreateGitHubFailure("pull request check", response.StatusCode, issueNumber);
    }

    private static InvalidOperationException CreateGitHubFailure(string operation, HttpStatusCode statusCode, int? issueNumber = null)
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

        var issueSuffix = issueNumber is null ? string.Empty : $" for #{issueNumber.Value}";
        return new InvalidOperationException($"GitHub {operation} failed{issueSuffix}: HTTP {(int)statusCode}{hint}.");
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
