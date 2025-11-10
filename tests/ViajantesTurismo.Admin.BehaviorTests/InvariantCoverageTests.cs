using System.Globalization;

namespace ViajantesTurismo.Admin.BehaviorTests;

/// <summary>
/// Validates the InvariantRegistry structure to ensure all invariants are properly registered.
/// </summary>
public class InvariantCoverageTests
{
    [Fact]
    public void Registry_ShouldContain_Exactly24TourInvariants()
    {
        // Arrange
        var tourInvariants = InvariantRegistry.GetInvariantsForAggregate(typeof(InvariantRegistry.Tour));

        // Assert
        Assert.Equal(24, tourInvariants.Length);
    }

    [Fact]
    public void Registry_ShouldContain_Exactly30CustomerInvariants()
    {
        // Arrange
        // Act
        var customerInvariants = InvariantRegistry.GetInvariantsForAggregate(typeof(InvariantRegistry.Customer));

        // Assert
        Assert.Equal(30, customerInvariants.Length);
    }

    [Fact]
    public void Registry_ShouldReturn_TotalOf54Invariants()
    {
        // Arrange
        // Act
        var tourInvariants = InvariantRegistry.GetInvariantsForAggregate(typeof(InvariantRegistry.Tour));
        var customerInvariants = InvariantRegistry.GetInvariantsForAggregate(typeof(InvariantRegistry.Customer));

        // Assert
        Assert.Equal(54, tourInvariants.Length + customerInvariants.Length);
    }

    [Fact]
    public void TourInvariants_ShouldFollow_NamingConvention()
    {
        // Arrange
        // Act
        var tourInvariants = InvariantRegistry.GetInvariantsForAggregate(typeof(InvariantRegistry.Tour));

        // Assert
        foreach (var invariant in tourInvariants)
        {
            Assert.Matches(@"^INV-TOUR-\d{3}$", invariant);
        }
    }

    [Fact]
    public void CustomerInvariants_ShouldFollow_NamingConvention()
    {
        // Arrange
        // Act
        var customerInvariants = InvariantRegistry.GetInvariantsForAggregate(typeof(InvariantRegistry.Customer));

        // Assert
        foreach (var invariant in customerInvariants)
        {
            Assert.Matches(@"^INV-CUST-\d{3}$", invariant);
        }
    }

    [Fact]
    public void TourInvariants_ShouldBe_Sequential()
    {
        // Arrange
        // Act
        var tourInvariants = InvariantRegistry.GetInvariantsForAggregate(typeof(InvariantRegistry.Tour));
        var numbers = tourInvariants
            .Select(i => int.Parse(i.Split('-')[2], CultureInfo.InvariantCulture))
            .OrderBy(n => n)
            .ToArray();

        // Assert
        for (var i = 0; i < numbers.Length; i++)
        {
            Assert.Equal(i + 1, numbers[i]);
        }
    }

    [Fact]
    public void CustomerInvariants_ShouldBe_Sequential()
    {
        // Arrange
        // Act
        var customerInvariants = InvariantRegistry.GetInvariantsForAggregate(typeof(InvariantRegistry.Customer));
        var numbers = customerInvariants
            .Select(i => int.Parse(i.Split('-')[2], CultureInfo.InvariantCulture))
            .OrderBy(n => n)
            .ToArray();

        // Assert
        for (var i = 0; i < numbers.Length; i++)
        {
            Assert.Equal(i + 1, numbers[i]);
        }
    }

    [Fact]
    public void AllInvariants_ShouldBe_Unique()
    {
        // Arrange
        // Act
        var tourInvariants = InvariantRegistry.GetInvariantsForAggregate(typeof(InvariantRegistry.Tour));
        var customerInvariants = InvariantRegistry.GetInvariantsForAggregate(typeof(InvariantRegistry.Customer));
        var allInvariants = tourInvariants.Concat(customerInvariants).ToArray();

        // Assert
        Assert.Equal(allInvariants.Length, allInvariants.Distinct().Count());
    }

    [Fact]
    public void Registry_ShouldReturn_EmptyArray_ForUnknownAggregate()
    {
        // Arrange
        var unknownAggregateType = typeof(string);

        // Act
        var invariants = InvariantRegistry.GetInvariantsForAggregate(unknownAggregateType);

        // Assert
        Assert.Empty(invariants);
    }

    [Theory]
    [InlineData("INV-TOUR-001")]
    [InlineData("INV-TOUR-024")]
    [InlineData("INV-CUST-001")]
    [InlineData("INV-CUST-030")]
    public void Registry_ShouldContain_BoundaryInvariants(string invariantId)
    {
        // Arrange
        // Act
        var tourInvariants = InvariantRegistry.GetInvariantsForAggregate(typeof(InvariantRegistry.Tour));
        var customerInvariants = InvariantRegistry.GetInvariantsForAggregate(typeof(InvariantRegistry.Customer));
        var allInvariants = tourInvariants.Concat(customerInvariants).ToArray();

        // Assert
        Assert.Contains(invariantId, allInvariants);
    }

    [Fact]
    public void CoverageValidator_ShouldInitialize_WithAllInvariants()
    {
        // Arrange
        // Act
        var validator = new InvariantCoverageValidator();
        var report = validator.GenerateReport();

        // Assert
        Assert.Equal(54, report.TotalInvariants);
        Assert.Equal(0, report.CoveredInvariants);
        Assert.Equal(54, report.UncoveredInvariants.Count);
        Assert.Equal(0.0, report.CoveragePercentage);
    }

    [Fact]
    public void CoverageValidator_ShouldTrack_ScenarioCoverage()
    {
        // Arrange
        var validator = new InvariantCoverageValidator();

        // Act
        validator.RecordScenarioCoverage("INV-TOUR-001", "Test Scenario 1");
        validator.RecordScenarioCoverage("INV-TOUR-001", "Test Scenario 2");
        validator.RecordScenarioCoverage("INV-CUST-001", "Test Scenario 3");

        var report = validator.GenerateReport();

        // Assert
        Assert.Equal(54, report.TotalInvariants);
        Assert.Equal(2, report.CoveredInvariants);
        Assert.Equal(52, report.UncoveredInvariants.Count);
        const double expectedCoveragePercentage = 3.7;
        const double tolerance = 0.2;
        Assert.InRange(report.CoveragePercentage, expectedCoveragePercentage - tolerance,
            expectedCoveragePercentage + tolerance);
        Assert.Contains("INV-TOUR-001", report.InvariantToScenarios.Keys);
        Assert.Contains("INV-CUST-001", report.InvariantToScenarios.Keys);
        Assert.Equal(2, report.InvariantToScenarios["INV-TOUR-001"].Count);
        Assert.Single(report.InvariantToScenarios["INV-CUST-001"]);
    }

    [Fact]
    public void CoverageValidator_Should_Calculate100PercentCoverage_WhenAllInvariantsCovered()
    {
        // Arrange
        var validator = new InvariantCoverageValidator();
        var allInvariants = InvariantRegistry.GetInvariantsForAggregate(typeof(InvariantRegistry.Tour))
            .Concat(InvariantRegistry.GetInvariantsForAggregate(typeof(InvariantRegistry.Customer)));

        // Act
        foreach (var invariant in allInvariants)
        {
            validator.RecordScenarioCoverage(invariant, $"Scenario for {invariant}");
        }

        var report = validator.GenerateReport();

        // Assert
        Assert.Equal(54, report.TotalInvariants);
        Assert.Equal(54, report.CoveredInvariants);
        Assert.Empty(report.UncoveredInvariants);
        Assert.Equal(100.0, report.CoveragePercentage);
    }
}
