using ViajantesTurismo.Admin.Application.Customers.Import;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public sealed class RowToCustomerMapperSectionTests
{
    [Theory]
    [InlineData("FirstName")]
    [InlineData("NationalId")]
    [InlineData("Email")]
    [InlineData("Street")]
    [InlineData("WeightKg")]
    [InlineData("RoomType")]
    [InlineData("EmergencyContactName")]
    public void MapCustomer_reports_a_missing_required_header_of_every_customer_section(string header)
    {
        // Arrange
        var headers = MappingInputs.CompleteHeaders.Where(candidate => candidate != header).ToArray();
        var (document, row) = MappingInputs.Create(headers: headers);

        // Act
        var result = RowToCustomerMapper.MapCustomer(document, row, new DateOnly(2030, 1, 1));

        // Assert
        result.IsFailure.ShouldBeTrue();
        var validationErrors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        validationErrors["headers"].ShouldContain($"Required header '{header}' is missing.");
    }

    [Theory]
    [InlineData("BikeType")]
    [InlineData("RoomType")]
    [InlineData("BedType")]
    [InlineData("HeightCentimeters")]
    [InlineData("CompanionId")]
    public void MapCustomer_reports_a_value_that_is_not_in_the_expected_format(string field)
    {
        // Arrange
        var (document, row) = MappingInputs.Create(overrides: new Dictionary<string, string> { [field] = "not-a-valid-value" });

        // Act
        var result = RowToCustomerMapper.MapCustomer(document, row, new DateOnly(2030, 1, 1));

        // Assert
        result.IsFailure.ShouldBeTrue();
        var validationErrors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        validationErrors[field].ShouldHaveSingleItem().ShouldBe($"{field} has invalid format.");
    }

    [Fact]
    public void MapCustomer_reports_medical_information_that_exceeds_its_limits()
    {
        // Arrange
        var tooLong = new string('a', 5000);
        var (document, row) = MappingInputs.Create(overrides: new Dictionary<string, string> { ["Allergies"] = tooLong, ["AdditionalInfo"] = tooLong });

        // Act
        var result = RowToCustomerMapper.MapCustomer(document, row, new DateOnly(2030, 1, 1));

        // Assert
        result.IsFailure.ShouldBeTrue();
        var validationErrors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        validationErrors.Keys.ShouldContain("Allergies");
    }

    [Fact]
    public void MapCustomer_reports_invalid_identification_address_and_emergency_contact_values_together()
    {
        // Arrange
        var (document, row) = MappingInputs.Create(overrides: new Dictionary<string, string>
        {
            ["NationalId"] = string.Empty,
            ["Street"] = string.Empty,
            ["EmergencyContactName"] = string.Empty,
        });

        // Act
        var result = RowToCustomerMapper.MapCustomer(document, row, new DateOnly(2030, 1, 1));

        // Assert
        result.IsFailure.ShouldBeTrue();
        var keys = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull().Keys;
        keys.ShouldContain("NationalId");
        keys.ShouldContain("Street");
        keys.ShouldContain("Name");
    }
}
