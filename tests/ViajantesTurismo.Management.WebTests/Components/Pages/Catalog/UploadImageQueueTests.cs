using SharedKernel.Blazor;
using SharedKernel.Testing;
using CatalogContractConstants = ViajantesTurismo.Catalog.Contracts.Application.ContractConstants;
using UploadImage = ViajantesTurismo.Management.Web.Components.Pages.Catalog.UploadImage;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

[Trait(TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[TestScope(TestScope.Component)]
public sealed class UploadImageQueueTests : BunitContext
{
    private readonly FakeCatalogToursApiClient catalogApi = new();

    public UploadImageQueueTests()
    {
        Services.AddUiFailureReporting();
        Services.AddSingleton<ICatalogToursApiClient>(catalogApi);
    }

    [Fact]
    public async Task The_shared_picker_explains_first_frame_behavior_before_and_after_selection()
    {
        // Arrange
        const string notice = "The gallery shows only the first frame of animated images.";
        const string sharedHelp = "[data-catalog-image-drop-zone] #imageFileHelp";
        var cut = Render<UploadImage>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        var file = new FakeBrowserFile([0x01], "scenery.webp", "image/webp");
        var picker = cut.FindComponent<InputFile>().Instance;

        // Act
        var before = cut.Find(sharedHelp).TextContent;
        await cut.InvokeAsync(() => picker.OnChange.InvokeAsync(new InputFileChangeEventArgs([file])));
        var after = cut.Find(sharedHelp).TextContent;

        // Assert
        before.ShouldContain(notice, StringComparison.Ordinal);
        after.ShouldContain(notice, StringComparison.Ordinal);
        cut.Find("#imageFile").GetAttribute("aria-describedby").ShouldBe("imageFileHelp");
        cut.Find("#imageFolder").GetAttribute("aria-describedby").ShouldBe("imageFileHelp");
        file.MaximumAllowedSize.ShouldBeNull();
    }

    [Fact]
    public async Task Multiple_selected_images_are_inspectable_and_removable_before_upload()
    {
        // Arrange
        var cut = Render<UploadImage>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        var first = new FakeBrowserFile([0x01], "mountains.jpg", "image/jpeg");
        var second = new FakeBrowserFile([0x01, 0x02], "river.png", "image/png");

        // Act
        await cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([first, second])));

        // Assert
        cut.Find("input[type='file']").HasAttribute("multiple").ShouldBeTrue();
        cut.Find("ol[aria-label='Selected images']").TextContent.ShouldContain("mountains.jpg", StringComparison.Ordinal);
        cut.Find("ol[aria-label='Selected images']").TextContent.ShouldContain("river.png", StringComparison.Ordinal);
        cut.Find("ol[aria-label='Selected images']").TextContent.ShouldContain("2 bytes", StringComparison.Ordinal);

        // Act
        await cut.Find("button[aria-label='Remove river.png']").ClickAsync();

        // Assert
        cut.Find("ol[aria-label='Selected images']").TextContent.ShouldNotContain("river.png", StringComparison.Ordinal);
        catalogApi.UploadTourImageCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Native_selection_accepts_ten_images_and_rejects_an_eleventh_before_opening_any_streams()
    {
        // Arrange
        var cut = Render<UploadImage>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        var picker = cut.FindComponent<InputFile>().Instance;
        var filesAtLimit = Enumerable.Range(1, 10)
            .Select(index => new FakeBrowserFile([0x01], $"image-{index}.png", "image/png"))
            .ToArray();
        var extraFile = new FakeBrowserFile([0x01], "extra.png", "image/png");
        var filesOverLimit = filesAtLimit.Append(extraFile).ToArray();
        var selectedRows = "ol[aria-label='Selected images'] li";

        // Act
        await cut.InvokeAsync(() => picker.OnChange.InvokeAsync(new InputFileChangeEventArgs(filesAtLimit)));
        var countAtLimit = cut.FindAll(selectedRows).Count;
        await cut.InvokeAsync(() => picker.OnChange.InvokeAsync(new InputFileChangeEventArgs(filesOverLimit)));

        // Assert
        countAtLimit.ShouldBe(10);
        cut.FindAll(selectedRows).ShouldBeEmpty();
        cut.Find("[role='alert']").TextContent.ShouldBe("Select up to 10 images at a time.");
        filesAtLimit.ShouldAllSatisfy(file => file.MaximumAllowedSize.ShouldBeNull());
        extraFile.MaximumAllowedSize.ShouldBeNull();
        catalogApi.UploadTourImageCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Queue_sends_individual_descriptions_and_opens_only_one_file_at_a_time()
    {
        // Arrange
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstCompletion = new TaskCompletionSource<CatalogMediaImageDto?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var descriptions = new List<string>();
        catalogApi.UploadTourImageHandler = (_, request, _, _) =>
        {
            descriptions.Add(request.AltText);
            if (descriptions.Count == 1)
            {
                requestStarted.TrySetResult();
                return firstCompletion.Task;
            }

            return Task.FromResult<CatalogMediaImageDto?>(new CatalogMediaImageDto
            {
                Id = Guid.CreateVersion7(),
                AltText = request.AltText,
                ResponsiveVariants = []
            });
        };
        var cut = Render<UploadImage>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        var first = new FakeBrowserFile([0x01], "mountains.jpg", "image/jpeg");
        var second = new FakeBrowserFile([0x01], "river.png", "image/png");
        await cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([first, second])));

        // Act
        await cut.Find("#altText").ChangeAsync("Mountain pass");
        await cut.Find("#altText-1").ChangeAsync("Quiet river");
        var submit = cut.Find("form").SubmitAsync();
        await requestStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);
        var secondStreamWasUnopened = second.MaximumAllowedSize is null;
        firstCompletion.TrySetResult(new CatalogMediaImageDto
        {
            Id = Guid.CreateVersion7(),
            AltText = "Mountain pass",
            ResponsiveVariants = []
        });
        await submit;

        // Assert
        secondStreamWasUnopened.ShouldBeTrue();
        descriptions.ShouldBe(["Mountain pass", "Quiet river"]);
        cut.FindAll("[role='status']").ShouldHaveCount(2);
    }

    [Fact]
    public async Task Canceling_an_active_image_keeps_the_submitted_sibling_uploading_without_another_submit()
    {
        // Arrange
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstCompletion = new TaskCompletionSource<CatalogMediaImageDto?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sent = new List<(string FileName, CancellationToken Token)>();
        var first = new FakeBrowserFile([0x01], "mountains.jpg", "image/jpeg");
        var second = new FakeBrowserFile([0x01], "river.png", "image/png");
        catalogApi.UploadTourImageHandler = (_, request, _, ct) =>
        {
            sent.Add((request.FileName, ct));
            if (request.FileName == first.Name)
            {
                firstStarted.TrySetResult();
                return firstCompletion.Task;
            }

            return Task.FromResult<CatalogMediaImageDto?>(new CatalogMediaImageDto
            {
                Id = Guid.CreateVersion7(),
                AltText = request.AltText,
                ResponsiveVariants = []
            });
        };
        var cut = Render<UploadImage>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        await cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([first, second])));
        await cut.Find("#altText").ChangeAsync("Mountain pass");
        await cut.Find("#altText-1").ChangeAsync("Quiet river");

        // Act
        var submit = cut.Find("form").SubmitAsync();
        await firstStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);
        await cut.Find("button[aria-label='Cancel upload of mountains.jpg']").ClickAsync();
        var secondStreamWasUnopened = second.MaximumAllowedSize is null;
        firstCompletion.TrySetCanceled(sent[0].Token);
        await submit;

        // Assert
        secondStreamWasUnopened.ShouldBeTrue();
        sent.Select(request => request.FileName).ShouldBe([first.Name, second.Name]);
        sent[0].Token.IsCancellationRequested.ShouldBeTrue();
        sent[1].Token.IsCancellationRequested.ShouldBeFalse();
        var rows = cut.FindAll("ol[aria-label='Selected images'] li");
        rows[0].QuerySelector("[data-upload-status]").ShouldNotBeNull().TextContent
            .ShouldBe("Canceled: mountains.jpg. Retry when ready.");
        rows[1].QuerySelector("[role='status']").ShouldNotBeNull().TextContent
            .ShouldBe("Image uploaded and awaiting processing.");
    }

    [Fact]
    public async Task Leaving_the_page_cancels_the_active_image_and_does_not_start_queued_images()
    {
        // Arrange
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstCompletion = new TaskCompletionSource<CatalogMediaImageDto?>(TaskCreationOptions.RunContinuationsAsynchronously);
        catalogApi.UploadTourImageHandler = (_, _, _, _) =>
        {
            firstStarted.TrySetResult();
            return firstCompletion.Task;
        };
        var cut = Render<UploadImage>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        var first = new FakeBrowserFile([0x01], "mountains.jpg", "image/jpeg");
        var second = new FakeBrowserFile([0x01], "river.png", "image/png");
        await cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([first, second])));
        await cut.Find("#altText").ChangeAsync("Mountain pass");
        await cut.Find("#altText-1").ChangeAsync("Quiet river");
        var submit = cut.Find("form").SubmitAsync();
        await firstStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);
        var requestToken = catalogApi.LastUploadCancellationToken;

        // Act
        await DisposeComponentsAsync();
        firstCompletion.TrySetCanceled(requestToken);
        await submit;

        // Assert
        requestToken.IsCancellationRequested.ShouldBeTrue();
        second.MaximumAllowedSize.ShouldBeNull();
        catalogApi.UploadTourImageCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Invalid_files_get_inline_guidance_and_only_the_valid_file_reaches_the_client()
    {
        // Arrange
        var cut = Render<UploadImage>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        var tooLarge = new FakeBrowserFile([0x01], "large.jpg", "image/jpeg")
        {
            DeclaredSize = CatalogContractConstants.MaxMediaUploadBytes + 1
        };
        var wrongType = new FakeBrowserFile([0x01], "notes.txt", "text/plain");
        var valid = new FakeBrowserFile([0x01], "river.webp", "image/webp");
        catalogApi.UploadedImage = new CatalogMediaImageDto
        {
            Id = Guid.CreateVersion7(),
            AltText = "Quiet river",
            ResponsiveVariants = []
        };

        // Act
        await cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([tooLarge, wrongType, valid])));
        await cut.Find("input[name='imageDescription']").ChangeAsync("Quiet river");
        await cut.Find("form").SubmitAsync();

        // Assert
        var guidance = cut.FindAll("[role='alert']").Select(alert => alert.TextContent).ToArray();
        guidance.ShouldHaveCount(2);
        guidance[0].ShouldContain("no larger than", StringComparison.Ordinal);
        guidance[1].ShouldContain("JPEG, PNG, WebP, or AVIF", StringComparison.Ordinal);
        tooLarge.MaximumAllowedSize.ShouldBeNull();
        wrongType.MaximumAllowedSize.ShouldBeNull();
        catalogApi.UploadTourImageCallCount.ShouldBe(1);
        catalogApi.LastUploadedFileName.ShouldBe(valid.Name);
    }

    [Fact]
    public async Task Retrying_a_failed_image_does_not_upload_the_successful_sibling_again()
    {
        // Arrange
        var cut = Render<UploadImage>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        var first = new FakeBrowserFile([0x01], "mountains.jpg", "image/jpeg");
        var second = new FakeBrowserFile([0x01], "river.png", "image/png");
        var sent = new List<string>();
        catalogApi.UploadTourImageHandler = (_, request, _, _) =>
        {
            sent.Add(request.FileName);
            if (sent.Count == 1)
            {
                throw new HttpRequestException("Catalog unavailable.");
            }

            return Task.FromResult<CatalogMediaImageDto?>(new CatalogMediaImageDto
            {
                Id = Guid.CreateVersion7(),
                AltText = request.AltText,
                ResponsiveVariants = []
            });
        };
        await cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([first, second])));
        await cut.Find("#altText").ChangeAsync("Mountain pass");
        await cut.Find("#altText-1").ChangeAsync("Quiet river");

        // Act
        await cut.Find("form").SubmitAsync();
        var shownError = cut.Find("[role='alert']").TextContent;
        await cut.Find("button[aria-label='Retry upload of mountains.jpg']").ClickAsync();

        // Assert
        shownError.ShouldContain("Image could not be uploaded", StringComparison.Ordinal);
        sent.ShouldBe([first.Name, second.Name, first.Name]);
        cut.FindAll("[role='alert']").ShouldBeEmpty();
        cut.FindAll("[role='status']").ShouldHaveCount(2);
    }
}
