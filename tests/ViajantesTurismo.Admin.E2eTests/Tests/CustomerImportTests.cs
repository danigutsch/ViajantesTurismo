using System.Text;
using Microsoft.Playwright;

namespace ViajantesTurismo.Admin.E2ETests.Tests;

/// <summary>
/// E2E tests for the CSV customer import wizard.
/// </summary>
public class CustomerImportTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    private const string CanonicalHeaders =
        "FirstName,LastName,Gender,BirthDate,Nationality,Occupation," +
        "NationalId,IdNationality,Email,Mobile,Street,Neighborhood," +
        "PostalCode,City,State,Country,WeightKg,HeightCentimeters," +
        "BikeType,RoomType,BedType,EmergencyContactName,EmergencyContactMobile";

    private static string BuildValidRow(string email) =>
        $"Jane,Smith,Female,1988-03-15,Brazilian,Designer,B67890,BR," +
        $"{email},+5511888887777,Rua B 456,Centro," +
        $"01310-100,São Paulo,SP,Brazil,60,165,Regular,DoubleOccupancy,SingleBed," +
        $"Carlos Silva,+5511777776666";

    private static string BuildCanonicalCsv(string email) =>
        CanonicalHeaders + "\n" + BuildValidRow(email);

    private static FilePayload ToCsvPayload(string csvContent, string fileName = "customers.csv") =>
        new()
        {
            Name = fileName,
            MimeType = "text/csv",
            Buffer = Encoding.UTF8.GetBytes(csvContent)
        };

    private async Task UploadCsv(string csvContent)
    {
        await Page.Locator("input[type='file']").SetInputFilesAsync(ToCsvPayload(csvContent));
    }

    [Fact]
    public async Task Can_Navigate_To_Import_Page_And_Upload_Csv_Wizard_Opens()
    {
        await NavigateToAsync("/customers/import");

        await Expect(Page).ToHaveTitleAsync("Import Customers");
        await Expect(Page.GetHeading("Import Customers")).ToBeVisibleAsync();

        var email = $"e2e-ui1-{Guid.NewGuid():N}@import.test";
        await UploadCsv(BuildCanonicalCsv(email));

        await Expect(Page.Locator(".badge.bg-secondary", new PageLocatorOptions { HasText = "column(s) detected" }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task Can_Auto_Match_Canonical_Headers_And_Enable_Preview()
    {
        await NavigateToAsync("/customers/import");

        var email = $"e2e-ui2-{Guid.NewGuid():N}@import.test";
        await UploadCsv(BuildCanonicalCsv(email));

        await Expect(Page.Locator(".alert-success", new PageLocatorOptions { HasText = "automatically matched" }))
            .ToBeVisibleAsync();

        var previewButton = Page.GetButton("Preview");
        await Expect(previewButton).Not.ToBeDisabledAsync();
    }

    [Fact]
    public async Task Can_Block_Preview_When_Required_Header_Is_Not_Canonical()
    {
        await NavigateToAsync("/customers/import");

        // Use a non-canonical header name for Email so auto-match fails
        var nonCanonicalCsv = CanonicalHeaders.Replace("Email", "EmailAddress", StringComparison.Ordinal) +
                               "\n" +
                               BuildValidRow($"e2e-ui3-{Guid.NewGuid():N}@import.test");

        await UploadCsv(nonCanonicalCsv);

        await Expect(Page.Locator(".alert-warning", new PageLocatorOptions { HasText = "could not be matched" }))
            .ToBeVisibleAsync();

        var previewButton = Page.GetButton("Preview");
        await Expect(previewButton).ToBeDisabledAsync();
    }

    [Fact]
    public async Task Can_Surface_Duplicate_Resolution_And_Commit_Keep_Decision()
    {
        var existingCustomer = await ApiTestHelper.CreateCustomerAsync(ApiClient);

        await NavigateToAsync("/customers/import");

        await UploadCsv(BuildCanonicalCsv(existingCustomer.Email));

        await Expect(Page.Locator(".alert-success", new PageLocatorOptions { HasText = "automatically matched" }))
            .ToBeVisibleAsync();

        await Page.GetButton("Preview").ClickAsync();
        await Expect(Page.Locator(".preview-table")).ToBeVisibleAsync();

        await Page.GetButton("Confirm Import").ClickAsync();

        await Expect(Page.GetByText("Resolve Duplicates")).ToBeVisibleAsync();

        var keepButton = Page.Locator("button[data-action='keep']").First;
        await keepButton.ClickAsync();

        var confirmImportButton = Page.Locator("button[data-action='confirm-import']");
        await Expect(confirmImportButton).Not.ToBeDisabledAsync();
        await confirmImportButton.ClickAsync();

        await Expect(Page.Locator(".alert-success", new PageLocatorOptions { HasText = "Import complete" }))
            .ToBeVisibleAsync();
    }
}

/// <summary>
/// Serial E2E tests for the full import commit flow (UI-4).
/// Clean-slate tests that import actual data.
/// </summary>
[Collection("E2E.Serial")]
public class CustomerImportSerialTests(E2EFixture fixture) : E2ESerialTestBase(fixture)
{
    private const string CanonicalHeaders =
        "FirstName,LastName,Gender,BirthDate,Nationality,Occupation," +
        "NationalId,IdNationality,Email,Mobile,Street,Neighborhood," +
        "PostalCode,City,State,Country,WeightKg,HeightCentimeters," +
        "BikeType,RoomType,BedType,EmergencyContactName,EmergencyContactMobile";

    private static string BuildValidRow(string email) =>
        $"Jane,Smith,Female,1988-03-15,Brazilian,Designer,B67890,BR," +
        $"{email},+5511888887777,Rua B 456,Centro," +
        $"01310-100,São Paulo,SP,Brazil,60,165,Regular,DoubleOccupancy,SingleBed," +
        $"Carlos Silva,+5511777776666";

    private static FilePayload ToCsvPayload(string csvContent, string fileName = "customers.csv") =>
        new()
        {
            Name = fileName,
            MimeType = "text/csv",
            Buffer = Encoding.UTF8.GetBytes(csvContent)
        };

    [Fact]
    public async Task Can_Complete_Import_Flow_And_Show_Success_Summary()
    {
        var email = $"e2e-ui4-{Guid.NewGuid():N}@import.test";
        var csv = CanonicalHeaders + "\n" + BuildValidRow(email);

        await NavigateToAsync("/customers/import");

        await Page.Locator("input[type='file']").SetInputFilesAsync(ToCsvPayload(csv));

        // Mapping step: wait for auto-match confirmation, then click Preview
        await Expect(Page.Locator(".alert-success", new PageLocatorOptions { HasText = "automatically matched" }))
            .ToBeVisibleAsync();
        await Page.GetButton("Preview").ClickAsync();

        // Preview step: verify preview table shows the row
        await Expect(Page.Locator(".preview-table")).ToBeVisibleAsync();
        await Expect(Page.Locator(".preview-table tbody tr").First).ToBeVisibleAsync();

        // Confirm import
        await Page.GetButton("Confirm Import").ClickAsync();

        // Result: success banner
        await Expect(Page.Locator(".alert-success", new PageLocatorOptions { HasText = "1 customer(s) imported successfully" }))
            .ToBeVisibleAsync();
    }
}
