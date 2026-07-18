using System.Security.Cryptography;
using System.Text;

namespace SharedKernel.RepoConfig.Tool;

internal static class GitHubIssueSnapshotDigest
{
    public static string Compute(IEnumerable<GitHubRoadmapReconcileIssue> issues)
    {
        var content = new StringBuilder();
        foreach (var issue in issues.OrderBy(issue => issue.Number))
        {
            content.Append(issue.Number).Append('\0')
                .Append(issue.Title).Append('\0')
                .Append(issue.State).Append('\0')
                .Append(FormatRelation(issue.Parent)).Append('\0')
                .AppendJoin('\u001f', issue.Labels.Order(StringComparer.Ordinal))
                .Append('\0').AppendJoin('\u001f', issue.SubIssues.Select(FormatRelation).Order(StringComparer.Ordinal))
                .Append('\0').AppendJoin('\u001f', issue.BlockedBy.Select(FormatRelation).Order(StringComparer.Ordinal))
                .Append('\0').AppendJoin('\u001f', issue.Blocking.Select(FormatRelation).Order(StringComparer.Ordinal))
                .Append('\n');
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content.ToString())));
    }

    private static string FormatRelation(GitHubRoadmapReconcileRelation? relation) => relation is null
        ? "-"
        : $"{relation.Repository}:{relation.Number}:{relation.State}";
}
