using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Application.Import;
using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public class RowToCustomerMapperTests
{
    private const string MultipleValidationErrorsDetailMessage = "Multiple validation errors occurred.";
    private static readonly string[] CompleteHeaders =
    [
        "FirstName", "LastName", "Gender", "BirthDate", "Nationality", "Occupation",
        "NationalId", "IdNationality",
        "Email", "Mobile", "Instagram", "Facebook",
        "Street", "Complement", "Neighborhood", "PostalCode", "City", "State", "Country",
        "WeightKg", "HeightCentimeters", "BikeType",
        "RoomType", "BedType", "CompanionId",
        "EmergencyContactName", "EmergencyContactMobile",
        "Allergies", "AdditionalInfo"
    ];

    private static readonly IReadOnlyDictionary<string, string> ValidRowValues = new Dictionary<string, string>
    {
        ["FirstName"] = "John",
        ["LastName"] = "Doe",
        ["Gender"] = "Male",
        ["BirthDate"] = "1990-01-02",
        ["Nationality"] = "Brazilian",
        ["Occupation"] = "Engineer",
        ["NationalId"] = "123456789",
        ["IdNationality"] = "BR",
        ["Email"] = "john.doe@example.com",
        ["Mobile"] = "+55 11 99999-9999",
        ["Instagram"] = "johndoe",
        ["Facebook"] = "john.doe",
        ["Street"] = "Main St 123",
        ["Complement"] = "Apartment 1",
        ["Neighborhood"] = "Centro",
        ["PostalCode"] = "12345-678",
        ["City"] = "Sao Paulo",
        ["State"] = "SP",
        ["Country"] = "Brazil",
        ["WeightKg"] = "75.5",
        ["HeightCentimeters"] = "180",
        ["BikeType"] = "Regular",
        ["RoomType"] = "SingleOccupancy",
        ["BedType"] = "DoubleBed",
        ["CompanionId"] = string.Empty,
        ["EmergencyContactName"] = "Jane Doe",
        ["EmergencyContactMobile"] = "+55 11 98888-8888",
        ["Allergies"] = "Peanuts",
        ["AdditionalInfo"] = "None"
    };

    [Fact]
    public void MapCustomer_When_Row_Contains_All_Supported_Columns_Returns_Customer()
    {
        // Arrange
        var (document, row) = CreateMappingInputs();

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
        var (document, row) = CreateMappingInputs(overrides: new Dictionary<string, string>
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
        var (document, row) = CreateMappingInputs(overrides: new Dictionary<string, string>
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
        var (document, row) = CreateMappingInputs(headers: CompleteHeaders.Where(header => header != "FirstName").ToArray());
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
        var (document, row) = CreateMappingInputs(overrides: new Dictionary<string, string>
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

    private static (CsvDocument Document, CsvRow Row) CreateMappingInputs(
        IReadOnlyDictionary<string, string>? overrides = null,
        IReadOnlyList<string>? headers = null)
    {
        var effectiveHeaders = headers ?? CompleteHeaders;
        var values = BuildRowValues(overrides);
        var row = CsvRow.Parse(string.Join(",", effectiveHeaders.Select(header => values[header])));
        var documentResult = CsvDocument.Create([.. effectiveHeaders], [row]);

        return documentResult.IsFailure
            ? throw new InvalidOperationException(documentResult.ErrorDetails?.Detail ?? "Failed to create CSV document for test.")
            : (documentResult.Value, row);
    }

    private static Dictionary<string, string> BuildRowValues(IReadOnlyDictionary<string, string>? overrides)
    {
        var values = ValidRowValues.ToDictionary(entry => entry.Key, entry => entry.Value);

        if (overrides is null)
        {
            return values;
        }

        foreach (var (key, value) in overrides)
        {
            values[key] = value;
        }

        return values;
    }
}
