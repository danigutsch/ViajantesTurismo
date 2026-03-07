using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Application.Import;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public class RowToCustomerMapperTests
{
    [Fact]
    public void MapCustomer_With_All_Customer_Component_Columns_Returns_Customer()
    {
        // Arrange
        string[] headers =
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

        var row = CsvRow.Parse(
            "John,Doe,Male,1990-01-02,Brazilian,Engineer,123456789,BR,john.doe@example.com,+55 11 99999-9999,johndoe,john.doe,Main St 123,Apartment 1,Centro,12345-678,Sao Paulo,SP,Brazil,75.5,180,Regular,SingleOccupancy,DoubleBed,,Jane Doe,+55 11 98888-8888,Peanuts,None"
        );

        var documentResult = CsvDocument.Create(
            headers: headers,
            rows: [row]
        );

        var document = documentResult.Value;

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
    public void MapCustomer_With_Invalid_Email_Returns_Domain_Validation_Failure()
    {
        // Arrange
        string[] headers =
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

        var row = CsvRow.Parse(
            "John,Doe,Male,1990-01-02,Brazilian,Engineer,123456789,BR,invalid-email,+55 11 99999-9999,johndoe,john.doe,Main St 123,Apartment 1,Centro,12345-678,Sao Paulo,SP,Brazil,75.5,180,Regular,SingleOccupancy,DoubleBed,,Jane Doe,+55 11 98888-8888,Peanuts,None"
        );

        var documentResult = CsvDocument.Create(
            headers: headers,
            rows: [row]
        );

        var document = documentResult.Value;

        // Act
        var customerResult = RowToCustomerMapper.MapCustomer(document, row, TimeProvider.System);

        // Assert
        Assert.True(customerResult.IsFailure);
        Assert.NotNull(customerResult.ErrorDetails);
        Assert.Contains("Email", customerResult.ErrorDetails.Detail, StringComparison.Ordinal);
        Assert.NotNull(customerResult.ErrorDetails.ValidationErrors);
        Assert.True(customerResult.ErrorDetails.ValidationErrors.ContainsKey("Email"));
    }
}
