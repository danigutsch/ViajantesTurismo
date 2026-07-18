using System.Net;
using System.Text;
using System.Text.Json;

namespace SharedKernel.RepoConfig.Tool;

internal sealed class GitHubRoadmapIntakeClient(HttpClient httpClient)
{
    private static readonly TimeSpan GitHubIntakeTimeout = TimeSpan.FromSeconds(30);

    private const string OpenIssuesQuery = """
        query($owner:String!,$name:String!,$after:String){
          repository(owner:$owner,name:$name){
            issues(first:100,after:$after,filterBy:{states:OPEN}){
              nodes{
                __typename
                number
                title
                state
                labels(first:100){nodes{name}pageInfo{hasNextPage}}
                parent{number}
              }
              pageInfo{hasNextPage endCursor}
            }
          }
        }
        """;

    private const string IssueQuery = """
        query($owner:String!,$name:String!,$number:Int!){
          repository(owner:$owner,name:$name){
            issueOrPullRequest(number:$number){
              __typename
              ... on Issue{
                number
                title
                state
                labels(first:100){nodes{name}pageInfo{hasNextPage}}
                parent{number}
              }
            }
          }
        }
        """;

    public async Task<IReadOnlyList<GitHubRoadmapIntakeIssue>> ReadOpenIssues(string repository, CancellationToken cancellationToken)
    {
        var (owner, name) = SplitRepository(repository);
        List<GitHubRoadmapIntakeIssue> issues = [];
        HashSet<int> seenIssueNumbers = [];
        string? cursor = null;

        do
        {
            using var document = await Send(
                OpenIssuesQuery,
                writer =>
                {
                    writer.WriteString("owner", owner);
                    writer.WriteString("name", name);
                    if (cursor is null)
                    {
                        writer.WriteNull("after");
                    }
                    else
                    {
                        writer.WriteString("after", cursor);
                    }
                },
                cancellationToken).ConfigureAwait(false);
            var connection = GetRequiredObject(GetRepository(document.RootElement), "issues");
            var nodes = GetRequiredArray(connection, "nodes");
            foreach (var node in nodes.EnumerateArray())
            {
                var issue = ReadIssue(node, expectedIssueNumber: null);
                if (!seenIssueNumbers.Add(issue.Number))
                {
                    throw new InvalidOperationException("GitHub intake response contains duplicate issue numbers.");
                }

                issues.Add(issue);
            }

            var pageInfo = GetRequiredObject(connection, "pageInfo");
            var hasNextPage = GetRequiredBoolean(pageInfo, "hasNextPage");
            cursor = GetNullableString(pageInfo, "endCursor");
            if (hasNextPage && string.IsNullOrWhiteSpace(cursor))
            {
                throw new InvalidOperationException("GitHub intake response has an incomplete issue page.");
            }

            if (!hasNextPage)
            {
                break;
            }
        }
        while (true);

        return issues;
    }

    public async Task<GitHubRoadmapIntakeIssue> ReadIssue(string repository, int issueNumber, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(issueNumber, 1);

        var (owner, name) = SplitRepository(repository);
        using var document = await Send(
            IssueQuery,
            writer =>
            {
                writer.WriteString("owner", owner);
                writer.WriteString("name", name);
                writer.WriteNumber("number", issueNumber);
            },
            cancellationToken).ConfigureAwait(false);
        var repositoryElement = GetRepository(document.RootElement);
        if (!repositoryElement.TryGetProperty("issueOrPullRequest", out var issueElement) || issueElement.ValueKind == JsonValueKind.Null)
        {
            throw new InvalidOperationException($"GitHub intake could not read issue #{issueNumber}.");
        }

        return ReadIssue(issueElement, issueNumber);
    }

    private async Task<JsonDocument> Send(string query, Action<Utf8JsonWriter> writeVariables, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(GitHubIntakeTimeout);
        using var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        try
        {
            return await SendRequest(query, writeVariables, requestCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            throw new GitHubIntakeTimeoutException();
        }
    }

    private async Task<JsonDocument> SendRequest(string query, Action<Utf8JsonWriter> writeVariables, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("query", query);
            writer.WritePropertyName("variables");
            writer.WriteStartObject();
            writeVariables(writer);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        var endpoint = new UriBuilder(Uri.UriSchemeHttps, "api.github.com") { Path = "graphql" }.Uri;
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(Encoding.UTF8.GetString(stream.ToArray()), Encoding.UTF8, "application/json")
        };

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            throw new InvalidOperationException("GitHub intake request failed.", exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw CreateGitHubFailure(response.StatusCode);
            }

            JsonDocument document;
            try
            {
                using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException("GitHub intake response could not be read.", exception);
            }

            if (HasErrors(document.RootElement))
            {
                document.Dispose();
                throw new InvalidOperationException("GitHub intake request failed.");
            }

            return document;
        }
    }

    private static GitHubRoadmapIntakeIssue ReadIssue(JsonElement root, int? expectedIssueNumber)
    {
        var typename = GetRequiredString(root, "__typename");
        if (string.Equals(typename, "PullRequest", StringComparison.Ordinal))
        {
            var issueSuffix = expectedIssueNumber is int issueNumber ? $": #{issueNumber}" : string.Empty;
            throw new InvalidOperationException($"GitHub intake rejected a pull request{issueSuffix}.");
        }

        if (!string.Equals(typename, "Issue", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("GitHub intake response did not contain an issue.");
        }

        var number = GetRequiredPositiveInteger(root, "number");
        if (expectedIssueNumber is int expected && number != expected)
        {
            throw new InvalidOperationException($"GitHub intake response did not contain issue #{expected}.");
        }

        var title = GetRequiredString(root, "title");
        var state = GetRequiredString(root, "state");
        if (state is not "OPEN" and not "CLOSED")
        {
            throw new InvalidOperationException($"GitHub intake issue #{number} has an unsupported state.");
        }

        var labels = ReadLabels(root, number);
        var parentNumber = ReadParentNumber(root, number);
        return new GitHubRoadmapIntakeIssue(number, title, state, labels, parentNumber);
    }

    private static string[] ReadLabels(JsonElement root, int issueNumber)
    {
        var labels = GetRequiredObject(root, "labels");
        var pageInfo = GetRequiredObject(labels, "pageInfo");
        if (GetRequiredBoolean(pageInfo, "hasNextPage"))
        {
            throw new InvalidOperationException($"GitHub intake issue #{issueNumber} has incomplete labels.");
        }

        var nodes = GetRequiredArray(labels, "nodes");
        HashSet<string> values = new(StringComparer.Ordinal);
        foreach (var node in nodes.EnumerateArray())
        {
            var label = GetRequiredString(node, "name");
            if (!string.Equals(label, label.Trim(), StringComparison.Ordinal) || !values.Add(label))
            {
                throw new InvalidOperationException($"GitHub intake issue #{issueNumber} has invalid labels.");
            }
        }

        return values.Order(StringComparer.Ordinal).ToArray();
    }

    private static int? ReadParentNumber(JsonElement root, int issueNumber)
    {
        if (!root.TryGetProperty("parent", out var parent) || parent.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (parent.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException($"GitHub intake issue #{issueNumber} has an invalid parent.");
        }

        return GetRequiredPositiveInteger(parent, "number");
    }

    private static JsonElement GetRepository(JsonElement root)
    {
        var data = GetRequiredObject(root, "data");
        return GetRequiredObject(data, "repository");
    }

    private static bool HasErrors(JsonElement root) => root.TryGetProperty("errors", out var errors)
        && errors.ValueKind == JsonValueKind.Array
        && errors.GetArrayLength() > 0;

    private static JsonElement GetRequiredObject(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("GitHub intake response did not contain the expected metadata.");
        }

        return property;
    }

    private static JsonElement GetRequiredArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("GitHub intake response did not contain the expected metadata.");
        }

        return property;
    }

    private static string GetRequiredString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new InvalidOperationException("GitHub intake response did not contain the expected metadata.");
        }

        return property.GetString() ?? string.Empty;
    }

    private static string? GetNullableString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }

    private static int GetRequiredPositiveInteger(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.Number
            || !property.TryGetInt32(out var value)
            || value < 1)
        {
            throw new InvalidOperationException("GitHub intake response did not contain the expected metadata.");
        }

        return value;
    }

    private static bool GetRequiredBoolean(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            throw new InvalidOperationException("GitHub intake response did not contain the expected metadata.");
        }

        return property.GetBoolean();
    }

    private static InvalidOperationException CreateGitHubFailure(HttpStatusCode statusCode)
    {
        var hint = statusCode switch
        {
            HttpStatusCode.Unauthorized => " (authentication required)",
            HttpStatusCode.Forbidden => " (access denied or rate limited)",
            HttpStatusCode.NotFound => " (resource not found or inaccessible)",
            HttpStatusCode.TooManyRequests => " (rate limited)",
            _ => string.Empty
        };
        return new InvalidOperationException($"GitHub intake request failed: HTTP {(int)statusCode}{hint}.");
    }

    private static (string Owner, string Name) SplitRepository(string repository)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repository);

        var parts = repository.Split('/');
        if (parts.Length != 2)
        {
            throw new InvalidOperationException("GitHub intake requires a repository shaped as owner/repository.");
        }

        return (parts[0], parts[1]);
    }
}
