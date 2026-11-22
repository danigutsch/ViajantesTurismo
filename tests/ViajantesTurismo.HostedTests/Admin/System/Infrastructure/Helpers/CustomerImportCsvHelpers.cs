using System.Text;
using Microsoft.Playwright;
using PandaBear.Testing.Playwright;

namespace ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Helpers;

/// <summary>Builds customer import CSV content and Playwright upload payloads for system tests.</summary>
internal static class CustomerImportCsvHelpers
{
    private const string CanonicalHeaders =
        "FirstName,LastName,Gender,BirthDate,Nationality,Occupation," +
        "NationalId,IdNationality,Email,Mobile,Street,Neighborhood," +
        "PostalCode,City,State,Country,WeightKg,HeightCentimeters," +
        "BikeType,RoomType,BedType,EmergencyContactName,EmergencyContactMobile";

    /// <summary>Builds a valid canonical data row with the given email and a generated unique national id and mobile number.</summary>
    public static string BuildValidRow(string email)
    {
        var identifier = Guid.NewGuid().ToString("N");
        var nationalId = $"B{identifier[..12]}";
        var mobile = $"+5511{Convert.ToUInt32(identifier[..8], 16) % 90_000_000 + 10_000_000}";

        return $"Jane,Smith,Female,1988-03-15,Brazilian,Designer,{nationalId},BR," +
               $"{email},{mobile},Rua B 456,Centro," +
        $"01310-100,São Paulo,SP,BR,60,165,Regular,DoubleOccupancy,SingleBed," +
        $"Carlos Silva,+5511777776666";
    }

    /// <summary>Builds a CSV with the canonical header and one valid row for the given email.</summary>
    public static string BuildCanonicalCsv(string email) =>
        CanonicalHeaders + "\n" + BuildValidRow(email);

    /// <summary>Returns the canonical header with <paramref name="originalHeader" /> replaced by <paramref name="replacementHeader" />.</summary>
    public static string ReplaceCanonicalHeader(string originalHeader, string replacementHeader) =>
        CanonicalHeaders.Replace(originalHeader, replacementHeader, StringComparison.Ordinal);

    /// <summary>Wraps CSV text as a UTF-8 <c>text/csv</c> Playwright file payload.</summary>
    public static FilePayload ToCsvPayload(string csvContent, string fileName = "customers.csv") =>
        new()
        {
            Name = fileName,
            MimeType = "text/csv",
            Buffer = Encoding.UTF8.GetBytes(csvContent)
        };

    /// <summary>Uploads a CSV through the native chooser after Blazor connects the file input.</summary>
    public static async Task UploadCsv(IPage page, string csvContent)
    {
        var input = page.Locator("input[type='file']");
        await input.WaitForBlazorInputFile();
        var chooser = await page.RunAndWaitForFileChooserAsync(
            () => input.ClickAsync());
        await chooser.SetFilesAsync(ToCsvPayload(csvContent));
    }
}
