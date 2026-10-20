namespace ViajantesTurismo.RepoConfig.Tool;

internal sealed record GitHubRoadmapReconcileSnapshot(string? RepositoryCommit, IReadOnlyList<GitHubRoadmapReconcileIssue> Issues);
