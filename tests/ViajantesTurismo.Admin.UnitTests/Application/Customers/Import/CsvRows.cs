namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

/// <summary>Builds customer import CSV content for tests.</summary>
internal static class CsvRows
{
    /// <summary>The CSV header row of the customer import format.</summary>
    public const string Headers =
        "FirstName,LastName,Gender,BirthDate,Nationality,Occupation,NationalId,IdNationality," +
        "Email,Mobile,Street,Neighborhood,PostalCode,City,State,Country," +
        "WeightKg,HeightCentimeters,BikeType,RoomType,BedType,EmergencyContactName,EmergencyContactMobile";

    /// <summary>Builds a header row plus one valid customer row using the given email.</summary>
    public static string Build(string email = "test@example.com") =>
        $"{Headers}\nJohn,Doe,Male,1990-01-01,USA,Engineer,A12345678,USA," +
        $"{email},+1234567890,123 Main St,Downtown,10001,New York,NY,USA," +
        $"75,175,Regular,DoubleOccupancy,SingleBed,Jane Doe,+0987654321";
}
