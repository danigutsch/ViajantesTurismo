using System.Text;
using System.Text.Json;

namespace SharedKernel.RepoConfig.Tool;

internal sealed class GitHubProjectClient(HttpClient httpClient)
{
    public async Task VerifyTarget(GitHubProjectTarget target, CancellationToken cancellationToken)
    {
        const string Query = "query($id:ID!){node(id:$id){... on ProjectV2{id number owner{... on User{login} ... on Organization{login}}}}}";
        var response = await Send(Query, writer =>
        {
            writer.WriteString("id", target.Id);
        }, cancellationToken).ConfigureAwait(false);
        var project = response.Data?.Node;
        if (!string.Equals(project?.Id, target.Id, StringComparison.Ordinal)
            || project?.Number != target.Number
            || !string.Equals(project.Owner?.Login, target.Owner, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Configured GitHub Project target could not be verified.");
        }
    }

    public async Task<string> GetIssueNodeId(string repository, int issueNumber, CancellationToken cancellationToken)
    {
        var parts = repository.Split('/');
        const string Query = "query($owner:String!,$repo:String!,$number:Int!){repository(owner:$owner,name:$repo){issue(number:$number){id}}}";
        var response = await Send(Query, writer =>
        {
            writer.WriteString("owner", parts[0]);
            writer.WriteString("repo", parts[1]);
            writer.WriteNumber("number", issueNumber);
        }, cancellationToken).ConfigureAwait(false);
        var repositoryResult = response.Data?.Repository;
        var issueId = repositoryResult?.Issue?.Id;
        return string.IsNullOrWhiteSpace(issueId)
            ? throw new InvalidOperationException($"GitHub issue mapping could not be found: #{issueNumber}.")
            : issueId;
    }

    public async Task AddIssue(GitHubProjectTarget target, string issueId, CancellationToken cancellationToken)
    {
        const string Query = "mutation($projectId:ID!,$contentId:ID!){addProjectV2ItemById(input:{projectId:$projectId,contentId:$contentId}){item{id}}}";
        var response = await Send(Query, writer =>
        {
            writer.WriteString("projectId", target.Id);
            writer.WriteString("contentId", issueId);
        }, cancellationToken).ConfigureAwait(false);
        if (response.Data?.AddProjectV2ItemById?.Item is null)
        {
            throw new InvalidOperationException("GitHub Project item could not be added.");
        }
    }

    public async Task<bool> HasIssue(GitHubProjectTarget target, string issueId, CancellationToken cancellationToken)
    {
        const string Query = "query($id:ID!,$after:String){node(id:$id){... on ProjectV2{items(first:100,after:$after){nodes{id content{... on Issue{id}} pageInfo{hasNextPage endCursor}}}}}}";
        string? cursor = null;
        GitHubProjectResponse.ProjectItemConnection? items;
        do
        {
            var response = await Send(Query, writer =>
            {
                writer.WriteString("id", target.Id);
                if (cursor is not null)
                {
                    writer.WriteString("after", cursor);
                }
                else
                {
                    writer.WriteNull("after");
                }
            }, cancellationToken).ConfigureAwait(false);
            items = response.Data?.Node?.Items;
            if (items?.Nodes?.Any(item => string.Equals(item.Content?.Id, issueId, StringComparison.Ordinal)) == true)
            {
                return true;
            }

            cursor = items?.PageInfo?.EndCursor;
        }
        while (items?.PageInfo?.HasNextPage == true);

        return false;
    }

    private async Task<GitHubProjectResponse> Send(string query, Action<Utf8JsonWriter> writeVariables, CancellationToken cancellationToken)
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
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"GitHub Project request failed with HTTP {(int)response.StatusCode}.");
        }

        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        var result = JsonSerializer.Deserialize(content, GitHubProjectJsonContext.Default.GitHubProjectResponse)
            ?? throw new InvalidOperationException("GitHub Project request returned no result.");
        var errorType = result.Errors?.Select(error => error.Type).FirstOrDefault(type => !string.IsNullOrWhiteSpace(type));
        if (!string.IsNullOrWhiteSpace(errorType))
        {
            throw new InvalidOperationException($"GitHub Project request failed ({errorType}).");
        }

        return result;
    }
}
