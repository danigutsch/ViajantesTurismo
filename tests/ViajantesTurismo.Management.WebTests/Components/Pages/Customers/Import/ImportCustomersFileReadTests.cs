using SharedKernel.Testing;
using System.Text;
using SharedKernel.Blazor;
using ViajantesTurismo.Management.Web.Components.Pages.Customers;
using ViajantesTurismo.Management.WebTests.Infrastructure;
using AdminContractConstants = ViajantesTurismo.Admin.Contracts.Application.ContractConstants;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers.Import;

[Trait(TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[TestScope(TestScope.Component)]
public sealed class ImportCustomersFileReadTests : BunitContext
{
    private readonly FakeCustomersApiClient _fakeCustomersApi = new();
    private readonly RecordingUiFailureReporter _failures = new();

    public ImportCustomersFileReadTests()
    {
        Services.AddSingleton<IUiFailureReporter>(_failures);
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
    }

    [Fact]
    public async Task Imports_the_exact_bytes_of_a_normal_file()
    {
        // Arrange
        var csv = CustomerImportCsvTestData.BuildCsvWithEmail("exact@example.com");
        ReadOnlyMemory<byte> uploaded = default;
        _fakeCustomersApi.SetImportCustomersHandler((bytes, _, _, _) =>
        {
            uploaded = bytes;
            return Task.FromResult(new ImportResultDto(1, 0));
        });
        var cut = Render<ImportCustomers>();
        var file = TestBrowserFiles.Csv(Encoding.UTF8.GetBytes(csv));

        // Act
        await cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([file])));
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").ClickAsync();
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Confirm Import");
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").ClickAsync();

        // Assert
        cut.WaitForAssertion(() => uploaded.IsEmpty.ShouldBeFalse());
        Encoding.UTF8.GetString(uploaded.Span).ShouldBe(csv);
        file.MaximumAllowedSize.ShouldBe(AdminContractConstants.CustomerImportMaxFileBytes);
        _failures.Reports.ShouldBeEmpty();
    }

    [Fact]
    public async Task Imports_a_file_exactly_at_the_maximum_size()
    {
        // Arrange
        var maxBytes = (int)AdminContractConstants.CustomerImportMaxFileBytes;
        var padding = maxBytes - CustomerImportCsvTestData.BuildCsvWithEmail(string.Empty).Length;
        var csv = CustomerImportCsvTestData.BuildCsvWithEmail(new string('x', padding));
        var content = Encoding.UTF8.GetBytes(csv);
        ReadOnlyMemory<byte> uploaded = default;
        _fakeCustomersApi.SetImportCustomersHandler((bytes, _, _, _) =>
        {
            uploaded = bytes;
            return Task.FromResult(new ImportResultDto(1, 0));
        });
        var cut = Render<ImportCustomers>();
        var file = TestBrowserFiles.Csv(content);

        // Act
        await cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([file])));
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").ClickAsync();
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Confirm Import");
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").ClickAsync();

        // Assert
        content.Length.ShouldBe(maxBytes);
        cut.WaitForAssertion(() => uploaded.IsEmpty.ShouldBeFalse());
        Encoding.UTF8.GetString(uploaded.Span).ShouldBe(csv);
        _failures.Reports.ShouldBeEmpty();
    }

    [Fact]
    public async Task Explains_that_an_empty_file_has_no_headers()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = TestBrowserFiles.Csv([]);

        // Act
        await cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([file])));

        // Assert
        file.MaximumAllowedSize.ShouldBe(AdminContractConstants.CustomerImportMaxFileBytes);
        cut.Find(".alert-danger").TextContent.ShouldContain("Headers must contain at least one column", StringComparison.Ordinal);
        cut.FindAll("select[data-field]").ShouldBeEmpty();
        _failures.Reports.ShouldBeEmpty();
    }

    [Fact]
    public async Task Rejects_a_file_over_the_maximum_without_opening_it()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = TestBrowserFiles.Csv([0x01], AdminContractConstants.CustomerImportMaxFileBytes + 1);

        // Act
        await cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([file])));

        // Assert
        cut.Markup.ShouldContain("File is too large", StringComparison.Ordinal);
        file.MaximumAllowedSize.ShouldBeNull();
    }

    [Fact]
    public async Task Reports_a_read_failure_when_the_stream_is_shorter_than_the_declared_size()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = TestBrowserFiles.Csv(Encoding.UTF8.GetBytes("Email\n"), declaredSize: 100);

        // Act
        await cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([file])));

        // Assert
        _failures.Reports.ShouldBe([(nameof(EndOfStreamException), "customers.import.read")]);
        cut.Markup.ShouldContain("could not be read", StringComparison.Ordinal);
        cut.FindAll(".card-header").ShouldBeEmpty();
    }

    [Fact]
    public async Task Reports_a_read_failure_when_the_stream_is_longer_than_the_declared_size()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = TestBrowserFiles.Csv(Encoding.UTF8.GetBytes("Email\nextra@example.com\n"), declaredSize: 6);

        // Act
        await cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([file])));

        // Assert
        _failures.Reports.ShouldBe([(nameof(IOException), "customers.import.read")]);
        cut.Markup.ShouldContain("could not be read", StringComparison.Ordinal);
        cut.FindAll(".card-header").ShouldBeEmpty();
    }
}
