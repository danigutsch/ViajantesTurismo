using System.Text;

namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Helpers;

internal static class CustomerImportCsvHelpers
{
    private const string CanonicalHeaders =
        "FirstName,LastName,Gender,BirthDate,Nationality,Occupation," +
        "NationalId,IdNationality,Email,Mobile,Street,Neighborhood," +
        "PostalCode,City,State,Country,WeightKg,HeightCentimeters," +
        "BikeType,RoomType,BedType,EmergencyContactName,EmergencyContactMobile";

    public static string BuildValidRow(string email)
    {
        var identifier = Guid.NewGuid().ToString("N");
        var nationalId = $"B{identifier[..12]}";
        var mobile = $"+5511{Convert.ToUInt32(identifier[..8], 16) % 90_000_000 + 10_000_000}";

        return $"Jane,Smith,Female,1988-03-15,Brazilian,Designer,{nationalId},BR," +
               $"{email},{mobile},Rua B 456,Centro," +
        $"01310-100,São Paulo,SP,Brazil,60,165,Regular,DoubleOccupancy,SingleBed," +
        $"Carlos Silva,+5511777776666";
    }

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
