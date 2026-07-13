namespace SharedKernel.RepoConfig.Tool;

internal sealed record RoadmapItemSnapshot(
    string Id,
    string Path,
    string Title,
    string Type,
    string Status,
    string Theme,
    int Order,
    string? Parent,
    decimal Reach,
    decimal Impact,
    decimal Confidence,
    decimal Effort,
    IReadOnlyList<string> BlockedBy,
    IReadOnlyList<string> Blocks,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Labels,
    int? GitHubIssue)
{
    public decimal Score => Effort == 0 ? 0 : Reach * Impact * Confidence / Effort;
}
