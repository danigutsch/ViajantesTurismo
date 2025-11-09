namespace ViajantesTurismo.Admin.BehaviorTests;

/// <summary>
/// Validates that all documented invariants have corresponding behavior test coverage.
/// </summary>
public class InvariantCoverageValidator
{
    private readonly HashSet<string> _allInvariants = new();
    private readonly Dictionary<string, List<string>> _invariantToScenarios = new();

    public InvariantCoverageValidator()
    {
        _allInvariants.UnionWith(InvariantRegistry.GetInvariantsForAggregate(typeof(InvariantRegistry.Tour)));
        _allInvariants.UnionWith(InvariantRegistry.GetInvariantsForAggregate(typeof(InvariantRegistry.Customer)));
    }

    /// <summary>
    /// Track that a scenario validates a specific invariant.
    /// Called from hooks during test execution.
    /// </summary>
    public void RecordScenarioCoverage(string invariantId, string scenarioTitle)
    {
        if (!_invariantToScenarios.TryGetValue(invariantId, out var scenarios))
        {
            scenarios = [];
            _invariantToScenarios[invariantId] = scenarios;
        }

        scenarios.Add(scenarioTitle);
    }

    /// <summary>
    /// Generate coverage report showing which invariants are covered.
    /// </summary>
    public CoverageReport GenerateReport()
    {
        var covered = _invariantToScenarios.Keys.ToHashSet();
        var uncovered = _allInvariants.Except(covered).ToList();

        return new CoverageReport
        {
            TotalInvariants = _allInvariants.Count,
            CoveredInvariants = covered.Count,
            UncoveredInvariants = uncovered.AsReadOnly(),
            InvariantToScenarios = _invariantToScenarios.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<string>)kvp.Value.AsReadOnly())
        };
    }
}

public class CoverageReport
{
    public required int TotalInvariants { get; init; }
    public required int CoveredInvariants { get; init; }
    public required IReadOnlyList<string> UncoveredInvariants { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyList<string>> InvariantToScenarios { get; init; }

    public double CoveragePercentage => TotalInvariants > 0
        ? (double)CoveredInvariants / TotalInvariants * 100
        : 0;
}
