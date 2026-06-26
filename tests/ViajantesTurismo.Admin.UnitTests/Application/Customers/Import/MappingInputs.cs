using ViajantesTurismo.Admin.Application.Import;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

internal static class MappingInputs
{
    public static readonly string[] CompleteHeaders =
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

    public static (CsvDocument Document, CsvRow Row) Create(
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
