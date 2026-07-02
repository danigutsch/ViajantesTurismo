namespace ViajantesTurismo.Admin.BehaviorTests.Infrastructure.Coverage;

/// <summary>
/// Validates the InvariantRegistry structure to ensure all invariants are properly registered.
/// </summary>
public class InvariantCoverageTests
{
    [Fact]
    public void Registry_should_contain_exactly_24_tour_invariants()
    {
        // Arrange
        // Act
        var tourInvariants = InvariantRegistry.GetTourInvariants();

        // Assert
        TestAssert.Equal(24, tourInvariants.Length);
    }

    [Fact]
    public void Registry_should_contain_exactly_30_customer_invariants()
    {
        // Arrange
        // Act
        var customerInvariants = InvariantRegistry.GetCustomerInvariants();

        // Assert
        TestAssert.Equal(30, customerInvariants.Length);
    }

    [Fact]
    public void Registry_should_return_total_of_54_invariants()
    {
        // Arrange
        // Act
        var allInvariants = InvariantRegistry.GetAllInvariants();

        // Assert
        TestAssert.Equal(54, allInvariants.Length);
    }

    [Fact]
    public void Tour_invariants_should_follow_naming_convention()
    {
        // Arrange
        // Act
        var tourInvariants = InvariantRegistry.GetTourInvariants();

        // Assert
        foreach (var invariant in tourInvariants)
        {
            TestAssert.Matches(@"^INV-TOUR-\d{3}$", invariant);
        }
    }

    [Fact]
    public void Customer_invariants_should_follow_naming_convention()
    {
        // Arrange
        // Act
        var customerInvariants = InvariantRegistry.GetCustomerInvariants();

        // Assert
        foreach (var invariant in customerInvariants)
        {
            TestAssert.Matches(@"^INV-CUST-\d{3}$", invariant);
        }
    }

    [Fact]
    public void Tour_invariants_should_be_sequential()
    {
        // Arrange
        // Act
        var tourInvariants = InvariantRegistry.GetTourInvariants();
        var numbers = tourInvariants
            .Select(i => int.Parse(i.Split('-')[2], CultureInfo.InvariantCulture))
            .OrderBy(n => n)
            .ToArray();

        // Assert
        for (var i = 0; i < numbers.Length; i++)
        {
            TestAssert.Equal(i + 1, numbers[i]);
        }
    }

    [Fact]
    public void Customer_invariants_should_be_sequential()
    {
        // Arrange
        // Act
        var customerInvariants = InvariantRegistry.GetCustomerInvariants();
        var numbers = customerInvariants
            .Select(i => int.Parse(i.Split('-')[2], CultureInfo.InvariantCulture))
            .OrderBy(n => n)
            .ToArray();

        // Assert
        for (var i = 0; i < numbers.Length; i++)
        {
            TestAssert.Equal(i + 1, numbers[i]);
        }
    }

    [Fact]
    public void All_invariants_should_be_unique()
    {
        // Arrange
        // Act
        var allInvariants = InvariantRegistry.GetAllInvariants();

        // Assert
        TestAssert.Equal(allInvariants.Length, allInvariants.Distinct().Count());
    }

    [Fact]
    public void Registry_should_return_empty_array_for_unknown_aggregate()
    {
        // Arrange
        var unknownAggregateType = typeof(string);

        // Act
        var invariants = InvariantRegistry.GetInvariantsForAggregate(unknownAggregateType);

        // Assert
        TestAssert.Empty(invariants);
    }

    [Theory]
    [InlineData(InvariantRegistry.Tour.UniqueIdentifier)]
    [InlineData(InvariantRegistry.Tour.PercentageDiscountMax100)]
    [InlineData(InvariantRegistry.Customer.EmailUnique)]
    [InlineData(InvariantRegistry.Customer.MedicalInfoMaxLength)]
    public void Registry_should_contain_boundary_invariants(string invariantId)
    {
        // Arrange
        // Act
        var allInvariants = InvariantRegistry.GetAllInvariants();

        // Assert
        TestAssert.Contains(invariantId, allInvariants);
    }

    [Fact]
    public void Coverage_validator_should_initialize_with_all_invariants()
    {
        // Arrange
        // Act
        var validator = new InvariantCoverageValidator();
        var report = validator.GenerateReport();

        // Assert
        TestAssert.Equal(54, report.TotalInvariants);
        TestAssert.Equal(0, report.CoveredInvariants);
        TestAssert.Equal(54, report.UncoveredInvariants.Count);
        TestAssert.Equal(0.0, report.CoveragePercentage);
    }

    [Fact]
    public void Coverage_validator_should_track_scenario_coverage()
    {
        // Arrange
        var validator = new InvariantCoverageValidator();

        // Act
        validator.RecordScenarioCoverage(InvariantRegistry.Tour.UniqueIdentifier, "Test Scenario 1");
        validator.RecordScenarioCoverage(InvariantRegistry.Tour.UniqueIdentifier, "Test Scenario 2");
        validator.RecordScenarioCoverage(InvariantRegistry.Customer.EmailUnique, "Test Scenario 3");

        var report = validator.GenerateReport();

        // Assert
        TestAssert.Equal(54, report.TotalInvariants);
        TestAssert.Equal(2, report.CoveredInvariants);
        TestAssert.Equal(52, report.UncoveredInvariants.Count);
        const double expectedCoveragePercentage = 3.7;
        const double tolerance = 0.2;
        TestAssert.InRange(report.CoveragePercentage, expectedCoveragePercentage - tolerance,
            expectedCoveragePercentage + tolerance);
        TestAssert.Contains(InvariantRegistry.Tour.UniqueIdentifier, report.InvariantToScenarios.Keys);
        TestAssert.Contains(InvariantRegistry.Customer.EmailUnique, report.InvariantToScenarios.Keys);
        TestAssert.Equal(2, report.InvariantToScenarios[InvariantRegistry.Tour.UniqueIdentifier].Count);
        TestAssert.ExactlyOne(report.InvariantToScenarios[InvariantRegistry.Customer.EmailUnique]);
    }

    [Fact]
    public void Coverage_validator_should_calculate_100_percent_coverage_when_all_invariants_covered()
    {
        // Arrange
        var validator = new InvariantCoverageValidator();
        var allInvariants = InvariantRegistry.GetAllInvariants();

        // Act
        foreach (var invariant in allInvariants)
        {
            validator.RecordScenarioCoverage(invariant, $"Scenario for {invariant}");
        }

        var report = validator.GenerateReport();

        // Assert
        TestAssert.Equal(54, report.TotalInvariants);
        TestAssert.Equal(54, report.CoveredInvariants);
        TestAssert.Empty(report.UncoveredInvariants);
        TestAssert.Equal(100.0, report.CoveragePercentage);
    }
}
