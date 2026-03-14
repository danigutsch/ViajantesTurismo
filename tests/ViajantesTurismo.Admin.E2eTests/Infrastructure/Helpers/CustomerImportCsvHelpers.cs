using System.Text;

namespace ViajantesTurismo.Admin.E2ETests.Infrastructure.Helpers;

internal static class CustomerImportCsvHelpers
{
    private const string CanonicalHeaders =
        "FirstName,LastName,Gender,BirthDate,Nationality,Occupation," +
        "NationalId,IdNationality,Email,Mobile,Street,Neighborhood," +
        "PostalCode,City,State,Country,WeightKg,HeightCentimeters," +
        "BikeType,RoomType,BedType,EmergencyContactName,EmergencyContactMobile";

    public static string BuildValidRow(string email) =>
        $"Jane,Smith,Female,1988-03-15,Brazilian,Designer,B67890,BR," +
        $"{email},+5511888887777,Rua B 456,Centro," +
        $"01310-100,São Paulo,SP,Brazil,60,165,Regular,DoubleOccupancy,SingleBed," +
        $"Carlos Silva,+5511777776666";

    public static string BuildCanonicalCsv(string email) =>
        CanonicalHeaders + "\n" + BuildValidRow(email);

    public static string ReplaceCanonicalHeader(string originalHeader, string replacementHeader) =>
        CanonicalHeaders.Replace(originalHeader, replacementHeader, StringComparison.Ordinal);

    public static FilePayload ToCsvPayload(string csvContent, string fileName = "customers.csv") =>
        new()
        {
            Name = fileName,
            MimeType = "text/csv",
            Buffer = Encoding.UTF8.GetBytes(csvContent)
        };

    public static async Task UploadCsv(IPage page, string csvContent)
    {
        await page.Locator("input[type='file']").SetInputFilesAsync(ToCsvPayload(csvContent));
    }
}
