using ViajantesTurismo.Admin.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers;

public sealed class ImportCustomersPageTests : BunitContext
{
    private static readonly string AllCanonicalHeaders =
        string.Join(",", CustomerImportHeaderMatcher.Fields.Select(f => f.Name));

    private readonly FakeCustomersApiClient _fakeCustomersApi = new();

    public ImportCustomersPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
    }

    [Fact]
    public void Renders_Page_Title_And_Header()
    {
        // Act
        var cut = Render<ImportCustomers>();

        // Assert
        var heading = cut.Find("h1");
        Assert.Contains("Import Customers", heading.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_Back_To_Customers_Button()
    {
        // Act
        var cut = Render<ImportCustomers>();

        // Assert
        var backButton = cut.Find("a.btn-outline-secondary[href='/customers']");
        Assert.Contains("Back to Customers", backButton.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_Description_Text()
    {
        // Act
        var cut = Render<ImportCustomers>();

        // Assert
        Assert.Contains("Upload a CSV file", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_Drop_Zone_With_Instructions_Initially()
    {
        // Act
        var cut = Render<ImportCustomers>();

        // Assert
        Assert.Contains("Drop a CSV file here", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("or click to browse", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Does_Not_Show_Any_Alert_Initially()
    {
        // Act
        var cut = Render<ImportCustomers>();

        // Assert
        Assert.Empty(cut.FindAll(".alert"));
    }

    [Fact]
    public void Drop_Zone_Has_Secondary_Border_Initially()
    {
        // Act
        var cut = Render<ImportCustomers>();

        // Assert
        var dropZone = cut.Find(".border.border-2");
        Assert.Contains("border-secondary", dropZone.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Drag_Enter_Changes_Drop_Zone_To_Primary_Border()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var dropZone = cut.Find(".border.border-2");

        // Act
        dropZone.TriggerEvent("ondragenter", EventArgs.Empty);

        // Assert
        dropZone = cut.Find(".border.border-2");
        Assert.Contains("border-primary", dropZone.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Drag_Leave_Restores_Secondary_Border()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var dropZone = cut.Find(".border.border-2");
        dropZone.TriggerEvent("ondragenter", EventArgs.Empty);

        // Act
        dropZone.TriggerEvent("ondragleave", EventArgs.Empty);

        // Assert
        dropZone = cut.Find(".border.border-2");
        Assert.Contains("border-secondary", dropZone.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_Validation_Error_For_Non_Csv_File()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText("data", "report.xlsx");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".alert.alert-danger")));
        Assert.Contains("Only CSV files are supported", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_Validation_Error_For_Oversized_File()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var largeContent = new byte[6 * 1024 * 1024]; // 6 MB exceeds the 5 MB limit
        var file = InputFileContent.CreateFromBinary(largeContent, "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".alert.alert-danger")));
        Assert.Contains("File is too large", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Maximum allowed size is 5 MB", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_Success_Alert_When_All_Rows_Imported()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(3, 0));
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(AllCanonicalHeaders + "\ndata", "customers.csv");

        // Act — upload file → mapping step → preview step → confirm import
        cut.FindComponent<InputFile>().UploadFiles(file);
        cut.WaitForAssertion(() => Assert.False(cut.Find("button.btn-primary").HasAttribute("disabled")));
        cut.Find("button.btn-primary").Click(); // → preview step
        cut.WaitForAssertion(() => Assert.Contains("Confirm Import", cut.Markup, StringComparison.Ordinal));
        cut.Find("button.btn-primary").Click(); // → confirm import

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("3 customer(s) imported successfully", cut.Markup, StringComparison.Ordinal));
        Assert.DoesNotContain("could not be imported", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_Warning_Alert_When_Import_Has_Row_Errors()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(2, 1));
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(AllCanonicalHeaders + "\ndata", "customers.csv");

        // Act — upload file → mapping step → preview step → confirm import
        cut.FindComponent<InputFile>().UploadFiles(file);
        cut.WaitForAssertion(() => Assert.False(cut.Find("button.btn-primary").HasAttribute("disabled")));
        cut.Find("button.btn-primary").Click(); // → preview step
        cut.WaitForAssertion(() => Assert.Contains("Confirm Import", cut.Markup, StringComparison.Ordinal));
        cut.Find("button.btn-primary").Click(); // → confirm import

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("2 customer(s) imported successfully", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("1 row(s) could not be imported", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_Error_Alert_When_Api_Throws()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersException(new InvalidOperationException("Connection refused"));
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(AllCanonicalHeaders + "\ndata", "customers.csv");

        // Act — upload file → mapping step → preview step → confirm import
        cut.FindComponent<InputFile>().UploadFiles(file);
        cut.WaitForAssertion(() => Assert.False(cut.Find("button.btn-primary").HasAttribute("disabled")));
        cut.Find("button.btn-primary").Click(); // → preview step
        cut.WaitForAssertion(() => Assert.Contains("Confirm Import", cut.Markup, StringComparison.Ordinal));
        cut.Find("button.btn-primary").Click(); // → confirm import

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("We couldn't import the customers right now. Please try again.", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Connection refused", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Success_Result_Shows_View_Customers_And_Import_Another_Actions()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(1, 0));
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(AllCanonicalHeaders + "\ndata", "customers.csv");

        // Act — upload file → mapping step → preview step → confirm import
        cut.FindComponent<InputFile>().UploadFiles(file);
        cut.WaitForAssertion(() => Assert.False(cut.Find("button.btn-primary").HasAttribute("disabled")));
        cut.Find("button.btn-primary").Click(); // → preview step
        cut.WaitForAssertion(() => Assert.Contains("Confirm Import", cut.Markup, StringComparison.Ordinal));
        cut.Find("button.btn-primary").Click(); // → confirm import

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("Import another file", cut.Markup, StringComparison.Ordinal));
        Assert.NotNull(cut.Find("a[href='/customers']"));
    }

    [Fact]
    public void Reset_Restores_Drop_Zone_After_Successful_Import()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(1, 0));
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(AllCanonicalHeaders + "\ndata", "customers.csv");
        cut.FindComponent<InputFile>().UploadFiles(file);
        cut.WaitForAssertion(() => Assert.False(cut.Find("button.btn-primary").HasAttribute("disabled")));
        cut.Find("button.btn-primary").Click(); // → preview step
        cut.WaitForAssertion(() => Assert.Contains("Confirm Import", cut.Markup, StringComparison.Ordinal));
        cut.Find("button.btn-primary").Click(); // → confirm import
        cut.WaitForAssertion(() => Assert.Contains("Import another file", cut.Markup, StringComparison.Ordinal));

        // Act — click "Import another file" (btn-sm variant in the result alert)
        cut.Find("button.btn-sm.btn-outline-secondary").Click();

        // Assert
        Assert.Contains("Drop a CSV file here", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll(".alert"));
    }
}
