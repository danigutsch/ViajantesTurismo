using PandaBear.Blazor;
using PandaBear.HttpClients;
using ViajantesTurismo.Management.WebTests.Infrastructure;
using UploadImage = ViajantesTurismo.Management.Web.Components.Pages.Catalog.UploadImage;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
public sealed class UploadImageFailureRaceTests : BunitContext
{
    private const string CanceledStatus = "Canceled: tour-image.jpg. Retry when ready.";

    private readonly FakeCatalogToursApiClient catalogApi = new();
    private readonly RecordingUiFailureReporter failures = new();

    public UploadImageFailureRaceTests()
    {
        Services.AddUiFailureReporting();
        Services.AddSingleton<IUiFailureReporter>(failures);
        Services.AddSingleton<ICatalogToursApiClient>(catalogApi);
    }

    [Fact]
    public async Task Submitting_without_a_selected_image_asks_for_one_and_starts_no_upload()
    {
        // Arrange
        var cut = Render<UploadImage>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        await cut.Find("#altText").ChangeAsync("Cyclists on a mountain pass.");

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Find("[role='alert']").TextContent.ShouldBe("Select an image file before uploading.");
        catalogApi.UploadTourImageCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Cancel_followed_by_the_request_observing_cancellation_shows_only_the_canceled_status()
    {
        // Arrange
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCompletion = new TaskCompletionSource<CatalogMediaImageDto?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestToken = CancellationToken.None;
        catalogApi.UploadTourImageHandler = (_, _, _, ct) =>
        {
            requestToken = ct;
            requestStarted.TrySetResult();
            return requestCompletion.Task;
        };
        var cut = Render<UploadImage>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        await cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([new RecordingBrowserFile()])));
        await cut.Find("#altText").ChangeAsync("Cyclists on a mountain pass.");
        var uploadTask = cut.Find("form").SubmitAsync();
        await requestStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);

        // Act
        await cut.Find("button[aria-label='Cancel upload of tour-image.jpg']").ClickAsync();
        requestCompletion.TrySetCanceled(requestToken);
        await uploadTask;

        // Assert
        cut.Find("[data-upload-status]").TextContent.ShouldBe(CanceledStatus);
        cut.FindAll("[role='alert']").ShouldBeEmpty();
        cut.Find("button[type='submit']").TextContent.Trim().ShouldBe("Retry upload");
        failures.Reports.ShouldBeEmpty();
    }

    [Fact]
    public async Task Cancel_followed_by_a_validation_failure_does_not_replace_the_canceled_status()
    {
        // Arrange
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCompletion = new TaskCompletionSource<CatalogMediaImageDto?>(TaskCreationOptions.RunContinuationsAsynchronously);
        catalogApi.UploadTourImageHandler = (_, _, _, _) =>
        {
            requestStarted.TrySetResult();
            return requestCompletion.Task;
        };
        var cut = Render<UploadImage>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        await cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([new RecordingBrowserFile()])));
        await cut.Find("#altText").ChangeAsync("Cyclists on a mountain pass.");
        var uploadTask = cut.Find("form").SubmitAsync();
        await requestStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);

        // Act
        await cut.Find("button[aria-label='Cancel upload of tour-image.jpg']").ClickAsync();
        requestCompletion.TrySetException(new ContractValidationException(
            "Validation failed",
            new Dictionary<string, string[]> { ["file"] = ["Unsupported."] }));
        await uploadTask;

        // Assert
        cut.Find("[data-upload-status]").TextContent.ShouldBe(CanceledStatus);
        cut.FindAll("[role='alert']").ShouldBeEmpty();
    }

    [Fact]
    public async Task Cancel_followed_by_a_transport_failure_reports_the_send_fault_without_showing_an_error()
    {
        // Arrange
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCompletion = new TaskCompletionSource<CatalogMediaImageDto?>(TaskCreationOptions.RunContinuationsAsynchronously);
        catalogApi.UploadTourImageHandler = (_, _, _, _) =>
        {
            requestStarted.TrySetResult();
            return requestCompletion.Task;
        };
        var cut = Render<UploadImage>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        await cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([new RecordingBrowserFile()])));
        await cut.Find("#altText").ChangeAsync("Cyclists on a mountain pass.");
        var uploadTask = cut.Find("form").SubmitAsync();
        await requestStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);

        // Act
        await cut.Find("button[aria-label='Cancel upload of tour-image.jpg']").ClickAsync();
        requestCompletion.TrySetException(new HttpRequestException("Connection reset."));
        await uploadTask;

        // Assert
        cut.Find("[data-upload-status]").TextContent.ShouldBe(CanceledStatus);
        cut.FindAll("[role='alert']").ShouldBeEmpty();
        failures.Reports.ShouldBe([(nameof(HttpRequestException), "catalog.upload-image.send")]);
    }

    [Fact]
    public async Task Cancel_followed_by_a_read_failure_reports_the_read_fault_without_showing_an_error()
    {
        // Arrange
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCompletion = new TaskCompletionSource<CatalogMediaImageDto?>(TaskCreationOptions.RunContinuationsAsynchronously);
        catalogApi.UploadTourImageHandler = (_, _, _, _) =>
        {
            requestStarted.TrySetResult();
            return requestCompletion.Task;
        };
        var cut = Render<UploadImage>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        await cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([new RecordingBrowserFile()])));
        await cut.Find("#altText").ChangeAsync("Cyclists on a mountain pass.");
        var uploadTask = cut.Find("form").SubmitAsync();
        await requestStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);

        // Act
        await cut.Find("button[aria-label='Cancel upload of tour-image.jpg']").ClickAsync();
        requestCompletion.TrySetException(new IOException("Stream closed."));
        await uploadTask;

        // Assert
        cut.Find("[data-upload-status]").TextContent.ShouldBe(CanceledStatus);
        cut.FindAll("[role='alert']").ShouldBeEmpty();
        failures.Reports.ShouldBe([(nameof(IOException), "catalog.upload-image.read")]);
    }

    [Fact]
    public async Task Retrying_after_a_failure_shows_the_retrying_status_while_the_new_request_is_in_flight()
    {
        // Arrange
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCompletion = new TaskCompletionSource<CatalogMediaImageDto?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callCount = 0;
        catalogApi.UploadTourImageHandler = (_, _, _, _) =>
        {
            if (Interlocked.Increment(ref callCount) == 1)
            {
                throw new HttpRequestException("Catalog unavailable.");
            }

            requestStarted.TrySetResult();
            return requestCompletion.Task;
        };
        var cut = Render<UploadImage>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        await cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([new RecordingBrowserFile()])));
        await cut.Find("#altText").ChangeAsync("Cyclists on a mountain pass.");
        await cut.Find("form").SubmitAsync();

        // Act
        var retryTask = cut.Find("form").SubmitAsync();
        await requestStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);

        // Assert
        cut.Find("[data-upload-status]").TextContent.ShouldBe("Retrying: tour-image.jpg");
        cut.Find("button[type='submit']").TextContent.Trim().ShouldBe("Uploading...");
        requestCompletion.TrySetResult(null);
        await retryTask;
    }

    [Fact]
    public async Task Leaving_the_page_during_an_upload_cancels_the_request()
    {
        // Arrange
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCompletion = new TaskCompletionSource<CatalogMediaImageDto?>(TaskCreationOptions.RunContinuationsAsynchronously);
        catalogApi.UploadTourImageHandler = (_, _, _, _) =>
        {
            requestStarted.TrySetResult();
            return requestCompletion.Task;
        };
        var cut = Render<UploadImage>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        await cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([new RecordingBrowserFile()])));
        await cut.Find("#altText").ChangeAsync("Cyclists on a mountain pass.");
        var uploadTask = cut.Find("form").SubmitAsync();
        await requestStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);

        // Act
        await DisposeComponentsAsync();
        requestCompletion.TrySetResult(null);
        await uploadTask;

        // Assert
        catalogApi.LastUploadCancellationToken.IsCancellationRequested.ShouldBeTrue();
        failures.Reports.ShouldBeEmpty();
    }
}
