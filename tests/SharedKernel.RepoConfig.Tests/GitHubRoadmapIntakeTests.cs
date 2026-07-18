using System.Globalization;
using System.Net;
using System.Text.Json;

namespace SharedKernel.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
[Collection("Repo config tool environment")]
public sealed class GitHubRoadmapIntakeTests
{
    [Fact]
    public async Task Intake_github_defaults_to_dry_run_and_fetches_only_safe_metadata()
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
              "mechanicalPriorityOverride": {
                "reach": 1,
                "impact": "1 plus direct open blockers, capped at 5.",
                "confidence": 0.1,
                "effort": { "type: epic": 8, "type: feature": 5, "type: enabler": 3, "type: docs": 2, "type: chore": 2, "default": 3 },
                "order": "Existing reviewed orders remain first; imported work uses topological order, score descending, then canonical ID."
              },
              "blockerEdges": [],
              "directCanonicalPrimaries": [],
              "childrenOfCanonicalPrimaries": [],
              "unmappedStructuralRoots": [],
              "needsHuman": [300, 100],
              "needsHumanParentChainExits": [],
              "integrity": {
                "expectedIssueCount": 2,
                "expectedDirectCanonicalPrimaryCount": 0,
                "expectedChildrenOfCanonicalPrimaryCount": 0,
                "expectedUnmappedStructuralRootCount": 0,
                "expectedNeedsHumanCount": 2,
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
                  "issues": {
                    "nodes": [
                      { "__typename": "Issue", "number": 300, "title": "Three hundred", "state": "OPEN", "labels": { "nodes": [{ "name": "type: docs" }], "pageInfo": { "hasNextPage": false } }, "parent": null },
                      { "__typename": "Issue", "number": 100, "title": "One hundred", "state": "OPEN", "labels": { "nodes": [{ "name": "type: enabler" }], "pageInfo": { "hasNextPage": false } }, "parent": null }
                    ],
                    "pageInfo": { "hasNextPage": false, "endCursor": null }
                  }
                }
              }
            }
            """);
        using var httpClient = new HttpClient(handler);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["intake", "github", "--root", workspace.RootPath], output, error, workspace.RootPath, httpClient, TestContext.Current.CancellationToken);
        var outputText = output.ToString();
        var errorText = error.ToString();
        var stateAfter = GitHubRoadmapIntakeTestOperations.ReadRoadmapState(workspace);
        var request = handler.LastRequest.ShouldNotBeNull();
        var requestBody = request.Body.ShouldNotBeNull();

        // Assert
        exitCode.ShouldBe(0);
        outputText.ShouldContain("dry-run: GitHub intake would add 2 open roadmap items.", StringComparison.Ordinal);
        errorText.ShouldBe(string.Empty);
        stateAfter.ShouldBe(stateBefore);
        request.Message.Method.ShouldBe(HttpMethod.Post);
        request.PathAndQuery.ShouldBe("/graphql");
        requestBody.ShouldContain("number", StringComparison.Ordinal);
        requestBody.ShouldContain("title", StringComparison.Ordinal);
        requestBody.ShouldContain("state", StringComparison.Ordinal);
        requestBody.ShouldContain("labels", StringComparison.Ordinal);
        requestBody.ShouldContain("parent", StringComparison.Ordinal);
        requestBody.ShouldNotContain("body", StringComparison.Ordinal);
        requestBody.ShouldNotContain("comments", StringComparison.Ordinal);
        requestBody.ShouldNotContain("token", StringComparison.Ordinal);
        requestBody.ShouldNotContain("mutation", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Intake_github_apply_allocates_links_prioritizes_and_is_idempotent()
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
        GitHubProjectSyncTestOperations.MapDefaultItem(workspace);
        GitHubRoadmapIntakeTestOperations.Configure(
            workspace,
            """
            {
              "repository": "owner/repository",
              "mechanicalPriorityOverride": {
                "reach": 1,
                "impact": "1 plus direct open blockers, capped at 5.",
                "confidence": 0.1,
                "effort": { "type: epic": 8, "type: feature": 5, "type: enabler": 3, "type: docs": 2, "type: chore": 2, "default": 3 },
                "order": "Existing reviewed orders remain first; imported work uses topological order, score descending, then canonical ID."
              },
              "blockerEdges": [
                { "blocker": 98, "blockerState": "CLOSED", "blocked": 100, "blockedState": "OPEN" },
                { "blocker": 99, "blockerState": "CLOSED", "blocked": 200, "blockedState": "OPEN" },
                { "blocker": 100, "blockerState": "OPEN", "blocked": 300, "blockedState": "OPEN" },
                { "blocker": 200, "blockerState": "OPEN", "blocked": 300, "blockedState": "OPEN" }
              ],
              "directCanonicalPrimaries": [{ "issue": 997, "roadmapItem": "RM-001" }],
              "childrenOfCanonicalPrimaries": [],
              "unmappedStructuralRoots": [],
              "needsHuman": [300, 100, 200],
              "needsHumanParentChainExits": [],
              "integrity": {
                "expectedIssueCount": 4,
                "expectedDirectCanonicalPrimaryCount": 1,
                "expectedChildrenOfCanonicalPrimaryCount": 0,
                "expectedUnmappedStructuralRootCount": 0,
                "expectedNeedsHumanCount": 3,
                "expectedBlockerEdgeCount": 4,
                "dispositionsAreDisjoint": true,
                "dispositionsCoverSnapshot": true
              }
            }
            """);
        var reviewedItemBefore = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        using var firstHandler = new TestHttpMessageHandler();
        firstHandler.EnqueueJson(
            HttpStatusCode.OK,
            """
            {
              "data": {
                "repository": {
                  "issues": {
                    "nodes": [
                      { "__typename": "Issue", "number": 300, "title": "Documentation work", "state": "OPEN", "labels": { "nodes": [{ "name": "type: docs" }], "pageInfo": { "hasNextPage": false } }, "parent": { "number": 100 } },
                      { "__typename": "Issue", "number": 997, "title": "Reviewed title must remain untouched", "state": "OPEN", "labels": { "nodes": [{ "name": "type: epic" }], "pageInfo": { "hasNextPage": false } }, "parent": null },
                      { "__typename": "Issue", "number": 200, "title": "Enabler work", "state": "OPEN", "labels": { "nodes": [{ "name": "type: enabler" }], "pageInfo": { "hasNextPage": false } }, "parent": null },
                      { "__typename": "Issue", "number": 100, "title": "Epic work", "state": "OPEN", "labels": { "nodes": [{ "name": "type: epic" }], "pageInfo": { "hasNextPage": false } }, "parent": null }
                    ],
                    "pageInfo": { "hasNextPage": false, "endCursor": null }
                  }
                }
              }
            }
            """);
        firstHandler.EnqueueJson(
            HttpStatusCode.OK,
            """
            {
              "data": {
                "repository": {
                  "issueOrPullRequest": { "__typename": "Issue", "number": 98, "title": "Closed support one", "state": "CLOSED", "labels": { "nodes": [], "pageInfo": { "hasNextPage": false } }, "parent": null }
                }
              }
            }
            """);
        firstHandler.EnqueueJson(
            HttpStatusCode.OK,
            """
            {
              "data": {
                "repository": {
                  "issueOrPullRequest": { "__typename": "Issue", "number": 99, "title": "Closed support two", "state": "CLOSED", "labels": { "nodes": [{ "name": "type: chore" }], "pageInfo": { "hasNextPage": false } }, "parent": null }
                }
              }
            }
            """);
        using var firstHttpClient = new HttpClient(firstHandler);

        // Act
        var firstExitCode = await RepoConfigToolApplication.Run(["intake", "github", "--apply", "--root", workspace.RootPath], firstOutput, firstError, workspace.RootPath, firstHttpClient, TestContext.Current.CancellationToken);
        var stateAfterFirstApply = GitHubRoadmapIntakeTestOperations.ReadRoadmapState(workspace);
        using var firstImportedItem = JsonDocument.Parse(workspace.ReadFile("roadmap/items/RM-018-github-100.json"));
        using var dependentItem = JsonDocument.Parse(workspace.ReadFile("roadmap/items/RM-020-github-300.json"));
        using var closedSupportItem = JsonDocument.Parse(workspace.ReadFile("roadmap/items/RM-021-github-98.json"));
        using var orderDocument = JsonDocument.Parse(workspace.ReadFile("roadmap/order.json"));
        using var secondHandler = new TestHttpMessageHandler();
        secondHandler.EnqueueJson(
            HttpStatusCode.OK,
            """
            {
              "data": {
                "repository": {
                  "issues": {
                    "nodes": [
                      { "__typename": "Issue", "number": 300, "title": "Documentation work", "state": "OPEN", "labels": { "nodes": [{ "name": "type: docs" }], "pageInfo": { "hasNextPage": false } }, "parent": { "number": 100 } },
                      { "__typename": "Issue", "number": 997, "title": "Reviewed title must remain untouched", "state": "OPEN", "labels": { "nodes": [{ "name": "type: epic" }], "pageInfo": { "hasNextPage": false } }, "parent": null },
                      { "__typename": "Issue", "number": 200, "title": "Enabler work", "state": "OPEN", "labels": { "nodes": [{ "name": "type: enabler" }], "pageInfo": { "hasNextPage": false } }, "parent": null },
                      { "__typename": "Issue", "number": 100, "title": "Epic work", "state": "OPEN", "labels": { "nodes": [{ "name": "type: epic" }], "pageInfo": { "hasNextPage": false } }, "parent": null }
                    ],
                    "pageInfo": { "hasNextPage": false, "endCursor": null }
                  }
                }
              }
            }
            """);
        secondHandler.EnqueueJson(
            HttpStatusCode.OK,
            """
            {
              "data": {
                "repository": {
                  "issueOrPullRequest": { "__typename": "Issue", "number": 98, "title": "Closed support one", "state": "CLOSED", "labels": { "nodes": [], "pageInfo": { "hasNextPage": false } }, "parent": null }
                }
              }
            }
            """);
        secondHandler.EnqueueJson(
            HttpStatusCode.OK,
            """
            {
              "data": {
                "repository": {
                  "issueOrPullRequest": { "__typename": "Issue", "number": 99, "title": "Closed support two", "state": "CLOSED", "labels": { "nodes": [{ "name": "type: chore" }], "pageInfo": { "hasNextPage": false } }, "parent": null }
                }
              }
            }
            """);
        using var secondHttpClient = new HttpClient(secondHandler);
        var secondExitCode = await RepoConfigToolApplication.Run(["intake", "github", "--apply", "--root", workspace.RootPath], secondOutput, secondError, workspace.RootPath, secondHttpClient, TestContext.Current.CancellationToken);
        var stateAfterSecondApply = GitHubRoadmapIntakeTestOperations.ReadRoadmapState(workspace);
        var firstItemRoot = firstImportedItem.RootElement;
        var dependentRoot = dependentItem.RootElement;
        var closedSupportRoot = closedSupportItem.RootElement;
        var orderedItems = orderDocument.RootElement.GetProperty("items").EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();
        var dependentBlockers = dependentRoot.GetProperty("blockedBy").EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();
        var closedSupportBlocks = closedSupportRoot.GetProperty("blocks").EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();

        // Assert
        firstExitCode.ShouldBe(0);
        firstOutput.ToString().ShouldContain("intake: added 3 open roadmap items.", StringComparison.Ordinal);
        firstError.ToString().ShouldBe(string.Empty);
        workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json").ShouldBe(reviewedItemBefore);
        firstItemRoot.GetProperty("id").GetString().ShouldBe("RM-018");
        firstItemRoot.GetProperty("integrations").GetProperty("github").GetProperty("issue").GetInt32().ShouldBe(100);
        firstItemRoot.GetProperty("scoring").GetProperty("effort").GetDecimal().ShouldBe(8m);
        dependentRoot.GetProperty("parent").GetString().ShouldBe("RM-018");
        dependentRoot.GetProperty("scoring").GetProperty("impact").GetDecimal().ShouldBe(3m);
        dependentRoot.GetProperty("scoring").GetProperty("effort").GetDecimal().ShouldBe(2m);
        dependentBlockers.ShouldBe(["RM-018", "RM-019"]);
        closedSupportRoot.GetProperty("status").GetString().ShouldBe("done");
        closedSupportRoot.GetProperty("triage").GetString().ShouldBe("untriaged");
        closedSupportBlocks.ShouldBe(["RM-018"]);
        orderedItems.ShouldBe(["RM-001", "RM-019", "RM-018", "RM-020"]);
        secondExitCode.ShouldBe(0);
        secondOutput.ToString().ShouldContain("intake: roadmap already matches the reconciliation manifest.", StringComparison.Ordinal);
        secondError.ToString().ShouldBe(string.Empty);
        stateAfterSecondApply.ShouldBe(stateAfterFirstApply);
    }

    [Fact]
    public async Task Intake_github_rejects_snapshot_drift_without_writing()
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
              "mechanicalPriorityOverride": {
                "reach": 1,
                "impact": "1 plus direct open blockers, capped at 5.",
                "confidence": 0.1,
                "effort": { "type: epic": 8, "type: feature": 5, "type: enabler": 3, "type: docs": 2, "type: chore": 2, "default": 3 },
                "order": "Existing reviewed orders remain first; imported work uses topological order, score descending, then canonical ID."
              },
              "blockerEdges": [],
              "directCanonicalPrimaries": [],
              "childrenOfCanonicalPrimaries": [],
              "unmappedStructuralRoots": [],
              "needsHuman": [100],
              "needsHumanParentChainExits": [],
              "integrity": {
                "expectedIssueCount": 1,
                "expectedDirectCanonicalPrimaryCount": 0,
                "expectedChildrenOfCanonicalPrimaryCount": 0,
                "expectedUnmappedStructuralRootCount": 0,
                "expectedNeedsHumanCount": 1,
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
                  "issues": {
                    "nodes": [
                      { "__typename": "Issue", "number": 101, "title": "Unexpected issue", "state": "OPEN", "labels": { "nodes": [], "pageInfo": { "hasNextPage": false } }, "parent": null }
                    ],
                    "pageInfo": { "hasNextPage": false, "endCursor": null }
                  }
                }
              }
            }
            """);
        using var httpClient = new HttpClient(handler);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["intake", "github", "--apply", "--root", workspace.RootPath], output, error, workspace.RootPath, httpClient, TestContext.Current.CancellationToken);
        var errorText = error.ToString();
        var stateAfter = GitHubRoadmapIntakeTestOperations.ReadRoadmapState(workspace);

        // Assert
        exitCode.ShouldBe(1);
        output.ToString().ShouldBe(string.Empty);
        errorText.ShouldContain("GitHub issue snapshot does not match the reconciliation manifest.", StringComparison.Ordinal);
        errorText.ShouldContain("Missing: #100. Unexpected: #101.", StringComparison.Ordinal);
        stateAfter.ShouldBe(stateBefore);
    }

    [Fact]
    public async Task Intake_github_rejects_pull_requests_without_writing()
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
              "mechanicalPriorityOverride": {
                "reach": 1,
                "impact": "1 plus direct open blockers, capped at 5.",
                "confidence": 0.1,
                "effort": { "type: epic": 8, "type: feature": 5, "type: enabler": 3, "type: docs": 2, "type: chore": 2, "default": 3 },
                "order": "Existing reviewed orders remain first; imported work uses topological order, score descending, then canonical ID."
              },
              "blockerEdges": [{ "blocker": 99, "blockerState": "CLOSED", "blocked": 100, "blockedState": "OPEN" }],
              "directCanonicalPrimaries": [],
              "childrenOfCanonicalPrimaries": [],
              "unmappedStructuralRoots": [],
              "needsHuman": [100],
              "needsHumanParentChainExits": [],
              "integrity": {
                "expectedIssueCount": 1,
                "expectedDirectCanonicalPrimaryCount": 0,
                "expectedChildrenOfCanonicalPrimaryCount": 0,
                "expectedUnmappedStructuralRootCount": 0,
                "expectedNeedsHumanCount": 1,
                "expectedBlockerEdgeCount": 1,
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
                  "issues": {
                    "nodes": [
                      { "__typename": "Issue", "number": 100, "title": "Open issue", "state": "OPEN", "labels": { "nodes": [], "pageInfo": { "hasNextPage": false } }, "parent": null }
                    ],
                    "pageInfo": { "hasNextPage": false, "endCursor": null }
                  }
                }
              }
            }
            """);
        handler.EnqueueJson(
            HttpStatusCode.OK,
            """
            {
              "data": {
                "repository": {
                  "issueOrPullRequest": { "__typename": "PullRequest" }
                }
              }
            }
            """);
        using var httpClient = new HttpClient(handler);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["intake", "github", "--apply", "--root", workspace.RootPath], output, error, workspace.RootPath, httpClient, TestContext.Current.CancellationToken);
        var errorText = error.ToString();
        var stateAfter = GitHubRoadmapIntakeTestOperations.ReadRoadmapState(workspace);

        // Assert
        exitCode.ShouldBe(1);
        output.ToString().ShouldBe(string.Empty);
        errorText.ShouldContain("GitHub intake rejected a pull request: #99.", StringComparison.Ordinal);
        stateAfter.ShouldBe(stateBefore);
    }

    [Fact]
    public async Task Intake_github_transitions_a_declared_imported_item_to_closed_support()
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
        GitHubRoadmapIntakeTestOperations.AddImportedOpenItem(workspace);
        GitHubRoadmapIntakeTestOperations.Configure(
            workspace,
            """
            {
              "repository": "owner/repository",
              "mechanicalPriorityOverride": {
                "reach": 1,
                "impact": "1 plus direct open blockers, capped at 5.",
                "confidence": 0.1,
                "effort": { "type: epic": 8, "type: feature": 5, "type: enabler": 3, "type: docs": 2, "type: chore": 2, "default": 3 },
                "order": "Existing reviewed orders remain first; imported work uses topological order, score descending, then canonical ID."
              },
              "blockerEdges": [],
              "closedItemTransitions": [{ "issue": 100, "roadmapItem": "RM-018" }],
              "directCanonicalPrimaries": [],
              "childrenOfCanonicalPrimaries": [],
              "unmappedStructuralRoots": [],
              "needsHuman": [101],
              "needsHumanParentChainExits": [],
              "integrity": {
                "expectedIssueCount": 1,
                "expectedDirectCanonicalPrimaryCount": 0,
                "expectedChildrenOfCanonicalPrimaryCount": 0,
                "expectedUnmappedStructuralRootCount": 0,
                "expectedNeedsHumanCount": 1,
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
                  "issues": {
                    "nodes": [
                      { "__typename": "Issue", "number": 101, "title": "New open issue", "state": "OPEN", "labels": { "nodes": [], "pageInfo": { "hasNextPage": false } }, "parent": null }
                    ],
                    "pageInfo": { "hasNextPage": false, "endCursor": null }
                  }
                }
              }
            }
            """);
        firstHandler.EnqueueJson(
            HttpStatusCode.OK,
            """
            {
              "data": {
                "repository": {
                  "issueOrPullRequest": { "__typename": "Issue", "number": 100, "title": "Closed imported issue", "state": "CLOSED", "labels": { "nodes": [], "pageInfo": { "hasNextPage": false } }, "parent": null }
                }
              }
            }
            """);
        using var firstHttpClient = new HttpClient(firstHandler);

        // Act
        var firstExitCode = await RepoConfigToolApplication.Run(["intake", "github", "--apply", "--root", workspace.RootPath], firstOutput, firstError, workspace.RootPath, firstHttpClient, TestContext.Current.CancellationToken);
        var stateAfterFirstApply = GitHubRoadmapIntakeTestOperations.ReadRoadmapState(workspace);
        using var transitionedItem = JsonDocument.Parse(workspace.ReadFile("roadmap/items/RM-018-github-100.json"));
        using var newOpenItem = JsonDocument.Parse(workspace.ReadFile("roadmap/items/RM-019-github-101.json"));
        using var orderDocument = JsonDocument.Parse(workspace.ReadFile("roadmap/order.json"));
        using var secondHandler = new TestHttpMessageHandler();
        secondHandler.EnqueueJson(
            HttpStatusCode.OK,
            """
            {
              "data": {
                "repository": {
                  "issues": {
                    "nodes": [
                      { "__typename": "Issue", "number": 101, "title": "New open issue", "state": "OPEN", "labels": { "nodes": [], "pageInfo": { "hasNextPage": false } }, "parent": null }
                    ],
                    "pageInfo": { "hasNextPage": false, "endCursor": null }
                  }
                }
              }
            }
            """);
        secondHandler.EnqueueJson(
            HttpStatusCode.OK,
            """
            {
              "data": {
                "repository": {
                  "issueOrPullRequest": { "__typename": "Issue", "number": 100, "title": "Closed imported issue", "state": "CLOSED", "labels": { "nodes": [], "pageInfo": { "hasNextPage": false } }, "parent": null }
                }
              }
            }
            """);
        using var secondHttpClient = new HttpClient(secondHandler);
        var secondExitCode = await RepoConfigToolApplication.Run(["intake", "github", "--apply", "--root", workspace.RootPath], secondOutput, secondError, workspace.RootPath, secondHttpClient, TestContext.Current.CancellationToken);
        var stateAfterSecondApply = GitHubRoadmapIntakeTestOperations.ReadRoadmapState(workspace);
        var transitionedRoot = transitionedItem.RootElement;
        var newOpenRoot = newOpenItem.RootElement;
        var transitionedBlocks = transitionedRoot.GetProperty("blocks").EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();
        var newOpenBlockers = newOpenRoot.GetProperty("blockedBy").EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();
        var orderedItems = orderDocument.RootElement.GetProperty("items").EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();

        // Assert
        firstExitCode.ShouldBe(0);
        firstOutput.ToString().ShouldContain("intake: transitioned 1 imported roadmap item to closed support.", StringComparison.Ordinal);
        firstError.ToString().ShouldBe(string.Empty);
        transitionedRoot.GetProperty("status").GetString().ShouldBe("done");
        transitionedRoot.GetProperty("triage").GetString().ShouldBe("untriaged");
        transitionedRoot.TryGetProperty("order", out _).ShouldBeFalse();
        transitionedRoot.TryGetProperty("scoring", out _).ShouldBeFalse();
        transitionedBlocks.ShouldBe([]);
        newOpenBlockers.ShouldBe([]);
        orderedItems.ShouldBe(["RM-001", "RM-019"]);
        secondExitCode.ShouldBe(0);
        secondOutput.ToString().ShouldContain("intake: roadmap already matches the reconciliation manifest.", StringComparison.Ordinal);
        secondError.ToString().ShouldBe(string.Empty);
        stateAfterSecondApply.ShouldBe(stateAfterFirstApply);
    }

    [Fact]
    public async Task Intake_github_preserves_exact_blocker_links_during_a_closed_transition()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        GitHubRoadmapIntakeTestOperations.AddImportedOpenItem(workspace);
        GitHubRoadmapIntakeTestOperations.Configure(
            workspace,
            """
            {
              "repository": "owner/repository",
              "mechanicalPriorityOverride": {
                "reach": 1,
                "impact": "1 plus direct open blockers, capped at 5.",
                "confidence": 0.1,
                "effort": { "type: epic": 8, "type: feature": 5, "type: enabler": 3, "type: docs": 2, "type: chore": 2, "default": 3 },
                "order": "Existing reviewed orders remain first; imported work uses topological order, score descending, then canonical ID."
              },
              "blockerEdges": [{ "blocker": 100, "blockerState": "CLOSED", "blocked": 101, "blockedState": "OPEN" }],
              "closedItemTransitions": [{ "issue": 100, "roadmapItem": "RM-018" }],
              "directCanonicalPrimaries": [],
              "childrenOfCanonicalPrimaries": [],
              "unmappedStructuralRoots": [],
              "needsHuman": [101],
              "needsHumanParentChainExits": [],
              "integrity": {
                "expectedIssueCount": 1,
                "expectedDirectCanonicalPrimaryCount": 0,
                "expectedChildrenOfCanonicalPrimaryCount": 0,
                "expectedUnmappedStructuralRootCount": 0,
                "expectedNeedsHumanCount": 1,
                "expectedBlockerEdgeCount": 1,
                "dispositionsAreDisjoint": true,
                "dispositionsCoverSnapshot": true
              }
            }
            """);
        using var handler = new TestHttpMessageHandler();
        handler.EnqueueJson(
            HttpStatusCode.OK,
            """
            {
              "data": {
                "repository": {
                  "issues": {
                    "nodes": [
                      { "__typename": "Issue", "number": 101, "title": "New open issue", "state": "OPEN", "labels": { "nodes": [], "pageInfo": { "hasNextPage": false } }, "parent": null }
                    ],
                    "pageInfo": { "hasNextPage": false, "endCursor": null }
                  }
                }
              }
            }
            """);
        handler.EnqueueJson(
            HttpStatusCode.OK,
            """
            {
              "data": {
                "repository": {
                  "issueOrPullRequest": { "__typename": "Issue", "number": 100, "title": "Closed imported issue", "state": "CLOSED", "labels": { "nodes": [], "pageInfo": { "hasNextPage": false } }, "parent": null }
                }
              }
            }
            """);
        using var httpClient = new HttpClient(handler);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["intake", "github", "--apply", "--root", workspace.RootPath], output, error, workspace.RootPath, httpClient, TestContext.Current.CancellationToken);
        using var transitionedItem = JsonDocument.Parse(workspace.ReadFile("roadmap/items/RM-018-github-100.json"));
        using var newOpenItem = JsonDocument.Parse(workspace.ReadFile("roadmap/items/RM-019-github-101.json"));
        using var orderDocument = JsonDocument.Parse(workspace.ReadFile("roadmap/order.json"));
        var transitionedBlocks = transitionedItem.RootElement.GetProperty("blocks").EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();
        var newOpenBlockers = newOpenItem.RootElement.GetProperty("blockedBy").EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();
        var orderedItems = orderDocument.RootElement.GetProperty("items").EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();

        // Assert
        exitCode.ShouldBe(0);
        error.ToString().ShouldBe(string.Empty);
        transitionedBlocks.ShouldBe(["RM-019"]);
        newOpenBlockers.ShouldBe(["RM-018"]);
        orderedItems.ShouldBe(["RM-001", "RM-019"]);
    }

    [Fact]
    public async Task Intake_github_rejects_an_undeclared_imported_closed_transition_without_writing()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        GitHubRoadmapIntakeTestOperations.AddImportedOpenItem(workspace);
        GitHubRoadmapIntakeTestOperations.Configure(
            workspace,
            """
            {
              "repository": "owner/repository",
              "mechanicalPriorityOverride": {
                "reach": 1,
                "impact": "1 plus direct open blockers, capped at 5.",
                "confidence": 0.1,
                "effort": { "type: epic": 8, "type: feature": 5, "type: enabler": 3, "type: docs": 2, "type: chore": 2, "default": 3 },
                "order": "Existing reviewed orders remain first; imported work uses topological order, score descending, then canonical ID."
              },
              "blockerEdges": [{ "blocker": 100, "blockerState": "CLOSED", "blocked": 101, "blockedState": "OPEN" }],
              "directCanonicalPrimaries": [],
              "childrenOfCanonicalPrimaries": [],
              "unmappedStructuralRoots": [],
              "needsHuman": [101],
              "needsHumanParentChainExits": [],
              "integrity": {
                "expectedIssueCount": 1,
                "expectedDirectCanonicalPrimaryCount": 0,
                "expectedChildrenOfCanonicalPrimaryCount": 0,
                "expectedUnmappedStructuralRootCount": 0,
                "expectedNeedsHumanCount": 1,
                "expectedBlockerEdgeCount": 1,
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
                  "issues": {
                    "nodes": [
                      { "__typename": "Issue", "number": 101, "title": "New open issue", "state": "OPEN", "labels": { "nodes": [], "pageInfo": { "hasNextPage": false } }, "parent": null }
                    ],
                    "pageInfo": { "hasNextPage": false, "endCursor": null }
                  }
                }
              }
            }
            """);
        handler.EnqueueJson(
            HttpStatusCode.OK,
            """
            {
              "data": {
                "repository": {
                  "issueOrPullRequest": { "__typename": "Issue", "number": 100, "title": "Closed imported issue", "state": "CLOSED", "labels": { "nodes": [], "pageInfo": { "hasNextPage": false } }, "parent": null }
                }
              }
            }
            """);
        using var httpClient = new HttpClient(handler);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["intake", "github", "--apply", "--root", workspace.RootPath], output, error, workspace.RootPath, httpClient, TestContext.Current.CancellationToken);
        var stateAfter = GitHubRoadmapIntakeTestOperations.ReadRoadmapState(workspace);

        // Assert
        exitCode.ShouldBe(1);
        output.ToString().ShouldBe(string.Empty);
        error.ToString().ShouldContain("GitHub closed support issue #100 must be declared as a closed item transition.", StringComparison.Ordinal);
        stateAfter.ShouldBe(stateBefore);
    }

    [Fact]
    public async Task Intake_github_rejects_a_declared_transition_without_an_existing_mapping()
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
              "mechanicalPriorityOverride": {
                "reach": 1,
                "impact": "1 plus direct open blockers, capped at 5.",
                "confidence": 0.1,
                "effort": { "type: epic": 8, "type: feature": 5, "type: enabler": 3, "type: docs": 2, "type: chore": 2, "default": 3 },
                "order": "Existing reviewed orders remain first; imported work uses topological order, score descending, then canonical ID."
              },
              "blockerEdges": [],
              "closedItemTransitions": [{ "issue": 100, "roadmapItem": "RM-018" }],
              "directCanonicalPrimaries": [],
              "childrenOfCanonicalPrimaries": [],
              "unmappedStructuralRoots": [],
              "needsHuman": [101],
              "needsHumanParentChainExits": [],
              "integrity": {
                "expectedIssueCount": 1,
                "expectedDirectCanonicalPrimaryCount": 0,
                "expectedChildrenOfCanonicalPrimaryCount": 0,
                "expectedUnmappedStructuralRootCount": 0,
                "expectedNeedsHumanCount": 1,
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
                  "issues": {
                    "nodes": [
                      { "__typename": "Issue", "number": 101, "title": "New open issue", "state": "OPEN", "labels": { "nodes": [], "pageInfo": { "hasNextPage": false } }, "parent": null }
                    ],
                    "pageInfo": { "hasNextPage": false, "endCursor": null }
                  }
                }
              }
            }
            """);
        handler.EnqueueJson(
            HttpStatusCode.OK,
            """
            {
              "data": {
                "repository": {
                  "issueOrPullRequest": { "__typename": "Issue", "number": 100, "title": "Closed issue", "state": "CLOSED", "labels": { "nodes": [], "pageInfo": { "hasNextPage": false } }, "parent": null }
                }
              }
            }
            """);
        using var httpClient = new HttpClient(handler);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["intake", "github", "--apply", "--root", workspace.RootPath], output, error, workspace.RootPath, httpClient, TestContext.Current.CancellationToken);
        var stateAfter = GitHubRoadmapIntakeTestOperations.ReadRoadmapState(workspace);

        // Assert
        exitCode.ShouldBe(1);
        output.ToString().ShouldBe(string.Empty);
        error.ToString().ShouldContain("GitHub closed item transition requires an existing exact mapping: #100 -> RM-018.", StringComparison.Ordinal);
        stateAfter.ShouldBe(stateBefore);
    }

    [Fact]
    public async Task Intake_github_rejects_a_declared_transition_with_a_different_roadmap_item()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        GitHubRoadmapIntakeTestOperations.AddImportedOpenItem(workspace);
        GitHubRoadmapIntakeTestOperations.Configure(
            workspace,
            """
            {
              "repository": "owner/repository",
              "mechanicalPriorityOverride": {
                "reach": 1,
                "impact": "1 plus direct open blockers, capped at 5.",
                "confidence": 0.1,
                "effort": { "type: epic": 8, "type: feature": 5, "type: enabler": 3, "type: docs": 2, "type: chore": 2, "default": 3 },
                "order": "Existing reviewed orders remain first; imported work uses topological order, score descending, then canonical ID."
              },
              "blockerEdges": [],
              "closedItemTransitions": [{ "issue": 100, "roadmapItem": "RM-019" }],
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
                  "issues": {
                    "nodes": [],
                    "pageInfo": { "hasNextPage": false, "endCursor": null }
                  }
                }
              }
            }
            """);
        handler.EnqueueJson(
            HttpStatusCode.OK,
            """
            {
              "data": {
                "repository": {
                  "issueOrPullRequest": { "__typename": "Issue", "number": 100, "title": "Closed issue", "state": "CLOSED", "labels": { "nodes": [], "pageInfo": { "hasNextPage": false } }, "parent": null }
                }
              }
            }
            """);
        using var httpClient = new HttpClient(handler);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["intake", "github", "--apply", "--root", workspace.RootPath], output, error, workspace.RootPath, httpClient, TestContext.Current.CancellationToken);
        var stateAfter = GitHubRoadmapIntakeTestOperations.ReadRoadmapState(workspace);

        // Assert
        exitCode.ShouldBe(1);
        output.ToString().ShouldBe(string.Empty);
        error.ToString().ShouldContain("GitHub closed item transition requires an existing exact mapping: #100 -> RM-019.", StringComparison.Ordinal);
        stateAfter.ShouldBe(stateBefore);
    }

    [Fact]
    public async Task Intake_github_replaces_imported_relationships_with_exact_github_metadata()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        GitHubRoadmapIntakeTestOperations.AddImportedOpenPairWithStaleRelationship(workspace);
        GitHubRoadmapIntakeTestOperations.Configure(
            workspace,
            """
            {
              "repository": "owner/repository",
              "mechanicalPriorityOverride": {
                "reach": 1,
                "impact": "1 plus direct open blockers, capped at 5.",
                "confidence": 0.1,
                "effort": { "type: epic": 8, "type: feature": 5, "type: enabler": 3, "type: docs": 2, "type: chore": 2, "default": 3 },
                "order": "Existing reviewed orders remain first; imported work uses topological order, score descending, then canonical ID."
              },
              "blockerEdges": [],
              "closedItemTransitions": [],
              "directCanonicalPrimaries": [],
              "childrenOfCanonicalPrimaries": [],
              "unmappedStructuralRoots": [],
              "needsHuman": [100, 101],
              "needsHumanParentChainExits": [],
              "integrity": {
                "expectedIssueCount": 2,
                "expectedDirectCanonicalPrimaryCount": 0,
                "expectedChildrenOfCanonicalPrimaryCount": 0,
                "expectedUnmappedStructuralRootCount": 0,
                "expectedNeedsHumanCount": 2,
                "expectedBlockerEdgeCount": 0,
                "dispositionsAreDisjoint": true,
                "dispositionsCoverSnapshot": true
              }
            }
            """);
        using var handler = new TestHttpMessageHandler();
        handler.EnqueueJson(
            HttpStatusCode.OK,
            """
            {
              "data": {
                "repository": {
                  "issues": {
                    "nodes": [
                      { "__typename": "Issue", "number": 100, "title": "Imported parent", "state": "OPEN", "labels": { "nodes": [], "pageInfo": { "hasNextPage": false } }, "parent": null },
                      { "__typename": "Issue", "number": 101, "title": "Imported child", "state": "OPEN", "labels": { "nodes": [], "pageInfo": { "hasNextPage": false } }, "parent": { "number": 100 } }
                    ],
                    "pageInfo": { "hasNextPage": false, "endCursor": null }
                  }
                }
              }
            }
            """);
        using var httpClient = new HttpClient(handler);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["intake", "github", "--apply", "--root", workspace.RootPath], output, error, workspace.RootPath, httpClient, TestContext.Current.CancellationToken);
        using var parentItem = JsonDocument.Parse(workspace.ReadFile("roadmap/items/RM-018-github-100.json"));
        using var childItem = JsonDocument.Parse(workspace.ReadFile("roadmap/items/RM-019-github-101.json"));
        var parentBlocks = parentItem.RootElement.GetProperty("blocks").EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();
        var childBlockers = childItem.RootElement.GetProperty("blockedBy").EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();

        // Assert
        exitCode.ShouldBe(0);
        output.ToString().ShouldContain("intake: updated 2 existing roadmap items with exact GitHub relationships.", StringComparison.Ordinal);
        error.ToString().ShouldBe(string.Empty);
        parentBlocks.ShouldBeEmpty();
        childBlockers.ShouldBeEmpty();
        childItem.RootElement.GetProperty("parent").GetString().ShouldBe("RM-018");
    }

    [Fact]
    public async Task Intake_github_rejects_snapshot_metadata_drift_without_writing()
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
              "mechanicalPriorityOverride": {
                "reach": 1,
                "impact": "1 plus direct open blockers, capped at 5.",
                "confidence": 0.1,
                "effort": { "type: epic": 8, "type: feature": 5, "type: enabler": 3, "type: docs": 2, "type: chore": 2, "default": 3 },
                "order": "Existing reviewed orders remain first; imported work uses topological order, score descending, then canonical ID."
              },
              "snapshotDigest": "0000000000000000000000000000000000000000000000000000000000000000",
              "blockerEdges": [],
              "closedItemTransitions": [],
              "directCanonicalPrimaries": [],
              "childrenOfCanonicalPrimaries": [],
              "unmappedStructuralRoots": [],
              "needsHuman": [100],
              "needsHumanParentChainExits": [],
              "integrity": {
                "expectedIssueCount": 1,
                "expectedDirectCanonicalPrimaryCount": 0,
                "expectedChildrenOfCanonicalPrimaryCount": 0,
                "expectedUnmappedStructuralRootCount": 0,
                "expectedNeedsHumanCount": 1,
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
                        "title": "Changed after reconciliation",
                        "state": "OPEN",
                        "labels": { "nodes": [], "pageInfo": { "hasNextPage": false, "endCursor": null } },
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
        var exitCode = await RepoConfigToolApplication.Run(["intake", "github", "--apply", "--root", workspace.RootPath], output, error, workspace.RootPath, httpClient, TestContext.Current.CancellationToken);
        var stateAfter = GitHubRoadmapIntakeTestOperations.ReadRoadmapState(workspace);

        // Assert
        exitCode.ShouldBe(1);
        output.ToString().ShouldBe(string.Empty);
        error.ToString().ShouldContain("GitHub issue snapshot metadata does not match the reconciliation manifest.", StringComparison.Ordinal);
        stateAfter.ShouldBe(stateBefore);
    }
}
