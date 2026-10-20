using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, "Regression")]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, SharedKernel.Testing.SharedKernelTestTraitNames.UnitScope)]
public sealed class CivilDateBoundaryTests
{
    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void Minimum_age_changes_on_the_tenth_birthday(int daysAfterBirthday, bool valid)
    {
        // Arrange
        var birthDate = new DateOnly(2020, 2, 3);
        var today = birthDate.AddYears(10).AddDays(daysAfterBirthday);
        // Act
        var result = PersonalInfo.Create("Ana", "Silva", "Female", birthDate, "Brazilian", "Student", today);
        // Assert
        result.IsSuccess.ShouldBe(valid);
    }

    [Fact]
    public void Future_birth_dates_are_rejected()
    {
        // Arrange
        var today = new DateOnly(2030, 2, 3);
        // Act
        var result = PersonalInfo.Create("Ana", "Silva", "Female", today.AddDays(1), "Brazilian", "Student", today);
        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void Payment_validation_uses_the_utc_calendar_date(int daysAfterToday, bool valid)
    {
        // Arrange
        var now = new DateTimeOffset(2030, 2, 3, 0, 0, 0, TimeSpan.Zero);
        var date = new DateOnly(2030, 2, 3).AddDays(daysAfterToday);
        // Act
        var result = Payment.Create(Guid.CreateVersion7(), 100m, date, PaymentMethod.Cash, new FakeTimeProvider(now), null, null);
        // Assert
        result.IsSuccess.ShouldBe(valid);
        if (valid)
        {
            result.Value.PaymentDate.ShouldBe(date);
            result.Value.RecordedAt.ShouldBe(now.UtcDateTime);
        }
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void Schedule_requires_end_after_start(int duration, bool valid)
    {
        // Arrange
        var start = new DateOnly(2030, 2, 3);
        // Act
        var result = TourSchedule.Create(start, start.AddDays(duration));
        // Assert
        result.IsSuccess.ShouldBe(valid);
        if (valid)
        {
            result.Value.DurationDays.ShouldBe(duration);
        }
    }
}
