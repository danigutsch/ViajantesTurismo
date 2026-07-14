using System.Net;

namespace SharedKernel.RepoConfig.Tests;

internal static class GitHubProjectSyncTestOperations
{
    public static void EnableProjectTarget(TemporaryRepoConfigWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var configText = workspace.ReadFile("roadmap/config.json");
        workspace.WriteFile("roadmap/config.json", configText.Replace(
            "\"sourceOfTruth\": \"projection\"",
            "\"sourceOfTruth\": \"projection\",\n      \"projectV2\": { \"id\": \"project-id\", \"owner\": \"owner\", \"number\": 1 }",
            StringComparison.Ordinal));
    }

    public static void MapDefaultItem(TemporaryRepoConfigWorkspace workspace, int issueNumber = 997)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace(
            "\"labels\": [",
            $"\"integrations\": {{ \"github\": {{ \"issue\": {issueNumber} }} }},\n  \"labels\": [",
            StringComparison.Ordinal));
    }

    public static void EnqueueExistingProjectItem(
        TestHttpMessageHandler handler,
        string projectFields,
        string fieldValues)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectFields);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldValues);

        handler.EnqueueJson(HttpStatusCode.OK, "{ \"data\": { \"node\": { \"id\": \"project-id\", \"number\": 1, \"owner\": { \"login\": \"owner\" } } } }");
        handler.EnqueueJson(HttpStatusCode.NotFound, "{}");
        handler.EnqueueJson(HttpStatusCode.OK, "{ \"labels\": [] }");
        handler.EnqueueJson(HttpStatusCode.OK, "{}");
        handler.EnqueueJson(HttpStatusCode.OK, "{ \"data\": { \"repository\": { \"issue\": { \"id\": \"issue-id\" } } } }");
        handler.EnqueueJson(HttpStatusCode.OK, "{ \"data\": { \"node\": { \"items\": { \"nodes\": [{ \"id\": \"item-id\", \"content\": { \"id\": \"issue-id\" } }], \"pageInfo\": { \"hasNextPage\": false, \"endCursor\": null } } } } }");
        handler.EnqueueJson(HttpStatusCode.OK, projectFields);
        handler.EnqueueJson(HttpStatusCode.OK, fieldValues);
    }
}
