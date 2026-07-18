namespace SharedKernel.RepoConfig.Tool;

internal sealed record GitHubRoadmapIntakeIssue(int Number, string Title, string State, IReadOnlyList<string> Labels, int? ParentNumber);
