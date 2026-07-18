using System.Globalization;
using System.Net;
using System.Text.Json;

namespace SharedKernel.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
[Collection("Repo config tool environment")]
public sealed class GitHubRoadmapReconcileTests
{
    [Fact]
    public async Task Reconcile_github_reports_the_seed_manifest_inputs_when_missing()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        RoadmapConfigTestOperations.EnableGitHubSync(workspace);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["reconcile", "github", "--root", workspace.RootPath], output, error, workspace.RootPath, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(1);
        output.ToString().ShouldBe(string.Empty);
        error.ToString().ShouldContain("GitHub reconciliation requires one seed manifest under roadmap/reconciliation/open-issues-*.json.", StringComparison.Ordinal);
        error.ToString().ShouldContain("Required reviewed inputs: mechanicalPriorityOverride, directCanonicalPrimaries, and closedItemTransitions.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reconcile_github_defaults_to_dry_run_and_requests_only_structural_metadata()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        GitHubRoadmapIntakeTestOperations.Configure(
            workspace,
            """
            {
              "repository": "owner/repository",
              "retrievedOn": "2026-07-17",
              "repositoryCommit": "0000000000000000000000000000000000000000",
              "source": "GitHub GraphQL repository.issues OPEN snapshot with parent, subissue, label, and blocker metadata.",
              "ruleVersion": "structural-parent-subissue-blocker-v1",
              "mechanicalPriorityOverride": {
                "reach": 1,
                "impact": "1 plus direct open blockers, capped at 5.",
                "confidence": 0.1,
                "effort": { "type: epic": 8, "type: feature": 5, "type: enabler": 3, "type: docs": 2, "type: chore": 2, "default": 3 },
                "order": "Existing reviewed orders remain first; imported work uses topological order, score descending, then canonical ID."
              },
              "rules": ["Only exact GitHub blocker relationships may become canonical blockedBy and blocks links."],
              "blockerEdges": [],
              "closedItemTransitions": [],
              "directCanonicalPrimaries": [],
              "childrenOfCanonicalPrimaries": [],
              "unmappedStructuralRoots": [],
              "needsHuman": [],
              "needsHumanParentChainExits": [],
              "integrity": {
                "expectedIssueCount": 0,
                "expectedDirectCanonicalPrimaryCount": 0,
                "expectedChildrenOfCanonicalPrimaryCount": 0,
                "expectedUnmappedStructuralRootCount": 0,
                "expectedNeedsHumanCount": 0,
                "expectedBlockerEdgeCount": 0,
                "dispositionsAreDisjoint": true,
                "dispositionsCoverSnapshot": true
              }
            }
            """);
        var stateBefore = GitHubRoadmapIntakeTestOperations.ReadRoadmapState(workspace);
        using var handler = new TestHttpMessageHandler();
        handler.EnqueueJson(
            HttpStatusCode.OK,
            """
            {
              "data": {
                "repository": {
                  "defaultBranchRef": { "target": { "oid": "1111111111111111111111111111111111111111" } },
                  "issues": {
                    "nodes": [
                      {
                        "__typename": "Issue",
                        "number": 100,
                        "title": "Epic one hundred",
                        "state": "OPEN",
                        "labels": { "nodes": [{ "name": "type: epic" }], "pageInfo": { "hasNextPage": false, "endCursor": null } },
                        "parent": null,
                        "subIssues": { "nodes": [], "pageInfo": { "hasNextPage": false, "endCursor": null } },
                        "blockedBy": { "nodes": [], "pageInfo": { "hasNextPage": false, "endCursor": null } },
                        "blocking": { "nodes": [], "pageInfo": { "hasNextPage": false, "endCursor": null } }
                      }
                    ],
                    "pageInfo": { "hasNextPage": false, "endCursor": null }
                  }
                }
              }
            }
            """);
        using var httpClient = new HttpClient(handler);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["reconcile", "github", "--root", workspace.RootPath], output, error, workspace.RootPath, httpClient, TestContext.Current.CancellationToken);
        var stateAfter = GitHubRoadmapIntakeTestOperations.ReadRoadmapState(workspace);
        var request = handler.LastRequest.ShouldNotBeNull();
        var requestBody = request.Body.ShouldNotBeNull();

        // Assert
        exitCode.ShouldBe(0);
        output.ToString().ShouldContain("dry-run: GitHub reconciliation would update", StringComparison.Ordinal);
        error.ToString().ShouldBe(string.Empty);
        stateAfter.ShouldBe(stateBefore);
        request.Message.Method.ShouldBe(HttpMethod.Post);
        request.PathAndQuery.ShouldBe("/graphql");
        requestBody.ShouldContain("number", StringComparison.Ordinal);
        requestBody.ShouldContain("title", StringComparison.Ordinal);
        requestBody.ShouldContain("state", StringComparison.Ordinal);
        requestBody.ShouldContain("labels", StringComparison.Ordinal);
        requestBody.ShouldContain("parent", StringComparison.Ordinal);
        requestBody.ShouldContain("subIssues", StringComparison.Ordinal);
        requestBody.ShouldContain("blockedBy", StringComparison.Ordinal);
        requestBody.ShouldContain("blocking", StringComparison.Ordinal);
        requestBody.ShouldNotContain("body", StringComparison.Ordinal);
        requestBody.ShouldNotContain("comments", StringComparison.Ordinal);
        requestBody.ShouldNotContain("token", StringComparison.Ordinal);
        requestBody.ShouldNotContain("mutation", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reconcile_github_apply_derives_a_stable_manifest()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var firstOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var firstError = new StringWriter(CultureInfo.InvariantCulture);
        using var secondOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var secondError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        GitHubRoadmapIntakeTestOperations.Configure(
            workspace,
            """
            {
              "repository": "owner/repository",
              "retrievedOn": "2026-07-17",
              "repositoryCommit": "0000000000000000000000000000000000000000",
              "source": "GitHub GraphQL repository.issues OPEN snapshot with parent, subissue, label, and blocker metadata.",
              "ruleVersion": "structural-parent-subissue-blocker-v1",
              "mechanicalPriorityOverride": {
                "reach": 1,
                "impact": "1 plus direct open blockers, capped at 5.",
                "confidence": 0.1,
                "effort": { "type: epic": 8, "type: feature": 5, "type: enabler": 3, "type: docs": 2, "type: chore": 2, "default": 3 },
                "order": "Existing reviewed orders remain first; imported work uses topological order, score descending, then canonical ID."
              },
              "rules": ["Only exact GitHub blocker relationships may become canonical blockedBy and blocks links."],
              "blockerEdges": [],
              "closedItemTransitions": [],
              "directCanonicalPrimaries": [],
              "childrenOfCanonicalPrimaries": [],
              "unmappedStructuralRoots": [],
              "needsHuman": [],
              "needsHumanParentChainExits": [],
              "integrity": {
                "expectedIssueCount": 0,
                "expectedDirectCanonicalPrimaryCount": 0,
                "expectedChildrenOfCanonicalPrimaryCount": 0,
                "expectedUnmappedStructuralRootCount": 0,
                "expectedNeedsHumanCount": 0,
                "expectedBlockerEdgeCount": 0,
                "dispositionsAreDisjoint": true,
                "dispositionsCoverSnapshot": true
              }
            }
            """);
        using var firstHandler = new TestHttpMessageHandler();
        firstHandler.EnqueueJson(
            HttpStatusCode.OK,
            """
            {
              "data": {
                "repository": {
                  "defaultBranchRef": { "target": { "oid": "1111111111111111111111111111111111111111" } },
                  "issues": {
                    "nodes": [
                      {
                        "__typename": "Issue",
                        "number": 100,
                        "title": "Epic one hundred",
                        "state": "OPEN",
                        "labels": { "nodes": [{ "name": "type: epic" }], "pageInfo": { "hasNextPage": false, "endCursor": null } },
                        "parent": null,
                        "subIssues": { "nodes": [], "pageInfo": { "hasNextPage": false, "endCursor": null } },
                        "blockedBy": { "nodes": [], "pageInfo": { "hasNextPage": false, "endCursor": null } },
                        "blocking": { "nodes": [], "pageInfo": { "hasNextPage": false, "endCursor": null } }
                      }
                    ],
                    "pageInfo": { "hasNextPage": false, "endCursor": null }
                  }
                }
              }
            }
            """);
        using var firstHttpClient = new HttpClient(firstHandler);

        // Act
        var firstExitCode = await RepoConfigToolApplication.Run(["reconcile", "github", "--apply", "--root", workspace.RootPath], firstOutput, firstError, workspace.RootPath, firstHttpClient, TestContext.Current.CancellationToken);
        var manifestAfterFirstApply = workspace.ReadFile("roadmap/reconciliation/open-issues-test.json");
        using var manifestDocument = JsonDocument.Parse(manifestAfterFirstApply);
        using var secondHandler = new TestHttpMessageHandler();
        secondHandler.EnqueueJson(
            HttpStatusCode.OK,
            """
            {
              "data": {
                "repository": {
                  "defaultBranchRef": { "target": { "oid": "1111111111111111111111111111111111111111" } },
                  "issues": {
                    "nodes": [
                      {
                        "__typename": "Issue",
                        "number": 100,
                        "title": "Epic one hundred",
                        "state": "OPEN",
                        "labels": { "nodes": [{ "name": "type: epic" }], "pageInfo": { "hasNextPage": false, "endCursor": null } },
                        "parent": null,
                        "subIssues": { "nodes": [], "pageInfo": { "hasNextPage": false, "endCursor": null } },
                        "blockedBy": { "nodes": [], "pageInfo": { "hasNextPage": false, "endCursor": null } },
                        "blocking": { "nodes": [], "pageInfo": { "hasNextPage": false, "endCursor": null } }
                      }
                    ],
                    "pageInfo": { "hasNextPage": false, "endCursor": null }
                  }
                }
              }
            }
            """);
        using var secondHttpClient = new HttpClient(secondHandler);
        var secondExitCode = await RepoConfigToolApplication.Run(["reconcile", "github", "--apply", "--root", workspace.RootPath], secondOutput, secondError, workspace.RootPath, secondHttpClient, TestContext.Current.CancellationToken);
        var manifestAfterSecondApply = workspace.ReadFile("roadmap/reconciliation/open-issues-test.json");
        var root = manifestDocument.RootElement;
        var structuralRoots = root.GetProperty("unmappedStructuralRoots").EnumerateArray().Select(issue => issue.GetInt32()).ToArray();

        // Assert
        firstExitCode.ShouldBe(0);
        firstOutput.ToString().ShouldContain("reconcile: updated", StringComparison.Ordinal);
        firstError.ToString().ShouldBe(string.Empty);
        root.GetProperty("repositoryCommit").GetString().ShouldBe("1111111111111111111111111111111111111111");
        structuralRoots.ShouldHaveSingleItem().ShouldBe(100);
        root.GetProperty("needsHuman").GetArrayLength().ShouldBe(0);
        root.GetProperty("integrity").GetProperty("expectedIssueCount").GetInt32().ShouldBe(1);
        root.GetProperty("integrity").GetProperty("expectedUnmappedStructuralRootCount").GetInt32().ShouldBe(1);
        secondExitCode.ShouldBe(0);
        secondOutput.ToString().ShouldContain("reconcile: reconciliation manifest already matches the GitHub snapshot.", StringComparison.Ordinal);
        secondError.ToString().ShouldBe(string.Empty);
        manifestAfterSecondApply.ShouldBe(manifestAfterFirstApply);
    }
}
