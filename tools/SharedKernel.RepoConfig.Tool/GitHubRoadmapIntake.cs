using System.Text;
using System.Text.Json;

namespace SharedKernel.RepoConfig.Tool;

internal sealed class GitHubRoadmapIntake
{
    private const int FirstImportedRoadmapNumber = 18;
    private const decimal ApprovedReach = 1m;
    private const decimal ApprovedConfidence = 0.1m;
    private const string DefaultTheme = "repo-operations";
    private const string OpenStatus = "proposed";
    private const string ClosedStatus = "done";

    private readonly HttpClient? _httpClient;
    private readonly RoadmapProject _project;

    internal GitHubRoadmapIntake(RoadmapProject project, HttpClient? httpClient)
    {
        ArgumentNullException.ThrowIfNull(project);

        _project = project;
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<string>> Preview(CancellationToken cancellationToken)
    {
        var plan = await CreatePlan(cancellationToken).ConfigureAwait(false);
        return plan.Messages(dryRun: true);
    }

    public async Task<IReadOnlyList<string>> Apply(CancellationToken cancellationToken)
    {
        var plan = await CreatePlan(cancellationToken).ConfigureAwait(false);
        plan.Apply();
        return plan.Messages(dryRun: false);
    }

    private async Task<IntakePlan> CreatePlan(CancellationToken cancellationToken)
    {
        var repository = GetGitHubRepository();
        var reconciliation = GitHubRoadmapReconciliation.Load(_project.RootPath);
        if (string.Equals(reconciliation.RuleVersion, "structural-parent-subissue-blocker-v1", StringComparison.Ordinal)
            && string.IsNullOrWhiteSpace(reconciliation.SnapshotDigest))
        {
            throw new InvalidOperationException("GitHub intake requires snapshotDigest; run reconcile github --apply first.");
        }

        if (!string.Equals(reconciliation.Repository, repository, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("GitHub reconciliation manifest repository does not match roadmap/config.json.");
        }

        using var ownedHttpClient = _httpClient is null ? GitHubHttpClient.Create("intake") : null;
        var httpClient = _httpClient ?? ownedHttpClient;
        var client = httpClient is null
            ? throw new InvalidOperationException("GitHub intake could not create an HTTP client.")
            : new GitHubRoadmapIntakeClient(httpClient);
        if (!string.IsNullOrWhiteSpace(reconciliation.SnapshotDigest))
        {
            var snapshot = await new GitHubRoadmapReconcileClient(httpClient).ReadSnapshot(repository, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(reconciliation.SnapshotDigest, GitHubIssueSnapshotDigest.Compute(snapshot.Issues), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("GitHub issue snapshot metadata does not match the reconciliation manifest.");
            }

            var snapshotIssues = snapshot.Issues.ToDictionary(issue => issue.Number);
            var openSnapshotIssues = snapshot.Issues
                .Where(issue => string.Equals(issue.State, "OPEN", StringComparison.Ordinal))
                .Select(ToIntakeIssue)
                .ToArray();
            var openSnapshotIssuesByNumber = ValidateOpenSnapshot(reconciliation, openSnapshotIssues);
            var closedSnapshotIssuesByNumber = ReadClosedEndpoints(reconciliation, snapshotIssues);
            return BuildPlan(reconciliation, openSnapshotIssuesByNumber, closedSnapshotIssuesByNumber);
        }

        var openIssues = await client.ReadOpenIssues(repository, cancellationToken).ConfigureAwait(false);
        var openIssuesByNumber = ValidateOpenSnapshot(reconciliation, openIssues);
        var closedIssuesByNumber = await ReadClosedEndpoints(client, repository, reconciliation, cancellationToken).ConfigureAwait(false);
        return BuildPlan(reconciliation, openIssuesByNumber, closedIssuesByNumber);
    }

    private string GetGitHubRepository()
    {
        if (!_project.GitHubEnabled)
        {
            throw new InvalidOperationException("GitHub intake is disabled in roadmap/config.json.");
        }

        return string.IsNullOrWhiteSpace(_project.GitHubRepository)
            ? throw new InvalidOperationException("roadmap/config.json must define integrations.github.repository before GitHub intake.")
            : _project.GitHubRepository;
    }

    private static Dictionary<int, GitHubRoadmapIntakeIssue> ValidateOpenSnapshot(
        GitHubRoadmapReconciliation reconciliation,
        IReadOnlyList<GitHubRoadmapIntakeIssue> openIssues)
    {
        if (openIssues.Any(issue => !string.Equals(issue.State, "OPEN", StringComparison.Ordinal))
            || openIssues.Select(issue => issue.Number).Distinct().Count() != openIssues.Count)
        {
            throw new InvalidOperationException("GitHub issue snapshot does not match the reconciliation manifest.");
        }

        var issuesByNumber = openIssues.ToDictionary(issue => issue.Number);
        var expectedNumbers = reconciliation.ExpectedOpenIssueNumbers;
        var expectedIssueNumbers = new HashSet<int>(expectedNumbers);
        var missingIssueNumbers = expectedIssueNumbers.Except(issuesByNumber.Keys).Order().ToArray();
        var unexpectedIssueNumbers = issuesByNumber.Keys.Except(expectedIssueNumbers).Order().ToArray();
        if (missingIssueNumbers.Length > 0 || unexpectedIssueNumbers.Length > 0)
        {
            throw new InvalidOperationException(
                $"GitHub issue snapshot does not match the reconciliation manifest. Missing: {FormatIssueNumbers(missingIssueNumbers)}. Unexpected: {FormatIssueNumbers(unexpectedIssueNumbers)}.");
        }

        foreach (var (issueNumber, expectedParent) in reconciliation.ParentChainExits)
        {
            if (!issuesByNumber.TryGetValue(issueNumber, out var issue) || issue.ParentNumber != expectedParent)
            {
                throw new InvalidOperationException("GitHub issue snapshot does not match the reconciliation manifest.");
            }
        }

        return issuesByNumber;
    }

    private static Dictionary<int, GitHubRoadmapIntakeIssue> ReadClosedEndpoints(
        GitHubRoadmapReconciliation reconciliation,
        Dictionary<int, GitHubRoadmapReconcileIssue> issuesByNumber)
    {
        Dictionary<int, GitHubRoadmapIntakeIssue> closedIssues = [];
        foreach (var (issueNumber, expectedState) in reconciliation.ClosedEndpointStates)
        {
            if (!issuesByNumber.TryGetValue(issueNumber, out var issue)
                || !string.Equals(issue.State, expectedState, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("GitHub issue snapshot does not match the reconciliation manifest.");
            }

            closedIssues.Add(issueNumber, ToIntakeIssue(issue));
        }

        return closedIssues;
    }

    private static async Task<Dictionary<int, GitHubRoadmapIntakeIssue>> ReadClosedEndpoints(
        GitHubRoadmapIntakeClient client,
        string repository,
        GitHubRoadmapReconciliation reconciliation,
        CancellationToken cancellationToken)
    {
        Dictionary<int, GitHubRoadmapIntakeIssue> issuesByNumber = [];
        foreach (var (issueNumber, expectedState) in reconciliation.ClosedEndpointStates.OrderBy(entry => entry.Key))
        {
            var issue = await client.ReadIssue(repository, issueNumber, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(issue.State, expectedState, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("GitHub issue snapshot does not match the reconciliation manifest.");
            }

            issuesByNumber.Add(issue.Number, issue);
        }

        return issuesByNumber;
    }

    private static GitHubRoadmapIntakeIssue ToIntakeIssue(GitHubRoadmapReconcileIssue issue) => new(
        issue.Number,
        issue.Title,
        issue.State,
        issue.Labels,
        issue.Parent?.Number);

    private static string FormatIssueNumbers(IEnumerable<int> issueNumbers)
    {
        var values = issueNumbers.Select(issueNumber => $"#{issueNumber}").ToArray();
        return values.Length == 0 ? "none" : string.Join(", ", values);
    }

    private IntakePlan BuildPlan(
        GitHubRoadmapReconciliation reconciliation,
        Dictionary<int, GitHubRoadmapIntakeIssue> openIssuesByNumber,
        Dictionary<int, GitHubRoadmapIntakeIssue> closedIssuesByNumber)
    {
        var existingItemsById = _project.Items.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var existingItemsByIssue = _project.Items
            .Where(item => item.GitHubIssue is int)
            .ToDictionary(item => item.GitHubIssue.GetValueOrDefault());
        ValidateDirectCanonicalPrimaries(reconciliation, existingItemsByIssue);
        ValidateCanonicalChildren(reconciliation, existingItemsByIssue);
        ValidateClosedItemTransitions(reconciliation, existingItemsByIssue);

        HashSet<string> usedItemIds = new(existingItemsById.Keys, StringComparer.Ordinal);
        Dictionary<int, string> itemIdsByIssue = existingItemsByIssue.ToDictionary(entry => entry.Key, entry => entry.Value.Id);
        List<IntakeCandidate> candidates = [];
        List<RoadmapItemSnapshot> closedItemTransitions = [];
        AllocateOpenItems(reconciliation, openIssuesByNumber, existingItemsByIssue, itemIdsByIssue, usedItemIds, candidates);
        AllocateClosedSupportItems(reconciliation, closedIssuesByNumber, existingItemsByIssue, itemIdsByIssue, usedItemIds, candidates, closedItemTransitions);
        AddExactBlockerLinks(reconciliation, itemIdsByIssue, candidates);
        AddParentLinks(candidates, itemIdsByIssue);
        var orderedOpenCandidates = OrderOpenCandidates(candidates.Where(candidate => candidate.IsOpen));
        SetOrders(orderedOpenCandidates, closedItemTransitions);

        return CreateWritePlan(reconciliation, openIssuesByNumber, itemIdsByIssue, candidates, existingItemsById, orderedOpenCandidates, closedItemTransitions);
    }

    private static void ValidateDirectCanonicalPrimaries(
        GitHubRoadmapReconciliation reconciliation,
        Dictionary<int, RoadmapItemSnapshot> existingItemsByIssue)
    {
        foreach (var (issueNumber, roadmapItemId) in reconciliation.DirectCanonicalPrimaries)
        {
            if (!existingItemsByIssue.TryGetValue(issueNumber, out var item)
                || !string.Equals(item.Id, roadmapItemId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"GitHub reconciliation direct canonical primary is not mapped exactly: #{issueNumber} -> {roadmapItemId}.");
            }
        }
    }

    private static void ValidateCanonicalChildren(
        GitHubRoadmapReconciliation reconciliation,
        Dictionary<int, RoadmapItemSnapshot> existingItemsByIssue)
    {
        var unmappedChild = reconciliation.ChildrenOfCanonicalPrimaries.FirstOrDefault(issueNumber => !existingItemsByIssue.ContainsKey(issueNumber));
        if (unmappedChild > 0)
        {
            throw new InvalidOperationException($"GitHub reconciliation child issue requires an exact existing mapping: #{unmappedChild}.");
        }
    }

    private static void ValidateClosedItemTransitions(
        GitHubRoadmapReconciliation reconciliation,
        Dictionary<int, RoadmapItemSnapshot> existingItemsByIssue)
    {
        foreach (var (issueNumber, roadmapItemId) in reconciliation.ClosedItemTransitions)
        {
            if (!existingItemsByIssue.TryGetValue(issueNumber, out var item)
                || !string.Equals(item.Id, roadmapItemId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"GitHub closed item transition requires an existing exact mapping: #{issueNumber} -> {roadmapItemId}.");
            }
        }
    }

    private static void AllocateOpenItems(
        GitHubRoadmapReconciliation reconciliation,
        Dictionary<int, GitHubRoadmapIntakeIssue> openIssuesByNumber,
        Dictionary<int, RoadmapItemSnapshot> existingItemsByIssue,
        Dictionary<int, string> itemIdsByIssue,
        HashSet<string> usedItemIds,
        List<IntakeCandidate> candidates)
    {
        var roadmapNumber = FirstImportedRoadmapNumber;
        foreach (var issueNumber in reconciliation.UnmappedOpenIssueNumbers.Order())
        {
            if (existingItemsByIssue.TryGetValue(issueNumber, out var existingItem))
            {
                itemIdsByIssue[issueNumber] = existingItem.Id;
                continue;
            }

            if (!openIssuesByNumber.TryGetValue(issueNumber, out var issue))
            {
                throw new InvalidOperationException("GitHub issue snapshot does not match the reconciliation manifest.");
            }

            var roadmapId = FindAvailableRoadmapId(ref roadmapNumber, usedItemIds);
            itemIdsByIssue.Add(issueNumber, roadmapId);
            candidates.Add(CreateCandidate(issue, roadmapId, isOpen: true, reconciliation.BlockerEdges));
        }
    }

    private void AllocateClosedSupportItems(
        GitHubRoadmapReconciliation reconciliation,
        Dictionary<int, GitHubRoadmapIntakeIssue> closedIssuesByNumber,
        Dictionary<int, RoadmapItemSnapshot> existingItemsByIssue,
        Dictionary<int, string> itemIdsByIssue,
        HashSet<string> usedItemIds,
        List<IntakeCandidate> candidates,
        List<RoadmapItemSnapshot> closedItemTransitions)
    {
        var roadmapNumber = FirstImportedRoadmapNumber;
        foreach (var issueNumber in reconciliation.ClosedEndpointStates.Keys.Order())
        {
            if (existingItemsByIssue.TryGetValue(issueNumber, out var existingItem))
            {
                if (!_project.IsClosed(existingItem))
                {
                    if (!reconciliation.ClosedItemTransitions.TryGetValue(issueNumber, out var transitionItemId)
                        || !string.Equals(existingItem.Id, transitionItemId, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"GitHub closed support issue #{issueNumber} must be declared as a closed item transition.");
                    }

                    closedItemTransitions.Add(existingItem);
                }

                itemIdsByIssue[issueNumber] = existingItem.Id;
                continue;
            }

            if (!closedIssuesByNumber.TryGetValue(issueNumber, out var issue))
            {
                throw new InvalidOperationException("GitHub issue snapshot does not match the reconciliation manifest.");
            }

            var roadmapId = FindAvailableRoadmapId(ref roadmapNumber, usedItemIds);
            itemIdsByIssue.Add(issueNumber, roadmapId);
            candidates.Add(CreateCandidate(issue, roadmapId, isOpen: false, reconciliation.BlockerEdges));
        }
    }

    private static IntakeCandidate CreateCandidate(
        GitHubRoadmapIntakeIssue issue,
        string roadmapId,
        bool isOpen,
        IReadOnlyCollection<GitHubRoadmapBlockerEdge> blockerEdges)
    {
        var classification = Classify(issue.Labels);
        var impact = isOpen
            ? Math.Min(5m, 1m + blockerEdges.Count(edge => edge.Blocked == issue.Number && string.Equals(edge.BlockerState, "OPEN", StringComparison.Ordinal)))
            : 0m;
        return new IntakeCandidate(issue, roadmapId, isOpen, classification.Type, classification.Effort, impact);
    }

    private static (string Type, decimal Effort) Classify(IReadOnlyList<string> labels)
    {
        var classifications = labels
            .Select(label => label switch
            {
                "type: epic" => "epic",
                "type: feature" => "feature",
                "type: enabler" => "enabler",
                "type: docs" or "type: documentation" => "documentation",
                "type: chore" => "chore",
                "type: issue" => "issue",
                _ => null
            })
            .Where(classification => classification is not null)
            .Select(classification => classification ?? string.Empty)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (classifications.Length > 1)
        {
            throw new InvalidOperationException("GitHub intake issue has conflicting type labels.");
        }

        return classifications.SingleOrDefault() switch
        {
            "epic" => ("epic", 8m),
            "feature" => ("feature", 5m),
            "enabler" => ("enabler", 3m),
            "documentation" => ("documentation", 2m),
            "chore" => ("issue", 2m),
            _ => ("issue", 3m)
        };
    }

    private static void AddExactBlockerLinks(
        GitHubRoadmapReconciliation reconciliation,
        Dictionary<int, string> itemIdsByIssue,
        List<IntakeCandidate> candidates)
    {
        var candidatesById = candidates.ToDictionary(candidate => candidate.Id, StringComparer.Ordinal);
        foreach (var edge in reconciliation.BlockerEdges)
        {
            if (!itemIdsByIssue.TryGetValue(edge.Blocker, out var blockerId)
                || !itemIdsByIssue.TryGetValue(edge.Blocked, out var blockedId))
            {
                throw new InvalidOperationException("GitHub reconciliation blocker edge cannot be mapped to roadmap items.");
            }

            if (candidatesById.TryGetValue(blockerId, out var blocker))
            {
                blocker.AddBlockedItem(blockedId);
            }

            if (candidatesById.TryGetValue(blockedId, out var blocked))
            {
                blocked.AddBlocker(blockerId);
            }
        }
    }

    private static void AddParentLinks(List<IntakeCandidate> candidates, Dictionary<int, string> itemIdsByIssue)
    {
        foreach (var candidate in candidates)
        {
            if (candidate.Issue.ParentNumber is int parentNumber
                && itemIdsByIssue.TryGetValue(parentNumber, out var parentId)
                && !string.Equals(parentId, candidate.Id, StringComparison.Ordinal))
            {
                candidate.Parent = parentId;
            }
        }
    }

    private static List<IntakeCandidate> OrderOpenCandidates(IEnumerable<IntakeCandidate> candidates)
    {
        var remaining = candidates.ToDictionary(candidate => candidate.Id, StringComparer.Ordinal);
        List<IntakeCandidate> ordered = [];
        while (remaining.Count > 0)
        {
            var next = remaining.Values
                .Where(candidate => candidate.BlockedBy.All(blockerId => !remaining.ContainsKey(blockerId)))
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.Id, StringComparer.Ordinal)
                .FirstOrDefault() ?? throw new InvalidOperationException("GitHub reconciliation blocker edges contain a cycle.");
            remaining.Remove(next.Id);
            ordered.Add(next);
        }

        return ordered;
    }

    private void SetOrders(IEnumerable<IntakeCandidate> orderedOpenCandidates, IReadOnlyCollection<RoadmapItemSnapshot> closedItemTransitions)
    {
        var transitionedItemIds = new HashSet<string>(closedItemTransitions.Select(item => item.Id), StringComparer.Ordinal);
        var nextOrder = _project.Items
            .Where(item => item.IsTriaged && !transitionedItemIds.Contains(item.Id))
            .Select(item => item.Order ?? 0)
            .DefaultIfEmpty()
            .Max();
        foreach (var candidate in orderedOpenCandidates)
        {
            candidate.Order = ++nextOrder;
        }
    }

    private IntakePlan CreateWritePlan(
        GitHubRoadmapReconciliation reconciliation,
        Dictionary<int, GitHubRoadmapIntakeIssue> openIssuesByNumber,
        Dictionary<int, string> itemIdsByIssue,
        List<IntakeCandidate> candidates,
        Dictionary<string, RoadmapItemSnapshot> existingItemsById,
        List<IntakeCandidate> orderedOpenCandidates,
        List<RoadmapItemSnapshot> closedItemTransitions)
    {
        Dictionary<string, string> writes = new(StringComparer.Ordinal);
        foreach (var candidate in candidates)
        {
            var itemPath = Path.Combine(_project.RootPath, RepoConfigPaths.Items, $"{candidate.Id}-github-{candidate.Issue.Number}.json");
            writes.Add(itemPath, CreateItemContent(candidate));
        }

        var existingRelationshipChanges = CreateExistingRelationshipChanges(
            reconciliation,
            openIssuesByNumber,
            itemIdsByIssue,
            candidates,
            existingItemsById,
            writes,
            closedItemTransitions);
        var transitionedItemIds = new HashSet<string>(closedItemTransitions.Select(item => item.Id), StringComparer.Ordinal);
        var orderUpdated = orderedOpenCandidates.Count > 0 || closedItemTransitions.Count > 0;
        if (orderUpdated)
        {
            var orderedItemIds = _project.Items
                .Where(item => item.IsTriaged && !transitionedItemIds.Contains(item.Id))
                .OrderByPriority()
                .Select(item => item.Id)
                .Concat(orderedOpenCandidates.Select(item => item.Id))
                .ToArray();
            writes.Add(Path.Combine(_project.RootPath, RepoConfigPaths.Order), CreateOrderContent(orderedItemIds));
        }

        return new IntakePlan(
            writes.OrderBy(write => write.Key, StringComparer.Ordinal).Select(write => new PlannedWrite(write.Key, write.Value)).ToArray(),
            candidates.Count(candidate => candidate.IsOpen),
            candidates.Count(candidate => !candidate.IsOpen),
            closedItemTransitions.Count,
            existingRelationshipChanges,
            orderUpdated);
    }

    private int CreateExistingRelationshipChanges(
        GitHubRoadmapReconciliation reconciliation,
        Dictionary<int, GitHubRoadmapIntakeIssue> openIssuesByNumber,
        Dictionary<int, string> itemIdsByIssue,
        List<IntakeCandidate> candidates,
        Dictionary<string, RoadmapItemSnapshot> existingItemsById,
        Dictionary<string, string> writes,
        IReadOnlyCollection<RoadmapItemSnapshot> closedItemTransitions)
    {
        var candidateIds = new HashSet<string>(candidates.Select(candidate => candidate.Id), StringComparer.Ordinal);
        var transitioningItemIds = new HashSet<string>(closedItemTransitions.Select(item => item.Id), StringComparer.Ordinal);
        Dictionary<string, string?> parentByItemId = new(StringComparer.Ordinal);
        foreach (var issue in openIssuesByNumber.Values)
        {
            if (itemIdsByIssue.TryGetValue(issue.Number, out var itemId))
            {
                parentByItemId[itemId] = issue.ParentNumber is int parentNumber && itemIdsByIssue.TryGetValue(parentNumber, out var parentId)
                    ? parentId
                    : null;
            }
        }

        Dictionary<string, List<string>> blocksByItemId = new(StringComparer.Ordinal);
        Dictionary<string, List<string>> blockedByItemId = new(StringComparer.Ordinal);
        foreach (var edge in reconciliation.BlockerEdges)
        {
            if (!itemIdsByIssue.TryGetValue(edge.Blocker, out var blockerId)
                || !itemIdsByIssue.TryGetValue(edge.Blocked, out var blockedId))
            {
                throw new InvalidOperationException("GitHub reconciliation blocker edge cannot be mapped to roadmap items.");
            }

            AddLink(blocksByItemId, blockerId, blockedId);
            AddLink(blockedByItemId, blockedId, blockerId);
        }

        var changes = 0;
        foreach (var item in existingItemsById.Values)
        {
            var desiredBlocks = blocksByItemId.TryGetValue(item.Id, out var blocks) ? blocks.Order(StringComparer.Ordinal).ToArray() : [];
            var desiredBlockedBy = blockedByItemId.TryGetValue(item.Id, out var blockers) ? blockers.Order(StringComparer.Ordinal).ToArray() : [];
            var transitionsToClosedSupport = transitioningItemIds.Contains(item.Id);
            var replacesGitHubRelationships = item.GitHubIssue is int;
            string? desiredParent = null;
            var updatesParent = IsIntakeGenerated(item) && parentByItemId.TryGetValue(item.Id, out desiredParent);
            if ((!replacesGitHubRelationships && desiredBlocks.Length == 0 && desiredBlockedBy.Length == 0 && !transitionsToClosedSupport && !updatesParent)
                || candidateIds.Contains(item.Id))
            {
                continue;
            }

            var itemPath = Path.Combine(_project.RootPath, item.Path);
            var updatedContent = CreateUpdatedItemContent(
                itemPath,
                item.Path,
                desiredBlocks,
                desiredBlockedBy,
                replacesGitHubRelationships,
                transitionsToClosedSupport,
                updatesParent,
                desiredParent,
                out var relationshipsChanged);
            if (updatedContent is null)
            {
                continue;
            }

            writes.Add(itemPath, updatedContent);
            if (relationshipsChanged)
            {
                changes++;
            }
        }

        return changes;
    }

    private static bool IsIntakeGenerated(RoadmapItemSnapshot item) => item.GitHubIssue is int issueNumber
        && string.Equals(Path.GetFileName(item.Path), $"{item.Id}-github-{issueNumber}.json", StringComparison.Ordinal);

    private static void AddLink(Dictionary<string, List<string>> linksByItemId, string itemId, string linkId)
    {
        if (!linksByItemId.TryGetValue(itemId, out var links))
        {
            links = [];
            linksByItemId.Add(itemId, links);
        }

        if (!links.Contains(linkId, StringComparer.Ordinal))
        {
            links.Add(linkId);
        }
    }

    private static string? CreateUpdatedItemContent(
        string itemPath,
        string relativeItemPath,
        string[] desiredBlocks,
        string[] desiredBlockedBy,
        bool replacesGitHubRelationships,
        bool transitionsToClosedSupport,
        bool updatesParent,
        string? desiredParent,
        out bool relationshipsChanged)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(itemPath));
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException($"Roadmap item root must be a JSON object: {relativeItemPath}.");
        }

        var existingBlocks = ReadStringArray(document.RootElement, "blocks");
        var existingBlockedBy = ReadStringArray(document.RootElement, "blockedBy");
        var blocks = replacesGitHubRelationships ? desiredBlocks : AddMissingLinks(existingBlocks, desiredBlocks);
        var blockedBy = replacesGitHubRelationships ? desiredBlockedBy : AddMissingLinks(existingBlockedBy, desiredBlockedBy);
        var linksChanged = !existingBlocks.SequenceEqual(blocks, StringComparer.Ordinal)
            || !existingBlockedBy.SequenceEqual(blockedBy, StringComparer.Ordinal);
        var currentParent = ReadNullableString(document.RootElement, "parent");
        var parentChanged = updatesParent && !string.Equals(currentParent, desiredParent, StringComparison.Ordinal);
        relationshipsChanged = linksChanged || parentChanged;
        if (!relationshipsChanged && !transitionsToClosedSupport)
        {
            return null;
        }

        var wroteStatus = false;
        var hasParentProperty = document.RootElement.TryGetProperty("parent", out _);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (transitionsToClosedSupport && string.Equals(property.Name, "status", StringComparison.Ordinal))
                {
                    writer.WriteString("status", ClosedStatus);
                    writer.WriteString("triage", "untriaged");
                    wroteStatus = true;
                }
                else if (transitionsToClosedSupport && property.Name is "triage" or "order" or "scoring")
                {
                    continue;
                }
                else if (updatesParent && string.Equals(property.Name, "parent", StringComparison.Ordinal))
                {
                    if (desiredParent is not null)
                    {
                        writer.WriteString("parent", desiredParent);
                    }
                }
                else if (string.Equals(property.Name, "blocks", StringComparison.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteStringArray(writer, blocks);
                }
                else if (string.Equals(property.Name, "blockedBy", StringComparison.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteStringArray(writer, blockedBy);
                }
                else
                {
                    writer.WritePropertyName(property.Name);
                    property.Value.WriteTo(writer);
                    if (updatesParent && !hasParentProperty && desiredParent is not null && string.Equals(property.Name, "id", StringComparison.Ordinal))
                    {
                        writer.WriteString("parent", desiredParent);
                    }
                }
            }

            writer.WriteEndObject();
        }

        if (transitionsToClosedSupport && !wroteStatus)
        {
            throw new InvalidOperationException($"Roadmap item must define status before a closed support transition: {relativeItemPath}.");
        }

        return Encoding.UTF8.GetString(stream.ToArray()) + Environment.NewLine;
    }

    private static string[] AddMissingLinks(string[] existingLinks, string[] desiredLinks)
    {
        var existing = new HashSet<string>(existingLinks, StringComparer.Ordinal);
        var additions = desiredLinks.Where(existing.Add).ToArray();
        return additions.Length > 0 ? existingLinks.Concat(additions).ToArray() : existingLinks;
    }

    private static string? ReadNullableString(JsonElement root, string propertyName) => root.TryGetProperty(propertyName, out var property)
        && property.ValueKind == JsonValueKind.String
        ? property.GetString()
        : null;

    private static string[] ReadStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"Roadmap item {propertyName} must be an array.");
        }

        var values = property.EnumerateArray().Select(element => element.ValueKind == JsonValueKind.String ? element.GetString() : null).ToArray();
        if (values.Any(string.IsNullOrWhiteSpace) || values.Distinct(StringComparer.Ordinal).Count() != values.Length)
        {
            throw new InvalidOperationException($"Roadmap item {propertyName} must contain unique non-empty strings.");
        }

        return values.Select(value => value ?? string.Empty).ToArray();
    }

    private static string CreateItemContent(IntakeCandidate candidate)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("$schema", "../schema/roadmap-item.schema.json");
            writer.WriteString("id", candidate.Id);
            if (!string.IsNullOrWhiteSpace(candidate.Parent))
            {
                writer.WriteString("parent", candidate.Parent);
            }

            writer.WriteString("title", candidate.Issue.Title);
            writer.WriteString("type", candidate.Type);
            writer.WriteString("status", candidate.IsOpen ? OpenStatus : ClosedStatus);
            if (candidate.IsOpen)
            {
                writer.WriteNumber("order", candidate.Order ?? throw new InvalidOperationException("Imported open roadmap items require an order."));
            }
            else
            {
                writer.WriteString("triage", "untriaged");
            }

            writer.WriteString("theme", DefaultTheme);
            writer.WriteString("outcome", $"GitHub issue #{candidate.Issue.Number} is canonically represented from the reconciliation manifest.");
            if (candidate.IsOpen)
            {
                writer.WritePropertyName("scoring");
                writer.WriteStartObject();
                writer.WriteNumber("reach", ApprovedReach);
                writer.WriteNumber("impact", candidate.Impact);
                writer.WriteNumber("confidence", ApprovedConfidence);
                writer.WriteNumber("effort", candidate.Effort);
                writer.WriteEndObject();
            }

            writer.WritePropertyName("blockedBy");
            WriteStringArray(writer, candidate.BlockedBy.Order(StringComparer.Ordinal));
            writer.WritePropertyName("blocks");
            WriteStringArray(writer, candidate.Blocks.Order(StringComparer.Ordinal));
            writer.WritePropertyName("dependencies");
            WriteStringArray(writer, []);
            writer.WritePropertyName("tags");
            WriteStringArray(writer, []);
            writer.WritePropertyName("labels");
            WriteStringArray(writer, candidate.Issue.Labels);
            writer.WritePropertyName("sources");
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WriteString("kind", "github-issue");
            writer.WriteString("reference", $"#{candidate.Issue.Number}");
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WritePropertyName("integrations");
            writer.WriteStartObject();
            writer.WritePropertyName("github");
            writer.WriteStartObject();
            writer.WriteNumber("issue", candidate.Issue.Number);
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray()) + Environment.NewLine;
    }

    private static string CreateOrderContent(IEnumerable<string> orderedItemIds)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("ordering", "lower order values are higher priority");
            writer.WritePropertyName("items");
            WriteStringArray(writer, orderedItemIds);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray()) + Environment.NewLine;
    }

    private static void WriteStringArray(Utf8JsonWriter writer, IEnumerable<string> values)
    {
        writer.WriteStartArray();
        foreach (var value in values)
        {
            writer.WriteStringValue(value);
        }

        writer.WriteEndArray();
    }

    private static string FindAvailableRoadmapId(ref int roadmapNumber, HashSet<string> usedItemIds)
    {
        while (true)
        {
            var candidate = FormatRoadmapId(roadmapNumber++);
            if (usedItemIds.Add(candidate))
            {
                return candidate;
            }
        }
    }

    private static string FormatRoadmapId(int roadmapNumber) => $"RM-{roadmapNumber:D3}";

    private sealed class IntakeCandidate(
        GitHubRoadmapIntakeIssue issue,
        string id,
        bool isOpen,
        string type,
        decimal effort,
        decimal impact)
    {
        private readonly HashSet<string> _blockedBy = new(StringComparer.Ordinal);
        private readonly HashSet<string> _blocks = new(StringComparer.Ordinal);

        public IReadOnlyCollection<string> BlockedBy => _blockedBy;

        public IReadOnlyCollection<string> Blocks => _blocks;

        public decimal Effort { get; } = effort;

        public string Id { get; } = id;

        public decimal Impact { get; } = impact;

        public GitHubRoadmapIntakeIssue Issue { get; } = issue;

        public bool IsOpen { get; } = isOpen;

        public int? Order { get; set; }

        public string? Parent { get; set; }

        public decimal Score => ApprovedReach * Impact * ApprovedConfidence / Effort;

        public string Type { get; } = type;

        public void AddBlocker(string blockerId) => _blockedBy.Add(blockerId);

        public void AddBlockedItem(string blockedItemId) => _blocks.Add(blockedItemId);
    }

    private sealed class IntakePlan(
        IReadOnlyList<PlannedWrite> writes,
        int openItemsToCreate,
        int closedSupportItemsToCreate,
        int closedItemsToTransition,
        int existingRelationshipChanges,
        bool orderUpdated)
    {
        public void Apply()
        {
            foreach (var write in writes)
            {
                WriteAtomically(write.Path, write.Content);
            }
        }

        public List<string> Messages(bool dryRun)
        {
            if (writes.Count == 0)
            {
                return [dryRun
                    ? "dry-run: roadmap already matches the reconciliation manifest."
                    : "intake: roadmap already matches the reconciliation manifest."];
            }

            var prefix = dryRun ? "dry-run: GitHub intake would" : "intake:";
            List<string> messages = [];
            if (openItemsToCreate > 0)
            {
                messages.Add(dryRun
                    ? $"{prefix} add {openItemsToCreate} open roadmap items."
                    : $"{prefix} added {openItemsToCreate} open roadmap items.");
            }

            if (closedSupportItemsToCreate > 0)
            {
                messages.Add(dryRun
                    ? $"{prefix} add {closedSupportItemsToCreate} closed support roadmap items."
                    : $"{prefix} added {closedSupportItemsToCreate} closed support roadmap items.");
            }

            if (closedItemsToTransition > 0)
            {
                var itemNoun = closedItemsToTransition == 1 ? "item" : "items";
                messages.Add(dryRun
                    ? $"{prefix} transition {closedItemsToTransition} imported roadmap {itemNoun} to closed support."
                    : $"{prefix} transitioned {closedItemsToTransition} imported roadmap {itemNoun} to closed support.");
            }

            if (existingRelationshipChanges > 0)
            {
                messages.Add(dryRun
                    ? $"{prefix} update {existingRelationshipChanges} existing roadmap items with exact GitHub relationships."
                    : $"{prefix} updated {existingRelationshipChanges} existing roadmap items with exact GitHub relationships.");
            }

            if (orderUpdated)
            {
                messages.Add(dryRun
                    ? $"{prefix} update roadmap/order.json."
                    : $"{prefix} updated roadmap/order.json.");
            }

            return messages;
        }

        private static void WriteAtomically(string path, string content)
        {
            var temporaryPath = path + ".tmp";
            try
            {
                File.WriteAllText(temporaryPath, content);
                File.Move(temporaryPath, path, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
    }

    private sealed record PlannedWrite(string Path, string Content);
}
