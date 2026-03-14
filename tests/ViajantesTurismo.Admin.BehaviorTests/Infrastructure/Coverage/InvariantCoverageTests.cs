namespace ViajantesTurismo.Admin.BehaviorTests.Infrastructure.Coverage;

/// <summary>
/// Validates the InvariantRegistry structure to ensure all invariants are properly registered.
/// </summary>
public class InvariantCoverageTests
{
    [Fact]
    public void Registry_ShouldContain_Exactly24TourInvariants()
    {
        // Arrange
        // Act
        var tourInvariants = InvariantRegistry.GetTourInvariants();

        // Assert
        Assert.Equal(24, tourInvariants.Length);
    }

    [Fact]
    public void Registry_ShouldContain_Exactly30CustomerInvariants()
    {
        // Arrange
        // Act
        var customerInvariants = InvariantRegistry.GetCustomerInvariants();

        // Assert
        Assert.Equal(30, customerInvariants.Length);
    }

    [Fact]
    public void Registry_ShouldReturn_TotalOf54Invariants()
    {
        // Arrange
        // Act
        var allInvariants = InvariantRegistry.GetAllInvariants();

        // Assert
        Assert.Equal(54, allInvariants.Length);
    }

    [Fact]
    public void TourInvariants_ShouldFollow_NamingConvention()
    {
        // Arrange
        // Act
        var tourInvariants = InvariantRegistry.GetTourInvariants();

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
        var customerInvariants = InvariantRegistry.GetCustomerInvariants();

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
        var tourInvariants = InvariantRegistry.GetTourInvariants();
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
        var customerInvariants = InvariantRegistry.GetCustomerInvariants();
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
        var allInvariants = InvariantRegistry.GetAllInvariants();

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
    [InlineData(InvariantRegistry.Tour.UniqueIdentifier)]
    [InlineData(InvariantRegistry.Tour.PercentageDiscountMax100)]
    [InlineData(InvariantRegistry.Customer.EmailUnique)]
    [InlineData(InvariantRegistry.Customer.MedicalInfoMaxLength)]
    public void Registry_ShouldContain_BoundaryInvariants(string invariantId)
    {
        // Arrange
        // Act
        var allInvariants = InvariantRegistry.GetAllInvariants();

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
        validator.RecordScenarioCoverage(InvariantRegistry.Tour.UniqueIdentifier, "Test Scenario 1");
        validator.RecordScenarioCoverage(InvariantRegistry.Tour.UniqueIdentifier, "Test Scenario 2");
        validator.RecordScenarioCoverage(InvariantRegistry.Customer.EmailUnique, "Test Scenario 3");

        var report = validator.GenerateReport();

        // Assert
        Assert.Equal(54, report.TotalInvariants);
        Assert.Equal(2, report.CoveredInvariants);
        Assert.Equal(52, report.UncoveredInvariants.Count);
        const double expectedCoveragePercentage = 3.7;
        const double tolerance = 0.2;
        Assert.InRange(report.CoveragePercentage, expectedCoveragePercentage - tolerance,
            expectedCoveragePercentage + tolerance);
        Assert.Contains(InvariantRegistry.Tour.UniqueIdentifier, report.InvariantToScenarios.Keys);
        Assert.Contains(InvariantRegistry.Customer.EmailUnique, report.InvariantToScenarios.Keys);
        Assert.Equal(2, report.InvariantToScenarios[InvariantRegistry.Tour.UniqueIdentifier].Count);
        Assert.Single(report.InvariantToScenarios[InvariantRegistry.Customer.EmailUnique]);
    }

    [Fact]
    public void CoverageValidator_Should_Calculate100PercentCoverage_WhenAllInvariantsCovered()
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
        Assert.Equal(54, report.TotalInvariants);
        Assert.Equal(54, report.CoveredInvariants);
        Assert.Empty(report.UncoveredInvariants);
        Assert.Equal(100.0, report.CoveragePercentage);
    }
}
