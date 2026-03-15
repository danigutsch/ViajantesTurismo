using System.Text.RegularExpressions;

namespace ViajantesTurismo.Admin.E2ETests.Customers;

/// <summary>
/// E2E tests for the CSV customer import wizard.
/// </summary>
public class CustomerImportTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Can_Navigate_To_Import_Page_And_Upload_Csv_Wizard_Opens()
    {
        // Arrange
        var email = $"e2e-ui1-{Guid.NewGuid():N}@import.test";

        // Act
        await NavigateTo("/customers/import");
        await CustomerImportCsvHelpers.UploadCsv(Page, CustomerImportCsvHelpers.BuildCanonicalCsv(email));

        // Assert
        await Expect(Page).ToHaveTitleAsync("Import Customers");
        await Expect(Page.GetHeading("Import Customers")).ToBeVisibleAsync();
        await Expect(Page.Locator(".badge.bg-secondary", new PageLocatorOptions { HasText = "column(s) detected" }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task Can_Auto_Match_Canonical_Headers_And_Enable_Preview()
    {
        // Arrange
        var email = $"e2e-ui2-{Guid.NewGuid():N}@import.test";

        // Act
        await NavigateTo("/customers/import");
        await CustomerImportCsvHelpers.UploadCsv(Page, CustomerImportCsvHelpers.BuildCanonicalCsv(email));

        // Assert
        await Expect(Page.Locator(".alert-success", new PageLocatorOptions { HasText = "automatically matched" }))
            .ToBeVisibleAsync();

        var previewButton = Page.GetButton("Preview");
        await Expect(previewButton).Not.ToBeDisabledAsync();
    }

    [Fact]
    public async Task Can_Block_Preview_When_Required_Header_Is_Not_Canonical()
    {
        // Arrange
        var nonCanonicalCsv = CustomerImportCsvHelpers.ReplaceCanonicalHeader("Email", "EmailAddress") +
                              "\n" +
                              CustomerImportCsvHelpers.BuildValidRow($"e2e-ui3-{Guid.NewGuid():N}@import.test");

        // Act
        await NavigateTo("/customers/import");
        await CustomerImportCsvHelpers.UploadCsv(Page, nonCanonicalCsv);

        // Assert
        await Expect(Page.Locator(".alert-warning", new PageLocatorOptions { HasText = "could not be matched" }))
            .ToBeVisibleAsync();

        var previewButton = Page.GetButton("Preview");
        await Expect(previewButton).ToBeDisabledAsync();
    }

    [Fact]
    public async Task Can_Surface_Duplicate_Resolution_And_Commit_Keep_Decision()
    {
        // Arrange
        var existingCustomer = await ApiClient.CreateCustomer();

        // Act
        await NavigateTo("/customers/import");
        await CustomerImportCsvHelpers.UploadCsv(Page, CustomerImportCsvHelpers.BuildCanonicalCsv(existingCustomer.Email));
        await Expect(Page.Locator(".alert-success", new PageLocatorOptions { HasText = "automatically matched" }))
            .ToBeVisibleAsync();
        await Page.GetButton("Preview").ClickAsync();
        await Page.GetButton("Confirm Import").ClickAsync();

        // Assert
        await Expect(Page.Locator(".preview-table")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Resolve Duplicates")).ToBeVisibleAsync();
        var duplicateRow = Page.Locator(".duplicate-resolution-table tbody tr")
            .Filter(new LocatorFilterOptions { HasText = existingCustomer.Email });
        await Expect(duplicateRow).ToHaveCountAsync(1);

        var keepButton = duplicateRow.Locator("button[data-action='keep']");
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
public partial class CustomerImportSerialTests(E2EFixture fixture) : E2ESerialTestBase(fixture)
{
    [Fact]
    public async Task Can_Complete_Import_Flow_Show_Final_Summary_And_Open_Customer_Details()
    {
        // Arrange
        var email = $"e2e-ui6-{Guid.NewGuid():N}@import.test";
        var csv = CustomerImportCsvHelpers.BuildCanonicalCsv(email);

        // Act
        await NavigateTo("/customers/import");
        await CustomerImportCsvHelpers.UploadCsv(Page, csv);
        await Expect(Page.Locator(".alert-success", new PageLocatorOptions { HasText = "automatically matched" }))
            .ToBeVisibleAsync();
        await Page.GetButton("Preview").ClickAsync();

        var previewRow = Page.Locator(".preview-table tbody tr")
            .Filter(new LocatorFilterOptions { HasText = email });

        await Expect(Page.Locator(".preview-table")).ToBeVisibleAsync();
        await Expect(previewRow).ToHaveCountAsync(1);

        await Page.GetButton("Confirm Import").ClickAsync();

        // Assert
        await Expect(Page.Locator(".alert-success", new PageLocatorOptions { HasText = "1 customer(s) imported successfully" }))
            .ToBeVisibleAsync();
        await Expect(Page.Locator("[data-testid='import-summary-counts']")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Created: 1")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Updated: 0")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Skipped: 0")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Failed: 0")).ToBeVisibleAsync();

        var successRow = Page.Locator("[data-testid='summary-success-rows'] tbody tr")
            .Filter(new LocatorFilterOptions { HasText = email });
        await Expect(successRow).ToHaveCountAsync(1);

        var viewCustomerLink = successRow.Locator("a[data-action='view-customer']");
        await Expect(viewCustomerLink).ToBeVisibleAsync();
        await viewCustomerLink.ClickAsync();

        await Expect(Page).ToHaveURLAsync(CustomerUrlRegex());
        await Expect(Page).ToHaveTitleAsync("Customer Details");
    }

    [GeneratedRegex(".*/customers/[0-9a-fA-F-]+$")]
    private static partial Regex CustomerUrlRegex();
}
