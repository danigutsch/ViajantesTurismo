using SharedKernel.BuildingBlocks;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, "Regression")]
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

    [Theory]
    [InlineData(2, 28, false)]
    [InlineData(3, 1, true)]
    public void Leap_day_birthday_is_reached_on_the_first_of_march_in_a_common_year(int month, int day, bool valid)
    {
        // Arrange
        var birthDate = new DateOnly(2016, 2, 29);
        var today = new DateOnly(2026, month, day);

        // Act
        var result = PersonalInfo.Create("Ana", "Silva", "Female", birthDate, "Brazilian", "Student", today);

        // Assert
        result.IsSuccess.ShouldBe(valid);
    }

    [Fact]
    public void Missing_birth_date_is_rejected_as_required()
    {
        // Arrange
        var today = new DateOnly(2030, 2, 3);

        // Act
        var result = PersonalInfo.Create("Ana", "Silva", "Female", default, "Brazilian", "Student", today);

        // Assert
        var errors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        errors["BirthDate"].ShouldBe(["Birth date is required."]);
    }

    [Fact]
    public void Too_young_customer_is_rejected_without_revealing_the_age()
    {
        // Arrange
        var today = new DateOnly(2030, 2, 3);

        // Act
        var result = PersonalInfo.Create("Ana", "Silva", "Female", today.AddYears(-9), "Brazilian", "Student", today);

        // Assert
        var errors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        errors["BirthDate"].ShouldBe(["Customer must meet the minimum age requirement."]);
    }

    [Fact]
    public void Names_are_reported_under_the_customer_field_names()
    {
        // Arrange
        var today = new DateOnly(2030, 2, 3);

        // Act
        var result = PersonalInfo.Create(" ", new string('s', 129), "Female", new DateOnly(1990, 1, 1), "Brazilian", "Student", today);

        // Assert
        var errors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        errors["FirstName"].ShouldBe(["First name is required."]);
        errors["LastName"].ShouldBe(["Last name cannot exceed 128 characters."]);
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
    public void Payment_validation_uses_the_UTC_calendar_date(int daysAfterToday, bool valid)
    {
        // Arrange
        var now = new DateTimeOffset(2030, 2, 3, 0, 0, 0, TimeSpan.Zero);
        var date = new DateOnly(2030, 2, 3).AddDays(daysAfterToday);
        // Act
        var scenario = new PaymentAccounts.PaymentScenario();
        var result = scenario.Tour.RecordBookingReceipt(scenario.Booking.Id, 0,
            Admin.Domain.Payments.ReceiptSettlement.Create(Currency.Eur, 100m, 0m).Value,
            date, null, PaymentMethod.Cash, "calendar-test", [], "owner", now, DateOnly.FromDateTime(now.UtcDateTime));
        // Assert
        result.IsSuccess.ShouldBe(valid);
        if (valid)
        {
            var receipt = scenario.History.ExportState().Receipts.ShouldHaveSingleItem();
            receipt.PaymentDate.ShouldBe(date);
            receipt.CreatedAt.ShouldBe(now);
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
