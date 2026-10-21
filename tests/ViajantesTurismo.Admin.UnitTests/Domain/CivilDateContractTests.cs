using System.Text.Json;
using ViajantesTurismo.Admin.Testing.Builders;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, "Regression")]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, PandaBear.Testing.SharedKernelTestTraitNames.UnitScope)]
public sealed class CivilDateContractTests
{
    [Fact]
    public void Birth_date_serializes_as_a_calendar_date()
    {
        // Arrange
        var customer = DtoBuilders.BuildCreateCustomerDto().PersonalInfo;
        // Act
        var json = JsonSerializer.Serialize(customer.BirthDate);
        // Assert
        json.ShouldBe("\"1990-01-01\"");
    }

    [Fact]
    public void Tour_schedule_serializes_without_time_or_offset()
    {
        // Arrange
        var tour = DtoBuilders.BuildTourDto(startDate: new DateTime(2030, 2, 3, 0, 0, 0, DateTimeKind.Utc));
        // Act
        var json = JsonSerializer.Serialize(tour.StartDate);
        // Assert
        json.ShouldBe("\"2030-02-03\"");
    }

    [Fact]
    public void Payment_date_serializes_as_a_calendar_date()
    {
        // Arrange
        var now = new DateTimeOffset(2030, 2, 3, 14, 0, 0, TimeSpan.Zero);
        var payment = Payment.Create(Guid.CreateVersion7(), 100m, DateOnly.FromDateTime(now.UtcDateTime.Date),
            PaymentMethod.Cash, new FakeTimeProvider(now), null, null).Value;
        // Act
        var json = JsonSerializer.Serialize(payment.PaymentDate);
        // Assert
        json.ShouldBe("\"2030-02-03\"");
    }
}
