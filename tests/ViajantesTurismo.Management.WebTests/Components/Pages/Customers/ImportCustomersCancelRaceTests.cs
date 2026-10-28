using PandaBear.Testing;
using ViajantesTurismo.Management.WebTests.Infrastructure;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[TestScope(TestScope.Component)]
public sealed class ImportCustomersCancelRaceTests : BunitContext
{
    private const string CanceledStatus = "Canceled: customers.csv. Confirm the import to retry.";

    private static readonly string Csv = CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.AllCanonicalValues;

    private readonly FakeCustomersApiClient fakeCustomersApi = new();
    private readonly RecordingUiFailureReporter failures = new();

    public ImportCustomersCancelRaceTests()
    {
        Services.AddSingleton<PandaBear.Blazor.IUiFailureReporter>(failures);
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICustomersApiClient>(fakeCustomersApi);
    }

    [Fact]
    public async Task Cancel_followed_by_the_import_request_observing_cancellation_shows_only_the_canceled_status()
    {
        // Arrange
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCompletion = new TaskCompletionSource<ImportResultDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestToken = CancellationToken.None;
        fakeCustomersApi.SetImportCustomersHandler((_, _, _, ct) =>
        {
            requestToken = ct;
            requestStarted.TrySetResult();
            return requestCompletion.Task;
        });
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, Csv);
        var importTask = ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").ClickAsync();
        await requestStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);

        // Act
        await cut.Find("button[aria-label='Cancel upload of customers.csv']").ClickAsync();
        requestCompletion.TrySetCanceled(requestToken);
        await importTask;

        // Assert
        cut.Find("[data-upload-status]").TextContent.ShouldBe(CanceledStatus);
        cut.FindAll(".alert-danger").ShouldBeEmpty();
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Retry Import").HasAttribute("disabled").ShouldBeFalse();
        failures.Reports.ShouldBeEmpty();
    }

    [Fact]
    public async Task Cancel_followed_by_an_import_failure_reports_it_but_keeps_the_canceled_status()
    {
        // Arrange
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCompletion = new TaskCompletionSource<ImportResultDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        fakeCustomersApi.SetImportCustomersHandler((_, _, _, _) =>
        {
            requestStarted.TrySetResult();
            return requestCompletion.Task;
        });
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, Csv);
        var importTask = ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").ClickAsync();
        await requestStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);

        // Act
        await cut.Find("button[aria-label='Cancel upload of customers.csv']").ClickAsync();
        requestCompletion.TrySetException(new InvalidOperationException("Connection refused by 10.0.0.7"));
        await importTask;

        // Assert
        cut.Find("[data-upload-status]").TextContent.ShouldBe(CanceledStatus);
        cut.Markup.ShouldNotContain("10.0.0.7", StringComparison.Ordinal);
        failures.Reports.ShouldBe([(nameof(InvalidOperationException), "customers.import.upload")]);
    }

    [Fact]
    public async Task Cancel_followed_by_the_commit_request_observing_cancellation_shows_only_the_canceled_status()
    {
        // Arrange
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCompletion = new TaskCompletionSource<ImportResultDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestToken = CancellationToken.None;
        fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        fakeCustomersApi.SetCommitImportHandler((_, _, _, _, ct) =>
        {
            requestToken = ct;
            requestStarted.TrySetResult();
            return requestCompletion.Task;
        });
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, Csv);
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").ClickAsync();
        await cut.Find("button[data-action='keep']").ClickAsync();
        var commitTask = cut.Find("button[data-action='confirm-import']").ClickAsync();
        await requestStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);

        // Act
        await cut.Find("button[aria-label='Cancel upload of customers.csv']").ClickAsync();
        requestCompletion.TrySetCanceled(requestToken);
        await commitTask;

        // Assert
        cut.Find("[data-upload-status]").TextContent.ShouldBe(CanceledStatus);
        cut.FindAll(".alert-danger").ShouldBeEmpty();
        failures.Reports.ShouldBeEmpty();
    }

    [Fact]
    public async Task Cancel_followed_by_a_commit_failure_reports_it_but_keeps_the_canceled_status()
    {
        // Arrange
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCompletion = new TaskCompletionSource<ImportResultDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        fakeCustomersApi.SetCommitImportHandler((_, _, _, _, _) =>
        {
            requestStarted.TrySetResult();
            return requestCompletion.Task;
        });
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, Csv);
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").ClickAsync();
        await cut.Find("button[data-action='keep']").ClickAsync();
        var commitTask = cut.Find("button[data-action='confirm-import']").ClickAsync();
        await requestStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);

        // Act
        await cut.Find("button[aria-label='Cancel upload of customers.csv']").ClickAsync();
        requestCompletion.TrySetException(new InvalidOperationException("Connection refused by 10.0.0.7"));
        await commitTask;

        // Assert
        cut.Find("[data-upload-status]").TextContent.ShouldBe(CanceledStatus);
        cut.Markup.ShouldNotContain("10.0.0.7", StringComparison.Ordinal);
        failures.Reports.ShouldBe([(nameof(InvalidOperationException), "customers.import.complete")]);
    }

    [Fact]
    public async Task Retrying_a_failed_import_shows_the_retrying_status_while_the_new_request_is_in_flight()
    {
        // Arrange
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCompletion = new TaskCompletionSource<ImportResultDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callCount = 0;
        fakeCustomersApi.SetImportCustomersHandler((_, _, _, _) =>
        {
            if (Interlocked.Increment(ref callCount) == 1)
            {
                throw new InvalidOperationException("Connection refused by 10.0.0.7");
            }

            requestStarted.TrySetResult();
            return requestCompletion.Task;
        });
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, Csv);
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").ClickAsync();

        // Act
        var retryTask = ImportCustomersTestDomHelper.FindButtonByText(cut, "Retry Import").ClickAsync();
        await requestStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);

        // Assert
        cut.Find("[data-upload-status]").TextContent.ShouldBe("Retrying: customers.csv");
        requestCompletion.TrySetResult(new ImportResultDto(1, 0));
        await retryTask;
    }

    [Fact]
    public async Task Going_back_from_duplicate_resolution_returns_to_the_preview()
    {
        // Arrange
        fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, Csv);
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").ClickAsync();
        cut.Markup.ShouldContain("Resolve Duplicates", StringComparison.Ordinal);

        // Act
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Back to Preview").ClickAsync();

        // Assert
        cut.Markup.ShouldNotContain("Resolve Duplicates", StringComparison.Ordinal);
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public async Task Leaving_the_page_during_an_import_cancels_the_request()
    {
        // Arrange
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCompletion = new TaskCompletionSource<ImportResultDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        fakeCustomersApi.SetImportCustomersHandler((_, _, _, _) =>
        {
            requestStarted.TrySetResult();
            return requestCompletion.Task;
        });
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, Csv);
        var importTask = ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").ClickAsync();
        await requestStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);

        // Act
        await DisposeComponentsAsync();
        requestCompletion.TrySetResult(new ImportResultDto(1, 0));
        await importTask;

        // Assert
        fakeCustomersApi.LastImportCancellationToken.IsCancellationRequested.ShouldBeTrue();
        failures.Reports.ShouldBeEmpty();
    }
}
