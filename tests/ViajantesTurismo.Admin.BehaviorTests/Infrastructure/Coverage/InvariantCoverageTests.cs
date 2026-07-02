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
        (tourInvariants.Length).ShouldBe(24);
    }

    [Fact]
    public void Registry_should_contain_exactly_30_customer_invariants()
    {
        // Arrange
        // Act
        var customerInvariants = InvariantRegistry.GetCustomerInvariants();

        // Assert
        (customerInvariants.Length).ShouldBe(30);
    }

    [Fact]
    public void Registry_should_return_total_of_54_invariants()
    {
        // Arrange
        // Act
        var allInvariants = InvariantRegistry.GetAllInvariants();

        // Assert
        (allInvariants.Length).ShouldBe(54);
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
            (invariant).ShouldMatch(@"^INV-TOUR-\d{3}$");
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
            (invariant).ShouldMatch(@"^INV-CUST-\d{3}$");
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
            (numbers[i]).ShouldBe(i + 1);
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
            (numbers[i]).ShouldBe(i + 1);
        }
    }

    [Fact]
    public void All_invariants_should_be_unique()
    {
        // Arrange
        // Act
        var allInvariants = InvariantRegistry.GetAllInvariants();

        // Assert
        (allInvariants.Distinct().Count()).ShouldBe(allInvariants.Length);
    }

    [Fact]
    public void Registry_should_return_empty_array_for_unknown_aggregate()
    {
        // Arrange
        var unknownAggregateType = typeof(string);

        // Act
        var invariants = InvariantRegistry.GetInvariantsForAggregate(unknownAggregateType);

        // Assert
        (invariants).ShouldBeEmpty();
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
        (allInvariants).ShouldContain(invariantId);
    }

    [Fact]
    public void Coverage_validator_should_initialize_with_all_invariants()
    {
        // Arrange
        // Act
        var validator = new InvariantCoverageValidator();
        var report = validator.GenerateReport();

        // Assert
        (report.TotalInvariants).ShouldBe(54);
        (report.CoveredInvariants).ShouldBe(0);
        (report.UncoveredInvariants.Count).ShouldBe(54);
        (report.CoveragePercentage).ShouldBe(0.0);
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
        (report.TotalInvariants).ShouldBe(54);
        (report.CoveredInvariants).ShouldBe(2);
        (report.UncoveredInvariants.Count).ShouldBe(52);
        const double expectedCoveragePercentage = 3.7;
        const double tolerance = 0.2;
        (report.CoveragePercentage).ShouldBeInRange(expectedCoveragePercentage - tolerance, expectedCoveragePercentage + tolerance);
        (report.InvariantToScenarios.Keys).ShouldContain(InvariantRegistry.Tour.UniqueIdentifier);
        (report.InvariantToScenarios.Keys).ShouldContain(InvariantRegistry.Customer.EmailUnique);
        (report.InvariantToScenarios[InvariantRegistry.Tour.UniqueIdentifier].Count).ShouldBe(2);
        (report.InvariantToScenarios[InvariantRegistry.Customer.EmailUnique]).ShouldHaveSingleItem();
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
        (report.TotalInvariants).ShouldBe(54);
        (report.CoveredInvariants).ShouldBe(54);
        (report.UncoveredInvariants).ShouldBeEmpty();
        (report.CoveragePercentage).ShouldBe(100.0);
    }
}
