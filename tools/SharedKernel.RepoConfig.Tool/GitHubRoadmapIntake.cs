using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SharedKernel.RepoConfig.Tool;

internal sealed class GitHubRoadmapIntake
{
    private readonly HttpClient? _httpClient;
    private RoadmapProject _project;

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
        var inputSnapshot = RoadmapWriteInputSnapshot.Capture(_project.RootPath);
        _project = RoadmapProject.Load(_project.RootPath);
        inputSnapshot.Verify();
        var repository = GetGitHubRepository();
        var reconciliation = GitHubRoadmapReconciliation.Load(_project.RootPath);
        if (string.IsNullOrWhiteSpace(reconciliation.SnapshotDigest))
        {
            throw new InvalidOperationException("GitHub intake requires snapshotDigest; run reconcile github --apply first.");
        }

        if (!string.Equals(reconciliation.Repository, repository, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("GitHub reconciliation manifest repository does not match roadmap/config.json.");
        }

        var (theme, openStatus, closedStatus) = ReadIntakeSettings();

        using var ownedHttpClient = _httpClient is null ? GitHubHttpClient.Create("intake") : null;
        var httpClient = (_httpClient ?? ownedHttpClient) ?? throw new InvalidOperationException("GitHub intake could not create an HTTP client.");
        var snapshot = await new GitHubRoadmapReconcileClient(httpClient)
            .ReadSnapshot(repository, reconciliation.GetRequiredIssueNumbers(_project), cancellationToken)
            .ConfigureAwait(false);
        var snapshotDigest = GitHubIssueSnapshotDigest.ComputeForOpenAndRequiredIssues(
            snapshot.Issues,
            reconciliation.GetRequiredIssueNumbers(_project),
            repository);
        if (!string.Equals(reconciliation.SnapshotDigest, snapshotDigest, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("GitHub issue snapshot metadata does not match the reconciliation manifest.");
        }

        var snapshotIssues = snapshot.Issues.ToDictionary(issue => issue.Number);
        var snapshotBlockerEdges = GitHubRoadmapReconciler.DeriveBlockerEdges(snapshot.Issues, snapshotIssues, repository);
        var manifestBlockerEdges = reconciliation.BlockerEdges
            .OrderBy(edge => edge.Blocker)
            .ThenBy(edge => edge.Blocked)
            .ThenBy(edge => edge.BlockerState, StringComparer.Ordinal)
            .ThenBy(edge => edge.BlockedState, StringComparer.Ordinal)
            .ToArray();
        if (!manifestBlockerEdges.SequenceEqual(snapshotBlockerEdges))
        {
            throw new InvalidOperationException("GitHub reconciliation blocker edges do not match the verified snapshot.");
        }

        var openSnapshotIssues = snapshot.Issues
            .Where(issue => string.Equals(issue.State, "OPEN", StringComparison.Ordinal))
            .ToArray();
        var openSnapshotIssuesByNumber = ValidateOpenSnapshot(reconciliation, openSnapshotIssues);
        var closedSnapshotIssuesByNumber = ReadClosedEndpoints(reconciliation, snapshotIssues);
        return BuildPlan(reconciliation, openSnapshotIssuesByNumber, closedSnapshotIssuesByNumber, theme, openStatus, closedStatus, inputSnapshot);
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

    private (string Theme, string OpenStatus, string ClosedStatus) ReadIntakeSettings()
    {
        if (string.IsNullOrWhiteSpace(_project.GitHubIntakeTheme)
            || string.IsNullOrWhiteSpace(_project.GitHubIntakeOpenStatus)
            || string.IsNullOrWhiteSpace(_project.GitHubIntakeClosedStatus))
        {
            throw new InvalidOperationException("roadmap/config.json must define integrations.github.intake before GitHub intake.");
        }

        var theme = _project.GitHubIntakeTheme;
        var openStatus = _project.GitHubIntakeOpenStatus;
        var closedStatus = _project.GitHubIntakeClosedStatus;
        var themeExists = Directory.EnumerateFiles(Path.Combine(_project.RootPath, RepoConfigPaths.Themes), "*.json", SearchOption.TopDirectoryOnly)
            .Any(path => ThemeHasId(path, theme));
        if (!themeExists)
        {
            throw new InvalidOperationException($"integrations.github.intake.theme references an unknown roadmap theme: {theme}.");
        }

        return (theme, openStatus, closedStatus);
    }

    private static bool ThemeHasId(string path, string expectedId)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.TryGetProperty("id", out var id)
            && id.ValueKind == JsonValueKind.String
            && string.Equals(id.GetString(), expectedId, StringComparison.Ordinal);
    }

    private static Dictionary<int, GitHubRoadmapReconcileIssue> ValidateOpenSnapshot(
        GitHubRoadmapReconciliation reconciliation,
        GitHubRoadmapReconcileIssue[] openIssues)
    {
        if (openIssues.Any(issue => !string.Equals(issue.State, "OPEN", StringComparison.Ordinal))
            || openIssues.Select(issue => issue.Number).Distinct().Count() != openIssues.Length)
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

        return issuesByNumber;
    }

    private static Dictionary<int, GitHubRoadmapReconcileIssue> ReadClosedEndpoints(
        GitHubRoadmapReconciliation reconciliation,
        Dictionary<int, GitHubRoadmapReconcileIssue> issuesByNumber)
    {
        Dictionary<int, GitHubRoadmapReconcileIssue> closedIssues = [];
        foreach (var (issueNumber, expectedState) in reconciliation.ClosedEndpointStates)
        {
            if (!issuesByNumber.TryGetValue(issueNumber, out var issue)
                || !string.Equals(issue.State, expectedState, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("GitHub issue snapshot does not match the reconciliation manifest.");
            }

            closedIssues.Add(issueNumber, issue);
        }

        return closedIssues;
    }

    private static string FormatIssueNumbers(IEnumerable<int> issueNumbers)
    {
        var values = issueNumbers.Select(issueNumber => $"#{issueNumber}").ToArray();
        return values.Length == 0 ? "none" : string.Join(", ", values);
    }

    private IntakePlan BuildPlan(
        GitHubRoadmapReconciliation reconciliation,
        Dictionary<int, GitHubRoadmapReconcileIssue> openIssuesByNumber,
        Dictionary<int, GitHubRoadmapReconcileIssue> closedIssuesByNumber,
        string theme,
        string openStatus,
        string closedStatus,
        RoadmapWriteInputSnapshot inputSnapshot)
    {
        var existingItemsById = _project.Items.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var existingItemsByIssue = _project.Items
            .Where(item => item.GitHubIssue is int)
            .ToDictionary(item => item.GitHubIssue.GetValueOrDefault());
        foreach (var issueNumber in openIssuesByNumber.Keys)
        {
            if (existingItemsByIssue.TryGetValue(issueNumber, out var existingItem) && _project.IsClosed(existingItem))
            {
                throw new InvalidOperationException($"GitHub open issue #{issueNumber} maps to closed roadmap item {existingItem.Id}; review the reopening before intake.");
            }
        }

        ValidateDirectCanonicalPrimaries(reconciliation, existingItemsByIssue);
        ValidateCanonicalChildren(reconciliation, existingItemsByIssue);
        ValidateClosedItemTransitions(reconciliation, existingItemsByIssue);

        HashSet<string> usedItemIds = new(existingItemsById.Keys, StringComparer.Ordinal);
        Dictionary<int, string> itemIdsByIssue = existingItemsByIssue.ToDictionary(entry => entry.Key, entry => entry.Value.Id);
        List<IntakeCandidate> candidates = [];
        List<RoadmapItemSnapshot> closedItemTransitions = [];
        var roadmapNumber = GetNextRoadmapNumber(usedItemIds, _project.ItemIdPrefix, reconciliation.PriorityPolicy.FirstItemNumber);
        AllocateOpenItems(reconciliation, openIssuesByNumber, existingItemsByIssue, itemIdsByIssue, usedItemIds, candidates, ref roadmapNumber);
        AllocateClosedSupportItems(reconciliation, closedIssuesByNumber, existingItemsByIssue, itemIdsByIssue, usedItemIds, candidates, closedItemTransitions, ref roadmapNumber);
        AddExactBlockerLinks(reconciliation, itemIdsByIssue, candidates);
        AddParentLinks(candidates, itemIdsByIssue, reconciliation.Repository);
        var importedOrdering = CreateImportedOrdering(reconciliation, openIssuesByNumber, itemIdsByIssue, candidates, existingItemsById, closedItemTransitions);

        return CreateWritePlan(
            reconciliation,
            openIssuesByNumber,
            itemIdsByIssue,
            candidates,
            existingItemsById,
            importedOrdering,
            closedItemTransitions,
            theme,
            openStatus,
            closedStatus,
            inputSnapshot);
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

            if (!IsIntakeGenerated(item))
            {
                throw new InvalidOperationException($"GitHub closed item transition must reference an intake-generated item: #{issueNumber} -> {roadmapItemId}.");
            }
        }
    }

    private void AllocateOpenItems(
        GitHubRoadmapReconciliation reconciliation,
        Dictionary<int, GitHubRoadmapReconcileIssue> openIssuesByNumber,
        Dictionary<int, RoadmapItemSnapshot> existingItemsByIssue,
        Dictionary<int, string> itemIdsByIssue,
        HashSet<string> usedItemIds,
        List<IntakeCandidate> candidates,
        ref int roadmapNumber)
    {
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

            var roadmapId = FindAvailableRoadmapId(ref roadmapNumber, usedItemIds, _project.ItemIdPrefix);
            itemIdsByIssue.Add(issueNumber, roadmapId);
            candidates.Add(CreateCandidate(issue, roadmapId, isOpen: true, reconciliation.BlockerEdges, reconciliation.PriorityPolicy));
        }
    }

    private void AllocateClosedSupportItems(
        GitHubRoadmapReconciliation reconciliation,
        Dictionary<int, GitHubRoadmapReconcileIssue> closedIssuesByNumber,
        Dictionary<int, RoadmapItemSnapshot> existingItemsByIssue,
        Dictionary<int, string> itemIdsByIssue,
        HashSet<string> usedItemIds,
        List<IntakeCandidate> candidates,
        List<RoadmapItemSnapshot> closedItemTransitions,
        ref int roadmapNumber)
    {
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

            var roadmapId = FindAvailableRoadmapId(ref roadmapNumber, usedItemIds, _project.ItemIdPrefix);
            itemIdsByIssue.Add(issueNumber, roadmapId);
            candidates.Add(CreateCandidate(issue, roadmapId, isOpen: false, reconciliation.BlockerEdges, reconciliation.PriorityPolicy));
        }
    }

    private static IntakeCandidate CreateCandidate(
        GitHubRoadmapReconcileIssue issue,
        string roadmapId,
        bool isOpen,
        IReadOnlyCollection<GitHubRoadmapBlockerEdge> blockerEdges,
        GitHubRoadmapPriorityPolicy priorityPolicy)
    {
        var classification = Classify(issue.Labels, priorityPolicy);
        var scoring = CreateScoring(issue.Number, isOpen, blockerEdges, priorityPolicy, classification.Effort);
        return new IntakeCandidate(issue, roadmapId, isOpen, classification.Type, scoring);
    }

    private static IntakeScoring CreateScoring(
        int issueNumber,
        bool isOpen,
        IReadOnlyCollection<GitHubRoadmapBlockerEdge> blockerEdges,
        GitHubRoadmapPriorityPolicy priorityPolicy,
        decimal effort)
    {
        var impact = isOpen
            ? Math.Min(
                priorityPolicy.ImpactCap,
                1m + blockerEdges.Count(edge => edge.Blocked == issueNumber && string.Equals(edge.BlockerState, "OPEN", StringComparison.Ordinal)))
            : 0m;
        return new IntakeScoring(priorityPolicy.Reach, impact, priorityPolicy.Confidence, effort);
    }

    private static (string Type, decimal Effort) Classify(
        IReadOnlyList<string> labels,
        GitHubRoadmapPriorityPolicy priorityPolicy)
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

        var (type, effortLabel) = classifications.SingleOrDefault() switch
        {
            "epic" => ("epic", "type: epic"),
            "feature" => ("feature", "type: feature"),
            "enabler" => ("enabler", "type: enabler"),
            "documentation" => ("documentation", "type: docs"),
            "chore" => ("issue", "type: chore"),
            _ => ("issue", null)
        };
        if (effortLabel is null)
        {
            return (type, priorityPolicy.DefaultEffort);
        }

        return priorityPolicy.EffortByLabel.TryGetValue(effortLabel, out var effort)
            ? (type, effort)
            : throw new InvalidOperationException($"GitHub reconciliation manifest does not define effort for {effortLabel}.");
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

    private static void AddParentLinks(
        List<IntakeCandidate> candidates,
        Dictionary<int, string> itemIdsByIssue,
        string repository)
    {
        foreach (var candidate in candidates)
        {
            if (candidate.Issue.Parent is { } parent
                && IsLocalOpenParent(parent, repository)
                && itemIdsByIssue.TryGetValue(parent.Number, out var parentId)
                && !string.Equals(parentId, candidate.Id, StringComparison.Ordinal))
            {
                candidate.Parent = parentId;
            }
        }
    }

    private static bool IsLocalOpenParent(GitHubRoadmapReconcileRelation parent, string repository) =>
        string.Equals(parent.Repository, repository, StringComparison.OrdinalIgnoreCase)
        && string.Equals(parent.State, "OPEN", StringComparison.Ordinal);

    private ImportedOrdering CreateImportedOrdering(
        GitHubRoadmapReconciliation reconciliation,
        Dictionary<int, GitHubRoadmapReconcileIssue> openIssuesByNumber,
        Dictionary<int, string> itemIdsByIssue,
        List<IntakeCandidate> candidates,
        Dictionary<string, RoadmapItemSnapshot> existingItemsById,
        IReadOnlyCollection<RoadmapItemSnapshot> closedItemTransitions)
    {
        var transitioningItemIds = new HashSet<string>(closedItemTransitions.Select(item => item.Id), StringComparer.Ordinal);
        Dictionary<string, List<string>> blockerIdsByItemId = new(StringComparer.Ordinal);
        foreach (var edge in reconciliation.BlockerEdges)
        {
            if (!itemIdsByIssue.TryGetValue(edge.Blocker, out var blockerId)
                || !itemIdsByIssue.TryGetValue(edge.Blocked, out var blockedId))
            {
                throw new InvalidOperationException("GitHub reconciliation blocker edge cannot be mapped to roadmap items.");
            }

            AddLink(blockerIdsByItemId, blockedId, blockerId);
        }

        Dictionary<string, ImportedOrderNode> remaining = new(StringComparer.Ordinal);
        Dictionary<string, IntakeScoring> scoringByItemId = new(StringComparer.Ordinal);
        foreach (var item in existingItemsById.Values.Where(item => item.IsTriaged && IsIntakeGenerated(item) && !transitioningItemIds.Contains(item.Id) && !_project.IsClosed(item)))
        {
            if (item.GitHubIssue is not int issueNumber || !openIssuesByNumber.TryGetValue(issueNumber, out var issue))
            {
                throw new InvalidOperationException($"Imported roadmap item requires current GitHub metadata before ordering: {item.Id}.");
            }

            var classification = Classify(issue.Labels, reconciliation.PriorityPolicy);
            var scoring = CreateScoring(issueNumber, isOpen: true, reconciliation.BlockerEdges, reconciliation.PriorityPolicy, classification.Effort);
            var blockers = blockerIdsByItemId.TryGetValue(item.Id, out var blockerIds) ? blockerIds : [];
            remaining.Add(item.Id, new ImportedOrderNode(item.Id, scoring.Score, blockers));
            scoringByItemId.Add(item.Id, scoring);
        }

        foreach (var candidate in candidates.Where(candidate => candidate.IsOpen))
        {
            var blockers = blockerIdsByItemId.TryGetValue(candidate.Id, out var blockerIds) ? blockerIds : [];
            remaining.Add(candidate.Id, new ImportedOrderNode(candidate.Id, candidate.Score, blockers));
            scoringByItemId.Add(candidate.Id, candidate.Scoring);
        }

        List<string> orderedItemIds = [];
        while (remaining.Count > 0)
        {
            var next = remaining.Values
                .Where(node => node.BlockedBy.All(blockerId => !remaining.ContainsKey(blockerId)))
                .OrderByDescending(node => node.Score)
                .ThenBy(node => node.Id, StringComparer.Ordinal)
                .FirstOrDefault() ?? throw new InvalidOperationException("GitHub reconciliation blocker edges contain a cycle.");
            remaining.Remove(next.Id);
            orderedItemIds.Add(next.Id);
        }

        var nextOrder = existingItemsById.Values
            .Where(item => item.IsTriaged && !IsIntakeGenerated(item) && !transitioningItemIds.Contains(item.Id))
            .Select(item => item.Order ?? 0)
            .DefaultIfEmpty()
            .Max();
        if (nextOrder > int.MaxValue - orderedItemIds.Count)
        {
            throw new InvalidOperationException("Roadmap order values are exhausted.");
        }

        Dictionary<string, int> ordersByItemId = new(StringComparer.Ordinal);
        foreach (var itemId in orderedItemIds)
        {
            ordersByItemId.Add(itemId, ++nextOrder);
        }

        foreach (var candidate in candidates.Where(candidate => candidate.IsOpen))
        {
            candidate.Order = ordersByItemId[candidate.Id];
        }

        var existingOrdersChanged = ordersByItemId.Any(entry => existingItemsById.TryGetValue(entry.Key, out var item) && item.Order != entry.Value);
        return new ImportedOrdering(orderedItemIds, ordersByItemId, scoringByItemId, existingOrdersChanged);
    }

    private IntakePlan CreateWritePlan(
        GitHubRoadmapReconciliation reconciliation,
        Dictionary<int, GitHubRoadmapReconcileIssue> openIssuesByNumber,
        Dictionary<int, string> itemIdsByIssue,
        List<IntakeCandidate> candidates,
        Dictionary<string, RoadmapItemSnapshot> existingItemsById,
        ImportedOrdering importedOrdering,
        List<RoadmapItemSnapshot> closedItemTransitions,
        string theme,
        string openStatus,
        string closedStatus,
        RoadmapWriteInputSnapshot inputSnapshot)
    {
        Dictionary<string, string> writes = new(StringComparer.Ordinal);
        foreach (var candidate in candidates)
        {
            var itemPath = Path.Combine(_project.RootPath, RepoConfigPaths.Items, $"{candidate.Id}-github-{candidate.Issue.Number}.json");
            if (inputSnapshot.GetExpectedContent(itemPath) is not null)
            {
                throw new InvalidOperationException($"Generated roadmap item path already belongs to another item: {itemPath}.");
            }

            writes.Add(itemPath, CreateItemContent(candidate, theme, openStatus, closedStatus));
        }

        var existingItemChanges = CreateExistingRelationshipChanges(
            reconciliation,
            openIssuesByNumber,
            itemIdsByIssue,
            candidates,
            existingItemsById,
            writes,
            closedItemTransitions,
            importedOrdering.OrdersByItemId,
            importedOrdering.ScoringByItemId,
            theme,
            openStatus,
            closedStatus);
        var transitionedItemIds = new HashSet<string>(closedItemTransitions.Select(item => item.Id), StringComparer.Ordinal);
        var orderUpdated = candidates.Any(candidate => candidate.IsOpen) || closedItemTransitions.Count > 0 || importedOrdering.ExistingOrdersChanged;
        if (orderUpdated)
        {
            var orderedItemIds = _project.Items
                .Where(item => item.IsTriaged && !IsIntakeGenerated(item) && !transitionedItemIds.Contains(item.Id))
                .OrderByPriority()
                .Select(item => item.Id)
                .Concat(importedOrdering.OrderedItemIds)
                .ToArray();
            writes.Add(Path.Combine(_project.RootPath, RepoConfigPaths.Order), CreateOrderContent(orderedItemIds));
        }

        return new IntakePlan(
            _project.RootPath,
            writes.OrderBy(write => write.Key, StringComparer.Ordinal)
                .Select(write => new AtomicFileWrite(
                    write.Key,
                    write.Value,
                    inputSnapshot.GetExpectedContent(write.Key)))
                .ToArray(),
            candidates.Count(candidate => candidate.IsOpen),
            candidates.Count(candidate => !candidate.IsOpen),
            closedItemTransitions.Count,
            existingItemChanges,
            orderUpdated,
            inputSnapshot);
    }

    private int CreateExistingRelationshipChanges(
        GitHubRoadmapReconciliation reconciliation,
        Dictionary<int, GitHubRoadmapReconcileIssue> openIssuesByNumber,
        Dictionary<int, string> itemIdsByIssue,
        List<IntakeCandidate> candidates,
        Dictionary<string, RoadmapItemSnapshot> existingItemsById,
        Dictionary<string, string> writes,
        IReadOnlyCollection<RoadmapItemSnapshot> closedItemTransitions,
        Dictionary<string, int> importedOrdersByItemId,
        Dictionary<string, IntakeScoring> importedScoringByItemId,
        string theme,
        string openStatus,
        string closedStatus)
    {
        var candidateIds = new HashSet<string>(candidates.Select(candidate => candidate.Id), StringComparer.Ordinal);
        var transitioningItemIds = new HashSet<string>(closedItemTransitions.Select(item => item.Id), StringComparer.Ordinal);
        Dictionary<string, string?> parentByItemId = new(StringComparer.Ordinal);
        foreach (var issue in openIssuesByNumber.Values)
        {
            if (itemIdsByIssue.TryGetValue(issue.Number, out var itemId))
            {
                parentByItemId[itemId] = issue.Parent is { } parent
                    && IsLocalOpenParent(parent, reconciliation.Repository)
                    && itemIdsByIssue.TryGetValue(parent.Number, out var parentId)
                    ? parentId
                    : null;
            }
        }

        Dictionary<string, IntakeMetadata> metadataByItemId = new(StringComparer.Ordinal);
        foreach (var issue in openIssuesByNumber.Values)
        {
            if (!itemIdsByIssue.TryGetValue(issue.Number, out var itemId)
                || !existingItemsById.TryGetValue(itemId, out var item)
                || !IsIntakeGenerated(item))
            {
                continue;
            }

            var classification = Classify(issue.Labels, reconciliation.PriorityPolicy);
            metadataByItemId.Add(
                itemId,
                new IntakeMetadata(
                    issue.Title,
                    classification.Type,
                    openStatus,
                    theme,
                    issue.Labels.Order(StringComparer.Ordinal).ToArray()));
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
            var updatesOrder = importedOrdersByItemId.TryGetValue(item.Id, out var desiredOrder) && item.Order != desiredOrder;
            importedScoringByItemId.TryGetValue(item.Id, out var desiredScoring);
            var updatesScoring = desiredScoring is not null && !desiredScoring.Matches(item);
            metadataByItemId.TryGetValue(item.Id, out var desiredMetadata);
            var updatesMetadata = desiredMetadata is not null && !desiredMetadata.Matches(item);
            if ((!replacesGitHubRelationships && desiredBlocks.Length == 0 && desiredBlockedBy.Length == 0 && !transitionsToClosedSupport && !updatesParent && !updatesOrder && !updatesScoring && !updatesMetadata)
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
                updatesOrder,
                desiredOrder,
                updatesScoring,
                desiredScoring,
                updatesMetadata,
                desiredMetadata,
                closedStatus,
                out var itemChanged);
            if (updatedContent is null)
            {
                continue;
            }

            writes.Add(itemPath, updatedContent);
            if (itemChanged)
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
        bool updatesOrder,
        int desiredOrder,
        bool updatesScoring,
        IntakeScoring? desiredScoring,
        bool updatesMetadata,
        IntakeMetadata? desiredMetadata,
        string closedStatus,
        out bool itemChanged)
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
        var orderChanged = updatesOrder && ReadNullableInteger(document.RootElement, "order") != desiredOrder;
        itemChanged = linksChanged || parentChanged || orderChanged || updatesScoring || updatesMetadata;
        if (!itemChanged && !transitionsToClosedSupport)
        {
            return null;
        }

        var wroteStatus = false;
        var wroteOrder = false;
        var wroteScoring = false;
        var wroteTitle = false;
        var wroteType = false;
        var wroteOpenStatus = false;
        var wroteTheme = false;
        var wroteLabels = false;
        var hasParentProperty = document.RootElement.TryGetProperty("parent", out _);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (transitionsToClosedSupport && string.Equals(property.Name, "status", StringComparison.Ordinal))
                {
                    writer.WriteString("status", closedStatus);
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
                else if (updatesOrder && string.Equals(property.Name, "order", StringComparison.Ordinal))
                {
                    writer.WriteNumber("order", desiredOrder);
                    wroteOrder = true;
                }
                else if (updatesScoring && string.Equals(property.Name, "scoring", StringComparison.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteScoring(writer, desiredScoring ?? throw new InvalidOperationException("Imported roadmap scoring update is incomplete."));
                    wroteScoring = true;
                }
                else if (updatesMetadata && string.Equals(property.Name, "title", StringComparison.Ordinal))
                {
                    writer.WriteString(property.Name, desiredMetadata?.Title);
                    wroteTitle = true;
                }
                else if (updatesMetadata && string.Equals(property.Name, "type", StringComparison.Ordinal))
                {
                    writer.WriteString(property.Name, desiredMetadata?.Type);
                    wroteType = true;
                }
                else if (updatesMetadata && string.Equals(property.Name, "status", StringComparison.Ordinal))
                {
                    writer.WriteString(property.Name, desiredMetadata?.Status);
                    wroteOpenStatus = true;
                }
                else if (updatesMetadata && string.Equals(property.Name, "theme", StringComparison.Ordinal))
                {
                    writer.WriteString(property.Name, desiredMetadata?.Theme);
                    wroteTheme = true;
                }
                else if (updatesMetadata && string.Equals(property.Name, "labels", StringComparison.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteStringArray(writer, desiredMetadata?.Labels ?? []);
                    wroteLabels = true;
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

        if (updatesOrder && !wroteOrder)
        {
            throw new InvalidOperationException($"Imported roadmap item must define order before reprioritization: {relativeItemPath}.");
        }

        if (updatesScoring && !wroteScoring)
        {
            throw new InvalidOperationException($"Imported roadmap item must define scoring before reprioritization: {relativeItemPath}.");
        }

        if (updatesMetadata && (!wroteTitle || !wroteType || !wroteOpenStatus || !wroteTheme || !wroteLabels))
        {
            throw new InvalidOperationException($"Imported roadmap item must define snapshot metadata before synchronization: {relativeItemPath}.");
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

    private static int? ReadNullableInteger(JsonElement root, string propertyName) => root.TryGetProperty(propertyName, out var property)
        && property.ValueKind == JsonValueKind.Number
        && property.TryGetInt32(out var value)
        ? value
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

    private static string CreateItemContent(
        IntakeCandidate candidate,
        string theme,
        string openStatus,
        string closedStatus)
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
            writer.WriteString("status", candidate.IsOpen ? openStatus : closedStatus);
            if (candidate.IsOpen)
            {
                writer.WriteNumber("order", candidate.Order ?? throw new InvalidOperationException("Imported open roadmap items require an order."));
            }
            else
            {
                writer.WriteString("triage", "untriaged");
            }

            writer.WriteString("theme", theme);
            writer.WriteString("outcome", $"GitHub issue #{candidate.Issue.Number} is canonically represented from the reconciliation manifest.");
            if (candidate.IsOpen)
            {
                writer.WritePropertyName("scoring");
                WriteScoring(writer, candidate.Scoring);
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

    private static void WriteScoring(Utf8JsonWriter writer, IntakeScoring scoring)
    {
        writer.WriteStartObject();
        writer.WriteNumber("reach", scoring.Reach);
        writer.WriteNumber("impact", scoring.Impact);
        writer.WriteNumber("confidence", scoring.Confidence);
        writer.WriteNumber("effort", scoring.Effort);
        writer.WriteEndObject();
    }

    private static int GetNextRoadmapNumber(IEnumerable<string> itemIds, string itemIdPrefix, int firstItemNumber)
    {
        var idPrefix = itemIdPrefix + "-";
        var maximumExistingNumber = itemIds
            .Where(itemId => itemId.StartsWith(idPrefix, StringComparison.Ordinal))
            .Select(itemId => int.TryParse(itemId.AsSpan(idPrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : 0)
            .DefaultIfEmpty()
            .Max();
        if (maximumExistingNumber == int.MaxValue)
        {
            throw new InvalidOperationException($"Roadmap item IDs have exhausted the {itemIdPrefix} numeric range.");
        }

        return Math.Max(firstItemNumber, maximumExistingNumber + 1);
    }

    private static string FindAvailableRoadmapId(ref int roadmapNumber, HashSet<string> usedItemIds, string itemIdPrefix)
    {
        while (true)
        {
            var candidate = FormatRoadmapId(itemIdPrefix, roadmapNumber);
            if (usedItemIds.Add(candidate))
            {
                if (roadmapNumber < int.MaxValue)
                {
                    roadmapNumber++;
                }

                return candidate;
            }

            if (roadmapNumber == int.MaxValue)
            {
                throw new InvalidOperationException($"Roadmap item IDs have exhausted the {itemIdPrefix} numeric range.");
            }

            roadmapNumber++;
        }
    }

    private static string FormatRoadmapId(string itemIdPrefix, int roadmapNumber) => $"{itemIdPrefix}-{roadmapNumber:D3}";

    private sealed class IntakeCandidate(
        GitHubRoadmapReconcileIssue issue,
        string id,
        bool isOpen,
        string type,
        IntakeScoring scoring)
    {
        private readonly HashSet<string> _blockedBy = new(StringComparer.Ordinal);
        private readonly HashSet<string> _blocks = new(StringComparer.Ordinal);

        public IReadOnlyCollection<string> BlockedBy => _blockedBy;

        public IReadOnlyCollection<string> Blocks => _blocks;

        public string Id { get; } = id;

        public GitHubRoadmapReconcileIssue Issue { get; } = issue;

        public bool IsOpen { get; } = isOpen;

        public int? Order { get; set; }

        public string? Parent { get; set; }

        public decimal Score => Scoring.Score;

        public IntakeScoring Scoring { get; } = scoring;

        public string Type { get; } = type;

        public void AddBlocker(string blockerId) => _blockedBy.Add(blockerId);

        public void AddBlockedItem(string blockedItemId) => _blocks.Add(blockedItemId);
    }

    private sealed record IntakeScoring(decimal Reach, decimal Impact, decimal Confidence, decimal Effort)
    {
        public decimal Score => Reach * Impact * Confidence / Effort;

        public bool Matches(RoadmapItemSnapshot item) => item.Reach == Reach
            && item.Impact == Impact
            && item.Confidence == Confidence
            && item.Effort == Effort;
    }

    private sealed record IntakeMetadata(string Title, string Type, string Status, string Theme, string[] Labels)
    {
        public bool Matches(RoadmapItemSnapshot item) => string.Equals(item.Title, Title, StringComparison.Ordinal)
            && string.Equals(item.Type, Type, StringComparison.Ordinal)
            && string.Equals(item.Status, Status, StringComparison.Ordinal)
            && string.Equals(item.Theme, Theme, StringComparison.Ordinal)
            && item.Labels.SequenceEqual(Labels, StringComparer.Ordinal);
    }

    private sealed class IntakePlan(
        string rootPath,
        IReadOnlyList<AtomicFileWrite> writes,
        int openItemsToCreate,
        int closedSupportItemsToCreate,
        int closedItemsToTransition,
        int existingItemChanges,
        bool orderUpdated,
        RoadmapWriteInputSnapshot inputSnapshot)
    {
        public void Apply() => RollbackFileWriteBatch.Apply(rootPath, writes, inputSnapshot.Verify);

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

            if (existingItemChanges > 0)
            {
                messages.Add(dryRun
                    ? $"{prefix} update {existingItemChanges} existing roadmap items with exact GitHub metadata."
                    : $"{prefix} updated {existingItemChanges} existing roadmap items with exact GitHub metadata.");
            }

            if (orderUpdated)
            {
                messages.Add(dryRun
                    ? $"{prefix} update roadmap/order.json."
                    : $"{prefix} updated roadmap/order.json.");
            }

            return messages;
        }

    }

    private sealed record ImportedOrderNode(string Id, decimal Score, IReadOnlyList<string> BlockedBy);

    private sealed record ImportedOrdering(
        IReadOnlyList<string> OrderedItemIds,
        Dictionary<string, int> OrdersByItemId,
        Dictionary<string, IntakeScoring> ScoringByItemId,
        bool ExistingOrdersChanged);
}
