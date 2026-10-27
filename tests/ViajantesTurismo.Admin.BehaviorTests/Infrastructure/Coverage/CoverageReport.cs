namespace ViajantesTurismo.Admin.BehaviorTests.Infrastructure.Coverage;

public class CoverageReport
{
    public required int TotalInvariants
    {
        get; init;
    }
    public required int CoveredInvariants
    {
        get; init;
    }
    public required IReadOnlyList<string> UncoveredInvariants
    {
        get; init;
    }
    public required IReadOnlyDictionary<string, IReadOnlyList<string>> InvariantToScenarios
    {
        get; init;
    }

    public double CoveragePercentage => TotalInvariants > 0
        ? (double)CoveredInvariants / TotalInvariants * 100
        : 0;
}
