using ViajantesTurismo.Common.BuildingBlocks;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Common.UnitTests.BuildingBlocks;

public sealed class DateRangeTests
{
    [Fact]
    public void Create_WithValidDates_ReturnsSuccessResult()
    {
        // Arrange
        var startDate = new DateTime(2025, 6, 1);
        var endDate = new DateTime(2025, 6, 10);

        // Act
        var result = DateRange.Create(startDate, endDate);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(startDate, result.Value.StartDate);
        Assert.Equal(endDate, result.Value.EndDate);
    }

    [Fact]
    public void Create_WithEndDateBeforeStartDate_ReturnsInvalidResult()
    {
        // Arrange
        var startDate = new DateTime(2025, 6, 10);
        var endDate = new DateTime(2025, 6, 1);

        // Act
        var result = DateRange.Create(startDate, endDate);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("End date must be after start date.", result.ErrorDetails.Detail);
        Assert.NotNull(result.ErrorDetails.ValidationErrors);
        Assert.Contains("schedule", result.ErrorDetails.ValidationErrors.Keys);
        Assert.Equal(["End date must be after start date."], result.ErrorDetails.ValidationErrors["schedule"]);
    }

    [Fact]
    public void Create_WithEndDateEqualToStartDate_ReturnsInvalidResult()
    {
        // Arrange
        var date = new DateTime(2025, 6, 1);

        // Act
        var result = DateRange.Create(date, date);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("End date must be after start date.", result.ErrorDetails.Detail);
    }

    [Fact]
    public void DurationDays_CalculatesCorrectDuration()
    {
        // Arrange
        var startDate = new DateTime(2025, 6, 1);
        var endDate = new DateTime(2025, 6, 8);

        // Act
        var result = DateRange.Create(startDate, endDate);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(7.0, result.Value.DurationDays);
    }

    [Fact]
    public void DurationDays_WithSingleDay_ReturnsCorrectValue()
    {
        // Arrange
        var startDate = new DateTime(2025, 6, 1, 0, 0, 0);
        var endDate = new DateTime(2025, 6, 2, 0, 0, 0);

        // Act
        var result = DateRange.Create(startDate, endDate);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1.0, result.Value.DurationDays);
    }

    [Fact]
    public void DurationDays_WithPartialDays_ReturnsDecimalValue()
    {
        // Arrange
        var startDate = new DateTime(2025, 6, 1, 10, 0, 0);
        var endDate = new DateTime(2025, 6, 2, 14, 0, 0);

        // Act
        var result = DateRange.Create(startDate, endDate);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(28.0 / 24.0, result.Value.DurationDays, precision: 10); // 1 day + 4 hours = 28 hours
    }

    [Fact]
    public void Equality_WithSameDates_AreEqual()
    {
        // Arrange
        var startDate = new DateTime(2025, 6, 1);
        var endDate = new DateTime(2025, 6, 10);
        var range1 = DateRange.Create(startDate, endDate).Value;
        var range2 = DateRange.Create(startDate, endDate).Value;

        // Act & Assert
        Assert.Equal(range1, range2);
        Assert.True(range1.Equals(range2));
        Assert.True(range1 == range2);
        Assert.False(range1 != range2);
    }

    [Fact]
    public void Equality_WithDifferentStartDate_AreNotEqual()
    {
        // Arrange
        var endDate = new DateTime(2025, 6, 10);
        var range1 = DateRange.Create(new DateTime(2025, 6, 1), endDate).Value;
        var range2 = DateRange.Create(new DateTime(2025, 6, 2), endDate).Value;

        // Act & Assert
        Assert.NotEqual(range1, range2);
        Assert.False(range1.Equals(range2));
        Assert.False(range1 == range2);
        Assert.True(range1 != range2);
    }

    [Fact]
    public void Equality_WithDifferentEndDate_AreNotEqual()
    {
        // Arrange
        var startDate = new DateTime(2025, 6, 1);
        var range1 = DateRange.Create(startDate, new DateTime(2025, 6, 10)).Value;
        var range2 = DateRange.Create(startDate, new DateTime(2025, 6, 11)).Value;

        // Act & Assert
        Assert.NotEqual(range1, range2);
        Assert.False(range1.Equals(range2));
        Assert.False(range1 == range2);
        Assert.True(range1 != range2);
    }

    [Fact]
    public void Equality_WithDifferentDates_AreNotEqual()
    {
        // Arrange
        var range1 = DateRange.Create(new DateTime(2025, 6, 1), new DateTime(2025, 6, 10)).Value;
        var range2 = DateRange.Create(new DateTime(2025, 7, 1), new DateTime(2025, 7, 10)).Value;

        // Act & Assert
        Assert.NotEqual(range1, range2);
        Assert.False(range1.Equals(range2));
    }

    [Fact]
    public void Equality_WithNull_AreNotEqual()
    {
        // Arrange
        var range = DateRange.Create(new DateTime(2025, 6, 1), new DateTime(2025, 6, 10)).Value;

        // Act & Assert
        Assert.False(range.Equals(null));
        Assert.False(range == null);
        Assert.True(range != null);
    }

    [Fact]
    public void GetHashCode_WithSameDates_ReturnsSameHashCode()
    {
        // Arrange
        var startDate = new DateTime(2025, 6, 1);
        var endDate = new DateTime(2025, 6, 10);
        var range1 = DateRange.Create(startDate, endDate).Value;
        var range2 = DateRange.Create(startDate, endDate).Value;

        // Act & Assert
        Assert.Equal(range1.GetHashCode(), range2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentDates_ReturnsDifferentHashCodes()
    {
        // Arrange
        var range1 = DateRange.Create(new DateTime(2025, 6, 1), new DateTime(2025, 6, 10)).Value;
        var range2 = DateRange.Create(new DateTime(2025, 7, 1), new DateTime(2025, 7, 10)).Value;

        // Act & Assert
        Assert.NotEqual(range1.GetHashCode(), range2.GetHashCode());
    }

    [Fact]
    public void Equality_WithDifferentTypes_ReturnsFalse()
    {
        // Arrange
        var range = DateRange.Create(new DateTime(2025, 6, 1), new DateTime(2025, 6, 10)).Value;
        var otherObject = new object();

        // Act & Assert
        Assert.False(range.Equals(otherObject));
    }

    [Fact]
    public void Create_WithUtcDates_PreservesUtcKind()
    {
        // Arrange
        var startDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = DateRange.Create(startDate, endDate);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(DateTimeKind.Utc, result.Value.StartDate.Kind);
        Assert.Equal(DateTimeKind.Utc, result.Value.EndDate.Kind);
    }

    [Fact]
    public void Create_WithLongDuration_CalculatesCorrectly()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 12, 31);

        // Act
        var result = DateRange.Create(startDate, endDate);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(364.0, result.Value.DurationDays);
    }

    [Fact]
    public void Create_WithMinimumTimeSpan_ReturnsSuccess()
    {
        // Arrange - 1 second difference
        var startDate = new DateTime(2025, 6, 1, 12, 0, 0);
        var endDate = new DateTime(2025, 6, 1, 12, 0, 1);

        // Act
        var result = DateRange.Create(startDate, endDate);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.DurationDays > 0);
        Assert.True(result.Value.DurationDays < 0.001); // Less than 0.001 days (1.44 minutes)
    }

    [Fact]
    public void Equality_InCollection_WorksCorrectly()
    {
        // Arrange
        var range1 = DateRange.Create(new DateTime(2025, 6, 1), new DateTime(2025, 6, 10)).Value;
        var range2 = DateRange.Create(new DateTime(2025, 6, 1), new DateTime(2025, 6, 10)).Value;
        var range3 = DateRange.Create(new DateTime(2025, 7, 1), new DateTime(2025, 7, 10)).Value;

        var set = new HashSet<DateRange> { range1, range2 };

        // Act & Assert
        Assert.Single(set); // range1 and range2 should be treated as the same
        Assert.Contains(range1, set);
        Assert.Contains(range2, set);
        Assert.DoesNotContain(range3, set);
    }

    [Fact]
    public void Equality_UsingEqualsMethod_WithSameInstance_ReturnsTrue()
    {
        // Arrange
        var range = DateRange.Create(new DateTime(2025, 6, 1), new DateTime(2025, 6, 10)).Value;

        // Act & Assert
        Assert.True(range.Equals(range));
        // ReSharper disable once EqualExpressionComparison
#pragma warning disable CS1718 // Comparison made to same variable
        Assert.True(range == range);
#pragma warning restore CS1718 // Comparison made to same variable
    }
}
