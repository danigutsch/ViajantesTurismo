using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public class RowToCustomerMapperTests
{
    private const string MultipleValidationErrorsDetailMessage = "Multiple validation errors occurred.";
    [Fact]
    public void MapCustomer_When_Row_Contains_All_Supported_Columns_Returns_Customer()
    {
        // Arrange
        var (document, row) = MappingInputs.Create();

        // Act
        var customerResult = RowToCustomerMapper.MapCustomer(document, row, TimeProvider.System);

        // Assert
        Assert.True(customerResult.IsSuccess);

        var customer = customerResult.Value;

        Assert.Equal("John", customer.PersonalInfo.FirstName);
        Assert.Equal("Doe", customer.PersonalInfo.LastName);
        Assert.Equal("123456789", customer.IdentificationInfo.NationalId);
        Assert.Equal("john.doe@example.com", customer.ContactInfo.Email);
        Assert.Equal("Main St 123", customer.Address.Street);
        Assert.Equal(75.5m, customer.PhysicalInfo.WeightKg);
        Assert.Equal(BikeType.Regular, customer.PhysicalInfo.BikeType);
        Assert.Equal(RoomType.SingleOccupancy, customer.AccommodationPreferences.RoomType);
        Assert.Equal("Jane Doe", customer.EmergencyContact.Name);
        Assert.Equal("Peanuts", customer.MedicalInfo.Allergies);
    }

    [Fact]
    public void MapCustomer_When_Email_Is_Invalid_Returns_Email_Validation_Failure()
    {
        // Arrange
        var (document, row) = MappingInputs.Create(overrides: new Dictionary<string, string>
        {
            ["Email"] = "invalid-email"
        });

        // Act
        var customerResult = RowToCustomerMapper.MapCustomer(document, row, TimeProvider.System);

        // Assert
        Assert.True(customerResult.IsFailure);
        Assert.NotNull(customerResult.ErrorDetails);
        Assert.Contains("Email", customerResult.ErrorDetails.Detail, StringComparison.Ordinal);
        Assert.NotNull(customerResult.ErrorDetails.ValidationErrors);
        Assert.True(customerResult.ErrorDetails.ValidationErrors.ContainsKey("Email"));
    }

    [Theory]
    [InlineData("BirthDate", "not-a-date", "BirthDate has invalid format.")]
    [InlineData("WeightKg", "heavy", "WeightKg has invalid format.")]
    [InlineData("HeightCentimeters", "tall", "HeightCentimeters has invalid format.")]
    [InlineData("BikeType", "RocketBike", "BikeType has invalid format.")]
    [InlineData("RoomType", "SpaceSuite", "RoomType has invalid format.")]
    [InlineData("BedType", "CloudBed", "BedType has invalid format.")]
    [InlineData("CompanionId", "definitely-not-a-guid", "CompanionId has invalid format.")]
    public void MapCustomer_When_Import_Field_Format_Is_Invalid_Returns_Field_Validation_Failure(
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
        Assert.True(customerResult.IsFailure);
        Assert.NotNull(customerResult.ErrorDetails);
        Assert.Equal(expectedMessage, customerResult.ErrorDetails.Detail);
        Assert.NotNull(customerResult.ErrorDetails.ValidationErrors);
        Assert.True(customerResult.ErrorDetails.ValidationErrors.TryGetValue(field, out var messages));
        Assert.Equal([expectedMessage], messages);
    }

    [Fact]
    public void MapCustomer_When_Required_Header_Is_Missing_Returns_Header_Validation_Failure()
    {
        // Arrange
        var (document, row) = MappingInputs.Create(headers: MappingInputs.CompleteHeaders.Where(header => header != "FirstName").ToArray());
        const string expectedMessage = "Required header 'FirstName' is missing.";

        // Act
        var customerResult = RowToCustomerMapper.MapCustomer(document, row, TimeProvider.System);

        // Assert
        Assert.True(customerResult.IsFailure);
        Assert.NotNull(customerResult.ErrorDetails);
        Assert.Equal(expectedMessage, customerResult.ErrorDetails.Detail);
        Assert.NotNull(customerResult.ErrorDetails.ValidationErrors);
        Assert.True(customerResult.ErrorDetails.ValidationErrors.TryGetValue("headers", out var messages));
        Assert.Equal([expectedMessage], messages);
    }

    [Fact]
    public void MapCustomer_When_Multiple_Import_Values_Are_Invalid_Returns_Aggregated_Validation_Failure()
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
        Assert.True(customerResult.IsFailure);
        Assert.NotNull(customerResult.ErrorDetails);
        Assert.Equal(MultipleValidationErrorsDetailMessage, customerResult.ErrorDetails.Detail);
        Assert.NotNull(customerResult.ErrorDetails.ValidationErrors);
        Assert.True(customerResult.ErrorDetails.ValidationErrors.ContainsKey("BirthDate"));
        Assert.True(customerResult.ErrorDetails.ValidationErrors.ContainsKey("WeightKg"));
        Assert.True(customerResult.ErrorDetails.ValidationErrors.ContainsKey("CompanionId"));
        Assert.Contains("BirthDate has invalid format.", customerResult.ErrorDetails.ValidationErrors["BirthDate"]);
        Assert.Contains("WeightKg has invalid format.", customerResult.ErrorDetails.ValidationErrors["WeightKg"]);
        Assert.Contains("CompanionId has invalid format.", customerResult.ErrorDetails.ValidationErrors["CompanionId"]);
    }

}
