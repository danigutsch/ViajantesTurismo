using System.Globalization;
using CatalogContractConstants = ViajantesTurismo.Catalog.Contracts.Application.ContractConstants;
using CatalogImageUploadFileReader = ViajantesTurismo.Management.Web.Components.Pages.Catalog.CatalogImageUploadFileReader;
using UploadImage = ViajantesTurismo.Management.Web.Components.Pages.Catalog.UploadImage;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
public sealed class UploadImagePageTests : BunitContext
{
    private readonly FakeCatalogToursApiClient catalogApi = new();

    public UploadImagePageTests()
    {
        Services.AddSingleton<ICatalogToursApiClient>(catalogApi);
    }

    [Fact]
    public void Displays_the_shared_catalog_upload_limit()
    {
        // Arrange
        var expectedLimitInMebibytes = CatalogContractConstants.MaxMediaUploadBytes / (1024 * 1024);

        // Act
        var cut = Render<UploadImage>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));

        // Assert
        cut.Markup.ShouldContain($"Maximum size: {expectedLimitInMebibytes} MiB.", StringComparison.Ordinal);
    }

    [Fact]
    public void Opens_browser_file_reads_with_the_shared_catalog_upload_limit()
    {
        // Arrange
        var file = new RecordingBrowserFile();

        // Act
        using var stream = CatalogImageUploadFileReader.Open(file);

        // Assert
        file.MaximumAllowedSize.ShouldBe(CatalogContractConstants.MaxMediaUploadBytes);
    }

    [Fact]
    public async Task Uploads_selected_image_and_displays_processing_confirmation()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var file = new RecordingBrowserFile();
        catalogApi.UploadedImage = new CatalogMediaImageDto
        {
            Id = Guid.CreateVersion7(),
            AltText = "Cyclists on a mountain pass.",
            ResponsiveVariants = []
        };
        var cut = Render<UploadImage>(parameters => parameters.Add(component => component.Id, tourId));
        var fileInput = cut.FindComponent<InputFile>();

        // Act
        await cut.InvokeAsync(() => fileInput.Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([file])));
        await cut.Find("#altText").ChangeAsync("Cyclists on a mountain pass.");
        await cut.Find("#caption").ChangeAsync("Mountain pass");
        await cut.Find("form").SubmitAsync();
        await cut.WaitForStateAsync(() => cut.FindAll("[role='status']").Count == 1, TimeSpan.FromSeconds(2));

        // Assert
        catalogApi.LastUploadedTourId.ShouldBe(tourId);
        catalogApi.LastUploadedFileName.ShouldBe(file.Name);
        catalogApi.LastUploadedContentType.ShouldBe(file.ContentType);
        catalogApi.LastUploadedAltText.ShouldBe("Cyclists on a mountain pass.");
        catalogApi.LastUploadedCaption.ShouldBe("Mountain pass");
        cut.Find("[role='status']").TextContent.ShouldBe("Image uploaded and awaiting processing.");
        cut.FindAll("[role='alert']").ShouldBeEmpty();
        cut.Find("button[type='submit']").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public async Task Cancel_targets_the_selected_image_and_late_completion_cannot_replace_canceled_state()
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
        var file = new RecordingBrowserFile();
        await cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([file])));
        await cut.Find("#altText").ChangeAsync("Cyclists on a mountain pass.");

        // Act
        var uploadTask = cut.Find("form").SubmitAsync();
        await requestStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);
        var cancelButton = cut.Find("button[aria-label='Cancel upload of tour-image.jpg']");
        cancelButton.TagName.ShouldBe("BUTTON");
        cancelButton.GetAttribute("type").ShouldBe("button");
        await cancelButton.ClickAsync();
        requestCompletion.TrySetResult(new CatalogMediaImageDto
        {
            Id = Guid.CreateVersion7(),
            AltText = "Cyclists on a mountain pass.",
            ResponsiveVariants = []
        });
        await uploadTask;

        // Assert
        catalogApi.LastUploadCancellationToken.IsCancellationRequested.ShouldBeTrue();
        cut.Find("[data-upload-status]").TextContent.ShouldContain("Canceled", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("Image uploaded and awaiting processing.", StringComparison.Ordinal);
        cut.Find("#altText").GetAttribute("value").ShouldBe("Cyclists on a mountain pass.");
    }

    [Fact]
    public async Task Empty_image_is_rejected_without_starting_an_upload()
    {
        // Arrange
        var cut = Render<UploadImage>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        var file = new RecordingBrowserFile([]);
        await cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([file])));
        await cut.Find("#altText").ChangeAsync("Cyclists on a mountain pass.");

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Find("[role='alert']").TextContent.ShouldContain("non-empty image file", StringComparison.Ordinal);
        catalogApi.UploadTourImageCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Canceled_upload_failure_cannot_replace_a_successful_retry()
    {
        // Arrange
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var oldRequestCompletion = new TaskCompletionSource<CatalogMediaImageDto?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callCount = 0;
        catalogApi.UploadTourImageHandler = (_, request, progress, _) =>
        {
            if (Interlocked.Increment(ref callCount) == 1)
            {
                requestStarted.TrySetResult();
                return oldRequestCompletion.Task;
            }

            progress?.Report(request.ContentLength);
            return Task.FromResult<CatalogMediaImageDto?>(new CatalogMediaImageDto
            {
                Id = Guid.CreateVersion7(),
                AltText = request.AltText,
                ResponsiveVariants = []
            });
        };
        var cut = Render<UploadImage>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        var file = new RecordingBrowserFile();
        await cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([file])));
        await cut.Find("#altText").ChangeAsync("Cyclists on a mountain pass.");

        // Act
        var oldUpload = cut.Find("form").SubmitAsync();
        await requestStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);
        await cut.Find("button[aria-label='Cancel upload of tour-image.jpg']").ClickAsync();
        await cut.Find("form").SubmitAsync();
        oldRequestCompletion.TrySetException(new HttpRequestException("Late failure."));
        await oldUpload;

        // Assert
        callCount.ShouldBe(2);
        cut.Find("[role='status']").TextContent.ShouldContain("Image uploaded and awaiting processing", StringComparison.Ordinal);
        cut.FindAll("[role='alert']").ShouldBeEmpty();
    }

    [Fact]
    public async Task Displays_authoritative_transport_bytes_then_indeterminate_server_phases_with_polite_announcements()
    {
        // Arrange
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCompletion = new TaskCompletionSource<CatalogMediaImageDto?>(TaskCreationOptions.RunContinuationsAsynchronously);
        IProgress<long>? reportedProgress = null;
        catalogApi.UploadTourImageHandler = (_, _, progress, _) =>
        {
            reportedProgress = progress;
            requestStarted.TrySetResult();
            return requestCompletion.Task;
        };
        var cut = Render<UploadImage>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        var file = new RecordingBrowserFile(new byte[100]);
        await cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([file])));
        await cut.Find("#altText").ChangeAsync("Cyclists on a mountain pass.");

        // Act
        var uploadTask = cut.Find("form").SubmitAsync();
        await requestStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);
        var progress = reportedProgress.ShouldNotBeNull();
        progress.Report(25);

        // Assert
        await cut.WaitForAssertionAsync(() => cut.Find("progress[value]").GetAttribute("value").ShouldBe(25L.ToString(CultureInfo.InvariantCulture)));
        cut.Find("progress[value]").GetAttribute("max").ShouldBe(file.Size.ToString(CultureInfo.InvariantCulture));
        var liveRegion = cut.Find("[role='status'][aria-live='polite']");
        liveRegion.GetAttribute("aria-atomic").ShouldBe("true");

        // Act
        progress.Report(file.Size);

        // Assert
        await cut.WaitForAssertionAsync(() => cut.Find("progress:not([value])").ShouldNotBeNull());
        cut.Find("[data-upload-status]").TextContent.ShouldContain("scanning, validating, decoding", StringComparison.Ordinal);
        cut.Find("[data-upload-status]").TextContent.ShouldContain("Progress is indeterminate", StringComparison.Ordinal);

        requestCompletion.TrySetResult(new CatalogMediaImageDto
        {
            Id = Guid.CreateVersion7(),
            AltText = "Cyclists on a mountain pass.",
            ResponsiveVariants = []
        });
        await uploadTask;
    }

    [Fact]
    public async Task Retry_preserves_form_values_and_uses_a_new_cancellation_token()
    {
        // Arrange
        var callCount = 0;
        var firstToken = CancellationToken.None;
        var retryToken = CancellationToken.None;
        catalogApi.UploadTourImageHandler = (_, request, progress, ct) =>
        {
            if (Interlocked.Increment(ref callCount) == 1)
            {
                firstToken = ct;
                throw new HttpRequestException("Catalog unavailable.");
            }

            retryToken = ct;
            progress?.Report(request.ContentLength);
            return Task.FromResult<CatalogMediaImageDto?>(new CatalogMediaImageDto
            {
                Id = Guid.CreateVersion7(),
                AltText = request.AltText,
                ResponsiveVariants = []
            });
        };
        var cut = Render<UploadImage>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        var file = new RecordingBrowserFile();
        await cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([file])));
        await cut.Find("#altText").ChangeAsync("Cyclists on a mountain pass.");
        await cut.Find("#caption").ChangeAsync("Mountain pass");

        // Act
        await cut.Find("form").SubmitAsync();
        cut.Find("button[type='submit']").TextContent.ShouldContain("Retry upload", StringComparison.Ordinal);
        await cut.Find("form").SubmitAsync();

        // Assert
        callCount.ShouldBe(2);
        firstToken.CanBeCanceled.ShouldBeTrue();
        retryToken.CanBeCanceled.ShouldBeTrue();
        retryToken.ShouldNotBe(firstToken);
        cut.Find("#altText").GetAttribute("value").ShouldBe("Cyclists on a mountain pass.");
        cut.Find("#caption").GetAttribute("value").ShouldBe("Mountain pass");
        cut.Find("[role='status']").TextContent.ShouldContain("Image uploaded and awaiting processing", StringComparison.Ordinal);
    }
}
