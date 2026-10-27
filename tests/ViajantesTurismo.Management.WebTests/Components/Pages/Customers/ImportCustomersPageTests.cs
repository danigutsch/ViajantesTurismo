using ViajantesTurismo.Management.WebTests.Infrastructure;
using System.Globalization;
using System.Text;
using ViajantesTurismo.Management.Web.Components.Pages.Customers;
using AdminContractConstants = ViajantesTurismo.Admin.Contracts.Application.ContractConstants;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
public sealed class ImportCustomersPageTests : BunitContext
{
    private readonly FakeCustomersApiClient _fakeCustomersApi = new();
    private readonly RecordingUiFailureReporter _failures = new();

    public ImportCustomersPageTests()
    {
        Services.AddSingleton<PandaBear.Blazor.IUiFailureReporter>(_failures);
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
    }

    [Fact]
    public void Renders_page_title_and_header()
    {
        // Act
        var cut = Render<ImportCustomers>();

        // Assert
        var heading = cut.Find("h1");
        heading.TextContent.ShouldContain("Import Customers", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_back_to_customers_button()
    {
        // Act
        var cut = Render<ImportCustomers>();

        // Assert
        var backButton = cut.Find("a.btn-outline-secondary[href='/customers']");
        backButton.TextContent.ShouldContain("Back to Customers", StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_description_text()
    {
        // Act
        var cut = Render<ImportCustomers>();

        // Assert
        cut.Markup.ShouldContain("Upload a CSV file", StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_drop_zone_with_instructions_initially()
    {
        // Act
        var cut = Render<ImportCustomers>();

        // Assert
        cut.Markup.ShouldContain("Drop a CSV file here", StringComparison.Ordinal);
        cut.Markup.ShouldContain("or click to browse", StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_the_authoritative_customer_import_limit()
    {
        // Arrange
        var expectedLimitInMebibytes = AdminContractConstants.CustomerImportMaxFileBytes / (1024 * 1024);

        // Act
        var cut = Render<ImportCustomers>();

        // Assert
        cut.Markup.ShouldContain($"max {expectedLimitInMebibytes} MiB", StringComparison.Ordinal);
    }

    [Fact]
    public void Does_not_show_any_alert_initially()
    {
        // Act
        var cut = Render<ImportCustomers>();

        // Assert
        cut.FindAll(".alert").ShouldBeEmpty();
    }

    [Fact]
    public void Drop_zone_has_secondary_border_initially()
    {
        // Act
        var cut = Render<ImportCustomers>();

        // Assert
        var dropZone = cut.Find(".border.border-2");
        dropZone.ClassName.ShouldContain("border-secondary", StringComparison.Ordinal);
    }

    [Fact]
    public void Drag_enter_changes_drop_zone_to_primary_border()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var dropZone = cut.Find(".border.border-2");

        // Act
        dropZone.TriggerEvent("ondragenter", EventArgs.Empty);

        // Assert
        dropZone = cut.Find(".border.border-2");
        dropZone.ClassName.ShouldContain("border-primary", StringComparison.Ordinal);
    }

    [Fact]
    public void Drag_leave_restores_secondary_border()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var dropZone = cut.Find(".border.border-2");
        dropZone.TriggerEvent("ondragenter", EventArgs.Empty);

        // Act
        dropZone.TriggerEvent("ondragleave", EventArgs.Empty);

        // Assert
        dropZone = cut.Find(".border.border-2");
        dropZone.ClassName.ShouldContain("border-secondary", StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_validation_error_for_non_csv_file()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText("data", "report.xlsx");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => cut.FindAll(".alert.alert-danger").ShouldNotBeEmpty());
        cut.Markup.ShouldContain("Only CSV files are supported", StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_validation_error_for_oversized_file()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var largeContent = new byte[2 * 1024 * 1024];
        var file = InputFileContent.CreateFromBinary(largeContent, "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => cut.FindAll(".alert.alert-danger").ShouldNotBeEmpty());
        cut.Markup.ShouldContain("File is too large", StringComparison.Ordinal);
        cut.Markup.ShouldContain("Maximum allowed size is 1 MiB", StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_success_alert_when_all_rows_imported()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(3, 0));
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\ndata", "customers.csv");

        // Act — upload file → mapping step → preview step → confirm import
        cut.FindComponent<InputFile>().UploadFiles(file);
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").Click();
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Confirm Import");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();

        // Assert
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("3 customer(s) imported successfully", StringComparison.Ordinal));
        cut.Markup.ShouldNotContain("could not be imported", StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_warning_alert_when_import_has_row_errors()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(2, 1));
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\ndata", "customers.csv");

        // Act — upload file → mapping step → preview step → confirm import
        cut.FindComponent<InputFile>().UploadFiles(file);
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").Click();
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Confirm Import");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();

        // Assert
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("2 customer(s) imported successfully", StringComparison.Ordinal));
        cut.Markup.ShouldContain("1 row(s) could not be imported", StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_error_alert_when_API_throws()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersException(new InvalidOperationException("Connection refused"));
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\ndata", "customers.csv");

        // Act — upload file → mapping step → preview step → confirm import
        cut.FindComponent<InputFile>().UploadFiles(file);
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").Click();
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Confirm Import");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("We couldn't import the customers right now. Please try again.", StringComparison.Ordinal);
            cut.Markup.ShouldNotContain("Connection refused", StringComparison.Ordinal);
        });
        _failures.Reports.ShouldBe([(nameof(InvalidOperationException), "customers.import.upload")]);
    }

    [Fact]
    public void Success_result_shows_view_customers_and_import_another_actions()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(1, 0));
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\ndata", "customers.csv");

        // Act — upload file → mapping step → preview step → confirm import
        cut.FindComponent<InputFile>().UploadFiles(file);
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").Click();
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Confirm Import");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();

        // Assert
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Import another file", StringComparison.Ordinal));
        cut.Find("a[href='/customers']").ShouldNotBeNull();
    }

    [Fact]
    public void Reset_restores_drop_zone_after_successful_import()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(1, 0));
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\ndata", "customers.csv");
        cut.FindComponent<InputFile>().UploadFiles(file);
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").Click();
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Confirm Import");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Import another file", StringComparison.Ordinal));

        // Act — click "Import another file" (btn-sm variant in the result alert)
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Import another file").Click();

        // Assert
        cut.Markup.ShouldContain("Drop a CSV file here", StringComparison.Ordinal);
        cut.FindAll(".alert").ShouldBeEmpty();
    }

    [Fact]
    public async Task Cancel_moves_focus_to_the_status_region_that_outlives_the_cancel_button()
    {
        // Arrange
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCompletion = new TaskCompletionSource<ImportResultDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        _fakeCustomersApi.SetImportCustomersHandler((_, _, _, _) =>
        {
            requestStarted.TrySetResult();
            return requestCompletion.Task;
        });
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\ndata", "customers.csv");
        cut.FindComponent<InputFile>().UploadFiles(file);
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").ClickAsync();
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Confirm Import");
        var importTask = ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").ClickAsync();
        await requestStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);

        // Act
        await cut.Find("button[aria-label='Cancel upload of customers.csv']").ClickAsync();
        requestCompletion.TrySetResult(new ImportResultDto(1, 0));
        await importTask;

        // Assert
        cut.Find("section[aria-label='Customer import upload status']").GetAttribute("tabindex").ShouldBe("-1");
        JSInterop.Invocations.ShouldContain(invocation => invocation.Identifier == "Blazor._internal.domWrapper.focus");
    }

    [Fact]
    public async Task Cancel_targets_the_current_file_and_late_completion_cannot_replace_canceled_state()
    {
        // Arrange
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCompletion = new TaskCompletionSource<ImportResultDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        _fakeCustomersApi.SetImportCustomersHandler((_, _, _, _) =>
        {
            requestStarted.TrySetResult();
            return requestCompletion.Task;
        });
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\ndata", "customers.csv");
        cut.FindComponent<InputFile>().UploadFiles(file);
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").ClickAsync();
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Confirm Import");

        // Act
        var importTask = ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").ClickAsync();
        await requestStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);
        var cancelButton = cut.Find("button[aria-label='Cancel upload of customers.csv']");
        cancelButton.TagName.ShouldBe("BUTTON");
        cancelButton.GetAttribute("type").ShouldBe("button");
        await cancelButton.ClickAsync();
        requestCompletion.TrySetResult(new ImportResultDto(1, 0));
        await importTask;

        // Assert
        _fakeCustomersApi.LastImportCancellationToken.IsCancellationRequested.ShouldBeTrue();
        cut.Find("[data-upload-status]").TextContent.ShouldContain("Canceled", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("Import complete", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Displays_authoritative_transport_bytes_then_indeterminate_server_phases_with_polite_announcements()
    {
        // Arrange
        var requestStarted = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCompletion = new TaskCompletionSource<ImportResultDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        IProgress<long>? reportedProgress = null;
        _fakeCustomersApi.SetImportCustomersHandler((content, _, progress, _) =>
        {
            reportedProgress = progress;
            requestStarted.TrySetResult(content.LongLength);
            return requestCompletion.Task;
        });
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\ndata", "customers.csv");
        cut.FindComponent<InputFile>().UploadFiles(file);
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").ClickAsync();
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Confirm Import");

        // Act
        var importTask = ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").ClickAsync();
        var totalBytes = await requestStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);
        var progress = reportedProgress.ShouldNotBeNull();
        var transferredBytes = totalBytes / 2;
        progress.Report(transferredBytes);

        // Assert
        await cut.WaitForAssertionAsync(() => cut.Find("progress[value]").GetAttribute("value").ShouldBe(transferredBytes.ToString(CultureInfo.InvariantCulture)));
        cut.Find("progress[value]").GetAttribute("max").ShouldBe(totalBytes.ToString(CultureInfo.InvariantCulture));
        var liveRegion = cut.Find("[role='status'][aria-live='polite']");
        liveRegion.GetAttribute("aria-atomic").ShouldBe("true");

        // Act
        progress.Report(totalBytes);

        // Assert
        await cut.WaitForAssertionAsync(() => cut.Find("progress:not([value])").ShouldNotBeNull());
        cut.Find("[data-upload-status]").TextContent.ShouldContain("scanning, validating, parsing", StringComparison.Ordinal);
        cut.Find("[data-upload-status]").TextContent.ShouldContain("Progress is indeterminate", StringComparison.Ordinal);

        requestCompletion.TrySetResult(new ImportResultDto(1, 0));
        await importTask;
    }

    [Fact]
    public async Task Retry_uses_a_new_cancellation_token_and_runs_the_import_request_again()
    {
        // Arrange
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstRequestCompletion = new TaskCompletionSource<ImportResultDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callCount = 0;
        var firstToken = CancellationToken.None;
        var retryToken = CancellationToken.None;
        _fakeCustomersApi.SetImportCustomersHandler((content, _, progress, ct) =>
        {
            if (Interlocked.Increment(ref callCount) == 1)
            {
                firstToken = ct;
                requestStarted.TrySetResult();
                return firstRequestCompletion.Task;
            }

            retryToken = ct;
            progress?.Report(content.LongLength);
            return Task.FromResult(new ImportResultDto(1, 0));
        });
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\ndata", "customers.csv");
        cut.FindComponent<InputFile>().UploadFiles(file);
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").ClickAsync();
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Confirm Import");

        // Act
        var firstImport = ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").ClickAsync();
        await requestStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);
        await cut.Find("button[aria-label='Cancel upload of customers.csv']").ClickAsync();
        firstRequestCompletion.TrySetResult(new ImportResultDto(1, 0));
        await firstImport;
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Retry Import").ClickAsync();

        // Assert
        callCount.ShouldBe(2);
        firstToken.IsCancellationRequested.ShouldBeTrue();
        retryToken.CanBeCanceled.ShouldBeTrue();
        retryToken.IsCancellationRequested.ShouldBeFalse();
        retryToken.ShouldNotBe(firstToken);
        cut.Markup.ShouldContain("Import complete", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cancel_while_conflict_states_are_loading_ends_canceled_and_skips_duplicate_resolution()
    {
        // Arrange
        var loadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loadCompletion = new TaskCompletionSource<IReadOnlyList<GetCustomerDto>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        _fakeCustomersApi.SetGetCustomersHandler((_, _) =>
        {
            loadStarted.TrySetResult();
            return loadCompletion.Task;
        });
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.AllCanonicalValues);

        // Act
        var importTask = ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").ClickAsync();
        await loadStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);
        await cut.Find("button[aria-label='Cancel upload of customers.csv']").ClickAsync();
        loadCompletion.TrySetResult([]);
        await importTask;

        // Assert
        cut.Find("[data-upload-status]").TextContent.ShouldContain("Canceled", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("Resolve Duplicates", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Conflict_result_does_not_show_a_completed_upload_for_the_file()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.AllCanonicalValues);

        // Act
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").ClickAsync();

        // Assert
        cut.Markup.ShouldContain("Resolve Duplicates", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("Complete: customers.csv", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("customers.csv complete.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Column_mappings_survive_retry_import_after_a_failed_attempt()
    {
        // Arrange
        var sentFiles = new List<string>();
        _fakeCustomersApi.SetImportCustomersHandler((content, _, progress, _) =>
        {
            sentFiles.Add(Encoding.UTF8.GetString(content));
            if (sentFiles.Count == 1)
            {
                throw new HttpRequestException("Transient failure.");
            }

            progress?.Report(content.LongLength);
            return Task.FromResult(new ImportResultDto(1, 0));
        });
        var cut = Render<ImportCustomers>();
        var csv = CustomerImportCsvTestData.AllCanonicalHeaders + ",Mail\n" + CustomerImportCsvTestData.AllCanonicalValues + ",mapped@example.com";
        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText(csv, "customers.csv"));
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");
        await cut.Find("select[data-field='Email']").ChangeAsync("Mail");
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").ClickAsync();
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Confirm Import");
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").ClickAsync();

        // Act
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Retry Import").ClickAsync();

        // Assert
        sentFiles.Count.ShouldBe(2);
        sentFiles[0].ShouldContain("mapped@example.com", StringComparison.Ordinal);
        sentFiles[1].ShouldBe(sentFiles[0]);
        cut.Markup.ShouldContain("Import complete", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Column_mappings_survive_retry_with_current_file()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(0, 1));
        var cut = Render<ImportCustomers>();
        var csv = CustomerImportCsvTestData.AllCanonicalHeaders + ",Mail\n" + CustomerImportCsvTestData.AllCanonicalValues + ",mapped@example.com";
        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText(csv, "customers.csv"));
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");
        await cut.Find("select[data-field='Email']").ChangeAsync("Mail");
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").ClickAsync();
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Confirm Import");
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").ClickAsync();

        // Act
        await cut.Find("button[data-action='retry-current-file']").ClickAsync();

        // Assert
        var selectedOption = cut.Find("select[data-field='Email']").QuerySelector("option[selected]").ShouldNotBeNull();
        selectedOption.GetAttribute("value").ShouldBe("Mail");
    }
}
