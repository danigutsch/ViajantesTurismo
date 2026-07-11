using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public class RowToCustomerMapperTests
{
    private const string MultipleValidationErrorsDetailMessage = "Multiple validation errors occurred.";
    [Fact]
    public void MapCustomer_when_row_contains_all_supported_columns_returns_customer()
    {
        // Arrange
        var (document, row) = MappingInputs.Create();

        // Act
        var customerResult = RowToCustomerMapper.MapCustomer(document, row, TimeProvider.System);

        // Assert
        (customerResult.IsSuccess).ShouldBeTrue();

        var customer = customerResult.Value;

        (customer.PersonalInfo.FirstName).ShouldBe("John");
        (customer.PersonalInfo.LastName).ShouldBe("Doe");
        (customer.IdentificationInfo.NationalId).ShouldBe("123456789");
        (customer.ContactInfo.Email).ShouldBe("john.doe@example.com");
        (customer.Address.Street).ShouldBe("Main St 123");
        (customer.PhysicalInfo.WeightKg).ShouldBe(75.5m);
        (customer.PhysicalInfo.BikeType).ShouldBe(BikeType.Regular);
        (customer.AccommodationPreferences.RoomType).ShouldBe(RoomType.SingleOccupancy);
        (customer.EmergencyContact.Name).ShouldBe("Jane Doe");
        (customer.MedicalInfo.Allergies).ShouldBe("Peanuts");
    }

    [Fact]
    public void MapCustomer_when_email_is_invalid_returns_email_validation_failure()
    {
        // Arrange
        var (document, row) = MappingInputs.Create(overrides: new Dictionary<string, string>
        {
            ["Email"] = "invalid-email"
        });

        // Act
        var customerResult = RowToCustomerMapper.MapCustomer(document, row, TimeProvider.System);

        // Assert
        (customerResult.IsFailure).ShouldBeTrue();
        (customerResult.ErrorDetails).ShouldNotBeNull();
        (customerResult.ErrorDetails.Detail).ShouldContain("Email", StringComparison.Ordinal);
        (customerResult.ErrorDetails.ValidationErrors).ShouldNotBeNull();
        (customerResult.ErrorDetails.ValidationErrors.ContainsKey("Email")).ShouldBeTrue();
    }

    [Theory]
    [InlineData("BirthDate", "not-a-date", "BirthDate has invalid format.")]
    [InlineData("WeightKg", "heavy", "WeightKg has invalid format.")]
    [InlineData("HeightCentimeters", "tall", "HeightCentimeters has invalid format.")]
    [InlineData("BikeType", "RocketBike", "BikeType has invalid format.")]
    [InlineData("RoomType", "SpaceSuite", "RoomType has invalid format.")]
    [InlineData("BedType", "CloudBed", "BedType has invalid format.")]
    [InlineData("CompanionId", "definitely-not-a-guid", "CompanionId has invalid format.")]
    public void MapCustomer_when_import_field_format_is_invalid_returns_field_validation_failure(
        string field,
        string invalidValue,
        string expectedMessage)
    {
        // Arrange
        var (document, row) = MappingInputs.Create(overrides: new Dictionary<string, string>
        {
            [field] = invalidValue
        });

        // Act
        var customerResult = RowToCustomerMapper.MapCustomer(document, row, TimeProvider.System);

        // Assert
        (customerResult.IsFailure).ShouldBeTrue();
        (customerResult.ErrorDetails).ShouldNotBeNull();
        (customerResult.ErrorDetails.Detail).ShouldBe(expectedMessage);
        (customerResult.ErrorDetails.ValidationErrors).ShouldNotBeNull();
        (customerResult.ErrorDetails.ValidationErrors.TryGetValue(field, out var messages)).ShouldBeTrue();
        (messages).ShouldBe([expectedMessage]);
    }

    [Fact]
    public void MapCustomer_when_required_header_is_missing_returns_header_validation_failure()
    {
        // Arrange
        var (document, row) = MappingInputs.Create(headers: MappingInputs.CompleteHeaders.Where(header => header != "FirstName").ToArray());
        const string expectedMessage = "Required header 'FirstName' is missing.";

        // Act
        var customerResult = RowToCustomerMapper.MapCustomer(document, row, TimeProvider.System);

        // Assert
        (customerResult.IsFailure).ShouldBeTrue();
        (customerResult.ErrorDetails).ShouldNotBeNull();
        (customerResult.ErrorDetails.Detail).ShouldBe(expectedMessage);
        (customerResult.ErrorDetails.ValidationErrors).ShouldNotBeNull();
        (customerResult.ErrorDetails.ValidationErrors.TryGetValue("headers", out var messages)).ShouldBeTrue();
        (messages).ShouldBe([expectedMessage]);
    }

    [Fact]
    public void MapCustomer_when_multiple_import_values_are_invalid_returns_aggregated_validation_failure()
    {
        // Arrange
        var (document, row) = MappingInputs.Create(overrides: new Dictionary<string, string>
        {
            ["BirthDate"] = "not-a-date",
            ["WeightKg"] = "heavy",
            ["CompanionId"] = "definitely-not-a-guid"
        });

        // Act
        var customerResult = RowToCustomerMapper.MapCustomer(document, row, TimeProvider.System);

        // Assert
        (customerResult.IsFailure).ShouldBeTrue();
        (customerResult.ErrorDetails).ShouldNotBeNull();
        (customerResult.ErrorDetails.Detail).ShouldBe(MultipleValidationErrorsDetailMessage);
        (customerResult.ErrorDetails.ValidationErrors).ShouldNotBeNull();
        (customerResult.ErrorDetails.ValidationErrors.ContainsKey("BirthDate")).ShouldBeTrue();
        (customerResult.ErrorDetails.ValidationErrors.ContainsKey("WeightKg")).ShouldBeTrue();
        (customerResult.ErrorDetails.ValidationErrors.ContainsKey("CompanionId")).ShouldBeTrue();
        (customerResult.ErrorDetails.ValidationErrors["BirthDate"]).ShouldContain("BirthDate has invalid format.");
        (customerResult.ErrorDetails.ValidationErrors["WeightKg"]).ShouldContain("WeightKg has invalid format.");
        (customerResult.ErrorDetails.ValidationErrors["CompanionId"]).ShouldContain("CompanionId has invalid format.");
    }

}
