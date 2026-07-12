namespace SharedKernel.RepoConfig.Tests;

internal static class RoadmapTestContent
{
    public const string IssueWithGitHubMappingJson = """
        {
          "$schema": "../schema/roadmap-item.schema.json",
          "id": "RM-002",
          "parent": "RM-001",
          "title": "Follow-up roadmap issue",
          "type": "issue",
          "status": "ready",
          "order": 20,
          "theme": "repo-operations",
          "outcome": "A follow-up issue is mapped to GitHub.",
          "scoring": {
            "reach": 10,
            "impact": 3,
            "confidence": 0.8,
            "effort": 2
          },
          "blockedBy": [],
          "blocks": [],
          "dependencies": [],
          "tags": [
            "roadmap"
          ],
          "labels": [
            "area: tooling"
          ],
          "integrations": {
            "github": {
              "issue": 997
            }
          }
        }
        """;

    public const string HigherPriorityIssueJson = """
        {
          "$schema": "../schema/roadmap-item.schema.json",
          "id": "RM-002",
          "parent": "RM-001",
          "title": "Higher priority roadmap issue",
          "type": "issue",
          "status": "ready",
          "order": 5,
          "theme": "repo-operations",
          "outcome": "An issue should appear before the epic in order.json.",
          "scoring": {
            "reach": 10,
            "impact": 3,
            "confidence": 0.8,
            "effort": 2
          },
          "blockedBy": [],
          "blocks": [],
          "dependencies": [],
          "tags": [
            "roadmap"
          ],
          "labels": [
            "area: tooling"
          ]
        }
        """;

    public const string BlockedIssueJson = """
        {
          "$schema": "../schema/roadmap-item.schema.json",
          "id": "RM-002",
          "parent": "RM-001",
          "title": "Follow-up roadmap issue",
          "type": "issue",
          "status": "ready",
          "order": 20,
          "theme": "repo-operations",
          "outcome": "A follow-up issue waits for the epic to close.",
          "scoring": {
            "reach": 10,
            "impact": 3,
            "confidence": 0.8,
            "effort": 2
          },
          "blockedBy": [
            "RM-001"
          ],
          "blocks": [],
          "dependencies": [],
          "tags": [
            "roadmap"
          ],
          "labels": [
            "area: tooling"
          ]
        }
        """;
}
