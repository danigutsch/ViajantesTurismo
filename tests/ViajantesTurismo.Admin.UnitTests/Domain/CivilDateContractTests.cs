using System.Text.Json;
using ViajantesTurismo.Admin.Testing.Builders;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, "Regression")]
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
        var date = new DateOnly(2030, 2, 3);
        var receipt = new Admin.Contracts.Application.CreateBookingReceiptDto
        {
            ExpectedVersion = 0,
            SettlementCurrency = Admin.Contracts.Application.CurrencyDto.Euro,
            Gross = 100m,
            Fees = 0m,
            PaymentDate = date,
            Reference = "calendar-test",
            Method = Admin.Contracts.Application.PaymentMethodDto.Cash,
            ProposedAllocations = []
        };
        // Act
        var json = JsonSerializer.Serialize(receipt.PaymentDate);
        // Assert
        json.ShouldBe("\"2030-02-03\"");
    }
}
