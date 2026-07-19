using System.Text.Json;

namespace SharedKernel.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class RoadmapReconciliationTests
{
    [Fact]
    public void Open_issue_reconciliation_manifest_preserves_exact_blocker_edge_invariants()
    {
        // Arrange
        using var reconciliationDocument = JsonDocument.Parse(RoadmapConfigSchemaTestFiles.ReadCheckedInReconciliation());

        // Act
        var root = reconciliationDocument.RootElement;
        var openIssueNumbers = root.GetProperty("directCanonicalPrimaries")
            .EnumerateArray()
            .Select(primary => primary.GetProperty("issue").GetInt32())
            .Concat(root.GetProperty("childrenOfCanonicalPrimaries").EnumerateArray().Select(issue => issue.GetInt32()))
            .Concat(root.GetProperty("unmappedStructuralRoots").EnumerateArray().Select(issue => issue.GetInt32()))
            .Concat(root.GetProperty("needsHuman").EnumerateArray().Select(issue => issue.GetInt32()))
            .ToHashSet();
        var rules = root.GetProperty("rules").EnumerateArray().Select(rule => rule.GetString() ?? string.Empty).ToArray();
        var snapshotDigest = root.GetProperty("snapshotDigest").GetString() ?? string.Empty;
        var edges = root.GetProperty("blockerEdges")
            .EnumerateArray()
            .Select(edge => (
                edge.GetProperty("blocker").GetInt32(),
                edge.GetProperty("blockerState").GetString() ?? string.Empty,
                edge.GetProperty("blocked").GetInt32(),
                edge.GetProperty("blockedState").GetString() ?? string.Empty))
            .ToArray();
        var expectedCount = root.GetProperty("integrity").GetProperty("expectedBlockerEdgeCount").GetInt32();
        var edgeKeys = edges.Select(edge => $"{edge.Item1}:{edge.Item2}:{edge.Item3}:{edge.Item4}").ToArray();
        var closedItemTransitionIssues = root.TryGetProperty("closedItemTransitions", out var closedItemTransitions)
            ? closedItemTransitions.EnumerateArray().Select(transition => transition.GetProperty("issue").GetInt32()).ToArray()
            : [];
        var closedEndpointIssueNumbers = edges
            .Where(edge => edge.Item2 == "CLOSED")
            .Select(edge => edge.Item1)
            .Concat(edges.Where(edge => edge.Item4 == "CLOSED").Select(edge => edge.Item3))
            .Concat(closedItemTransitionIssues)
            .ToHashSet();

        // Assert
        rules.ShouldContain("Only exact GitHub blocker relationships may become canonical blockedBy and blocks links.");
        snapshotDigest.Length.ShouldBe(64);
        root.GetProperty("integrity").GetProperty("dispositionsAreDisjoint").GetBoolean().ShouldBeTrue();
        root.GetProperty("integrity").GetProperty("dispositionsCoverSnapshot").GetBoolean().ShouldBeTrue();
        expectedCount.ShouldBe(edges.Length);
        edgeKeys.Distinct(StringComparer.Ordinal).Count().ShouldBe(edgeKeys.Length);
        foreach (var edge in edges)
        {
            edge.Item1.ShouldBeGreaterThan(0);
            edge.Item3.ShouldBeGreaterThan(0);
            edge.Item2.ShouldBeOneOf("OPEN", "CLOSED");
            edge.Item4.ShouldBeOneOf("OPEN", "CLOSED");
            if (edge.Item2 == "OPEN")
            {
                openIssueNumbers.Contains(edge.Item1).ShouldBeTrue();
            }
            else
            {
                closedEndpointIssueNumbers.Contains(edge.Item1).ShouldBeTrue();
            }

            if (edge.Item4 == "OPEN")
            {
                openIssueNumbers.Contains(edge.Item3).ShouldBeTrue();
            }
            else
            {
                closedEndpointIssueNumbers.Contains(edge.Item3).ShouldBeTrue();
            }
        }

        foreach (var transitionIssue in closedItemTransitionIssues)
        {
            closedEndpointIssueNumbers.Contains(transitionIssue).ShouldBeTrue();
        }
    }

    [Fact]
    public void Checked_in_roadmap_blocker_links_are_reciprocal()
    {
        // Arrange
        var itemContents = RoadmapConfigSchemaTestFiles.ReadCheckedInItems();

        // Act
        Dictionary<string, string[]> blockedByLinksById = new(StringComparer.Ordinal);
        Dictionary<string, string[]> blocksLinksById = new(StringComparer.Ordinal);
        foreach (var itemContent in itemContents)
        {
            using var itemDocument = JsonDocument.Parse(itemContent);
            var root = itemDocument.RootElement;
            var itemId = root.GetProperty("id").GetString() ?? string.Empty;
            var blockedByLinks = root.GetProperty("blockedBy").EnumerateArray().Select(link => link.GetString() ?? string.Empty).ToArray();
            var blocksLinks = root.GetProperty("blocks").EnumerateArray().Select(link => link.GetString() ?? string.Empty).ToArray();
            blockedByLinksById.Add(itemId, blockedByLinks);
            blocksLinksById.Add(itemId, blocksLinks);
        }

        // Assert
        foreach (var (itemId, blockedByLinks) in blockedByLinksById)
        {
            foreach (var blockerId in blockedByLinks)
            {
                blocksLinksById.TryGetValue(blockerId, out var blockerBlocks).ShouldBeTrue();
                blockerBlocks.ShouldNotBeNull().ShouldContain(itemId);
            }
        }

        foreach (var (itemId, blocksLinks) in blocksLinksById)
        {
            foreach (var blockedItemId in blocksLinks)
            {
                blockedByLinksById.TryGetValue(blockedItemId, out var blockedItemBlockers).ShouldBeTrue();
                blockedItemBlockers.ShouldNotBeNull().ShouldContain(itemId);
            }
        }
    }
}
