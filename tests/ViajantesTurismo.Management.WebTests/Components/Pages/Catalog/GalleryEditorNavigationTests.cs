using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SharedKernel.Blazor;
using SharedKernel.HttpClients;
using SharedKernel.Testing;
using ViajantesTurismo.Management.Web.Components.Pages.Catalog;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

[Trait(TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[Trait(TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
[TestScope(TestScope.Component)]
public sealed class GalleryEditorNavigationTests : BunitContext
{
    private readonly FakeGalleryEditorApiClient catalog = new();

    public GalleryEditorNavigationTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddRouting();
        Services.AddUiFailureReporting();
        Services.AddSingleton<ICatalogGalleryEditorApiClient>(catalog);
        SetRendererInfo(new RendererInfo("Server", true));
    }

    [Fact]
    public async Task Earlier_gallery_read_cannot_replace_a_return_visit_to_the_same_tour()
    {
        // Arrange
        var ct = Xunit.TestContext.Current.CancellationToken;
        var tour = Guid.CreateVersion7();
        var otherTour = Guid.CreateVersion7();
        var tourParameters = ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(EditGallery.Id)] = tour });
        var otherParameters = ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(EditGallery.Id)] = otherTour });
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var response = new TaskCompletionSource<HttpOutcome<CatalogGalleryEditorStateDto>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var visits = 0;
        catalog.StateHandler = id =>
        {
            if (id == tour && ++visits == 1)
            {
                started.TrySetResult();
                return response.Task;
            }

            var state = id == tour
                ? FakeGalleryEditorApiClient.LegacyState with
                {
                    IsRetired = true
                }
                : FakeGalleryEditorApiClient.LegacyState;
            return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, state));
        };
        var cut = Render<EditGallery>(parameters => parameters.Add(page => page.Id, otherTour));

        // Act
        var pending = cut.InvokeAsync(() => cut.Instance.SetParametersAsync(tourParameters));
        await FakeGalleryEditorApiClient.ReleaseAfter(async () =>
        {
            await started.Task.WaitAsync(ct);
            await cut.InvokeAsync(() => cut.Instance.SetParametersAsync(otherParameters));
            await cut.InvokeAsync(() => cut.Instance.SetParametersAsync(tourParameters));
        }, () => response.TrySetResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, FakeGalleryEditorApiClient.LegacyState)));
        await pending;

        // Assert
        cut.Markup.ShouldContain("This tour has been retired.", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("Initialize this gallery", StringComparison.Ordinal);
        var reads = catalog.StateReads.Select(read => read.TourId).ToArray();
        reads.ShouldBe([otherTour, tour, otherTour, tour]);
    }

    [Fact]
    public async Task An_earlier_initialization_cannot_block_or_reload_a_new_tour_visit()
    {
        // Arrange
        var ct = Xunit.TestContext.Current.CancellationToken;
        var tour = Guid.CreateVersion7();
        var otherTour = Guid.CreateVersion7();
        var tourParameters = ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(EditGallery.Id)] = tour });
        var otherParameters = ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(EditGallery.Id)] = otherTour });
        var started = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        var response = new TaskCompletionSource<HttpOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        catalog.InitializeHandler = id =>
        {
            started.TrySetResult(id);
            return response.Task;
        };
        var cut = Render<EditGallery>(parameters => parameters.Add(page => page.Id, tour));
        var initialize = cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Initialize gallery");

        // Act
        var pending = initialize.ClickAsync();
        var otherVisitWasInteractive = false;
        await FakeGalleryEditorApiClient.ReleaseAfter(async () =>
        {
            var requestedTour = await started.Task.WaitAsync(ct);
            requestedTour.ShouldBe(tour);
            await cut.InvokeAsync(() => cut.Instance.SetParametersAsync(otherParameters));
            var nextInitialize = cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Initialize gallery");
            otherVisitWasInteractive = !nextInitialize.HasAttribute("disabled");
            await cut.InvokeAsync(() => cut.Instance.SetParametersAsync(tourParameters));
        }, () => response.TrySetResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.NoContent, location: null)));
        await pending;

        // Assert
        var reads = catalog.StateReads.Select(read => read.TourId).ToArray();
        reads.ShouldBe([tour, otherTour, tour]);
        otherVisitWasInteractive.ShouldBeTrue();
    }

    [Fact]
    public async Task An_earlier_placement_cannot_show_its_outcome_on_a_return_visit()
    {
        // Arrange
        var ct = Xunit.TestContext.Current.CancellationToken;
        var tour = Guid.CreateVersion7();
        var otherTour = Guid.CreateVersion7();
        var tourParameters = ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(EditGallery.Id)] = tour });
        var otherParameters = ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(EditGallery.Id)] = otherTour });
        var started = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        var response = new TaskCompletionSource<HttpOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        var state = FakeGalleryEditorApiClient.LegacyState with
        {
            PhotoIds = [Guid.CreateVersion7()]
        };
        catalog.StateHandler = _ => Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, state));
        catalog.PlacementHandler = id =>
        {
            started.TrySetResult(id);
            return response.Task;
        };
        var cut = Render<EditGallery>(parameters => parameters.Add(page => page.Id, tour));
        await cut.Find("button[aria-label='Remove Gallery photo']").ClickAsync();
        var save = cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Save gallery");

        // Act
        var pending = save.ClickAsync();
        await FakeGalleryEditorApiClient.ReleaseAfter(async () =>
        {
            var requestedTour = await started.Task.WaitAsync(ct);
            requestedTour.ShouldBe(tour);
            await cut.InvokeAsync(() => cut.Instance.SetParametersAsync(otherParameters));
            await cut.InvokeAsync(() => cut.Instance.SetParametersAsync(tourParameters));
        }, () => response.TrySetResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.Accepted, new Uri("?placementSequence=7", UriKind.Relative))));
        await pending;

        // Assert
        var reads = catalog.StateReads.ToArray();
        reads.ShouldBe([(tour, null), (otherTour, null), (tour, null)]);
        cut.Markup.ShouldNotContain("The gallery change was accepted.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Earlier_photo_choices_cannot_replace_the_return_visit_selection()
    {
        // Arrange
        var ct = Xunit.TestContext.Current.CancellationToken;
        var tour = Guid.CreateVersion7();
        var otherTour = Guid.CreateVersion7();
        var stalePhoto = Guid.CreateVersion7();
        var currentPhoto = Guid.CreateVersion7();
        var tourParameters = ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(EditGallery.Id)] = tour });
        var otherParameters = ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(EditGallery.Id)] = otherTour });
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var response = new TaskCompletionSource<HttpOutcome<CatalogGalleryPhotoOptionsDto>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var visits = 0;
        catalog.StateHandler = id => Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, id != tour
            ? FakeGalleryEditorApiClient.LegacyState
            : FakeGalleryEditorApiClient.LegacyState with
            {
                PhotoIds = [++visits == 1 ? stalePhoto : currentPhoto]
            }));
        catalog.PhotoHandler = request =>
        {
            if (request.IncludedPhotoIds?.Contains(stalePhoto) == true)
            {
                started.TrySetResult();
                return response.Task;
            }

            return FakeGalleryEditorApiClient.CreatePhotoPage(request);
        };
        var cut = Render<EditGallery>(parameters => parameters.Add(page => page.Id, otherTour));

        // Act
        var pending = cut.InvokeAsync(() => cut.Instance.SetParametersAsync(tourParameters));
        await FakeGalleryEditorApiClient.ReleaseAfter(async () =>
        {
            await started.Task.WaitAsync(ct);
            await cut.InvokeAsync(() => cut.Instance.SetParametersAsync(otherParameters));
            await cut.InvokeAsync(() => cut.Instance.SetParametersAsync(tourParameters));
        }, () => response.TrySetResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK,
            new CatalogGalleryPhotoOptionsDto([new(stalePhoto, "Earlier photo", null, null)], 1))));
        await pending;

        // Assert
        cut.Find("button[aria-label='Remove Gallery photo']").ShouldNotBeNull();
        cut.Markup.ShouldNotContain("Earlier photo", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Current_visit_writes_reload_the_current_editor(bool savePlacement)
    {
        // Arrange
        var tour = Guid.CreateVersion7();
        var authored = FakeGalleryEditorApiClient.LegacyState with
        {
            PhotoIds = [Guid.CreateVersion7()]
        };
        catalog.StateHandler = _ => Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK,
            savePlacement || catalog.StateReads.Count > 1 ? authored : FakeGalleryEditorApiClient.LegacyState));
        catalog.PlacementHandler = _ => Task.FromResult(HttpOutcomeMapper.Succeeded(
            HttpStatusCode.Accepted, new Uri("?placementSequence=7", UriKind.Relative)));
        var cut = Render<EditGallery>(parameters => parameters.Add(page => page.Id, tour));
        if (savePlacement)
        {
            await cut.Find("button[aria-label='Remove Gallery photo']").ClickAsync();
        }

        // Act
        var buttonLabel = savePlacement ? "Save gallery" : "Initialize gallery";
        var action = cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == buttonLabel);
        await action.ClickAsync();

        // Assert
        var reads = catalog.StateReads.ToArray();
        reads.ShouldBe([(tour, null), (tour, savePlacement ? 7L : null)]);
        var refresh = cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Refresh gallery");
        refresh.HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public async Task A_stale_placement_cannot_be_resubmitted_before_loading_the_current_gallery()
    {
        // Arrange
        var tour = Guid.CreateVersion7();
        var originalPhoto = Guid.CreateVersion7();
        var committedPhoto = Guid.CreateVersion7();
        var original = FakeGalleryEditorApiClient.LegacyState with
        {
            PhotoIds = [originalPhoto]
        };
        var current = original with
        {
            StreamVersion = 5,
            ProjectedStreamVersion = 5,
            AcceptedPlacementSequence = 1,
            ResolvedPlacementSequence = 1,
            PhotoIds = [committedPhoto]
        };
        catalog.StateHandler = _ => Task.FromResult(HttpOutcomeMapper.Succeeded(
            HttpStatusCode.OK, catalog.StateReads.Count == 1 ? original : current));
        catalog.PhotoHandler = request =>
        {
            var photos = (request.IncludedPhotoIds ?? []).Select(id => new CatalogGalleryPhotoOptionDto(
                id, id == originalPhoto ? "Original photo" : "Committed photo", null, null)).ToArray();
            return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, new CatalogGalleryPhotoOptionsDto(photos, photos.Length)));
        };
        catalog.PlacementHandler = _ => Task.FromResult(catalog.PlacementRequests.Count == 1
            ? HttpOutcomeMapper.Status(HttpOutcomeKind.Conflict, HttpStatusCode.Conflict)
            : HttpOutcomeMapper.Succeeded(HttpStatusCode.Accepted, new Uri("?placementSequence=2", UriKind.Relative)));
        var cut = Render<EditGallery>(parameters => parameters.Add(page => page.Id, tour));
        await cut.Find("button[aria-label='Remove Original photo']").ClickAsync();

        // Act
        var save = cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Save gallery");
        await save.ClickAsync();

        // Assert
        cut.Find("[role='alert']").TextContent.ShouldContain("Refresh before trying again.", StringComparison.Ordinal);
        var staleSave = cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Save gallery");
        var staleEditor = staleSave.Closest("fieldset").ShouldNotBeNull();
        staleEditor.HasAttribute("disabled").ShouldBeTrue();
        var firstWrite = catalog.PlacementRequests.ShouldHaveSingleItem();
        firstWrite.StreamVersion.ShouldBe(3L);
        firstWrite.AcceptedPlacementSequence.ShouldBe(0L);

        // Act
        await staleSave.TriggerEventAsync("onclick", new MouseEventArgs());

        // Assert
        catalog.PlacementRequests.ShouldHaveSingleItem();

        // Act
        var refresh = cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Refresh gallery");
        await refresh.ClickAsync();

        // Assert
        cut.Find("[aria-label='Remove Committed photo']").ShouldNotBeNull();
        cut.FindAll("[role='alert']").ShouldBeEmpty();
        var unchangedSave = cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Save gallery");
        unchangedSave.HasAttribute("disabled").ShouldBeTrue();

        // Act
        await cut.Find("button[aria-label='Remove Committed photo']").ClickAsync();
        var refreshedSave = cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Save gallery");
        await refreshedSave.ClickAsync();

        // Assert
        catalog.PlacementRequests.Count.ShouldBe(2);
        var secondWrite = catalog.PlacementRequests[1];
        secondWrite.StreamVersion.ShouldBe(5L);
        secondWrite.AcceptedPlacementSequence.ShouldBe(1L);
    }

    [Fact]
    public async Task A_rejected_selection_can_be_corrected_without_refreshing_the_gallery()
    {
        // Arrange
        var tour = Guid.CreateVersion7();
        var original = FakeGalleryEditorApiClient.LegacyState with
        {
            PhotoIds = [Guid.CreateVersion7()]
        };
        catalog.StateHandler = _ => Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, original));
        catalog.PlacementHandler = _ => Task.FromResult(HttpOutcomeMapper.Status(HttpOutcomeKind.ValidationProblem, HttpStatusCode.BadRequest));
        var cut = Render<EditGallery>(parameters => parameters.Add(page => page.Id, tour));
        await cut.Find("button[aria-label='Remove Gallery photo']").ClickAsync();

        // Act
        var save = cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Save gallery");
        await save.ClickAsync();

        // Assert
        var rejectedEditor = cut.Find("fieldset");
        rejectedEditor.HasAttribute("disabled").ShouldBeFalse();
        cut.Find("[role='alert']").TextContent.ShouldContain("Check your selected photos", StringComparison.Ordinal);
        catalog.StateReads.ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("https://catalog.example/gallery-editor?placementSequence=7")]
    [InlineData("/catalog/tours/gallery-editor")]
    [InlineData("?placementSequence=invalid")]
    [InlineData("?placementSequence=0")]
    public async Task An_accepted_change_without_a_local_sequence_waits_for_the_gallery_state(string? location)
    {
        // Arrange
        var tour = Guid.CreateVersion7();
        var state = FakeGalleryEditorApiClient.LegacyState with
        {
            PhotoIds = [Guid.CreateVersion7()]
        };
        catalog.StateHandler = _ => Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, state));
        catalog.PlacementHandler = _ => Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.Accepted,
            location is null ? null : new Uri(location, UriKind.RelativeOrAbsolute)));
        var cut = Render<EditGallery>(parameters => parameters.Add(page => page.Id, tour));
        await cut.Find("button[aria-label='Remove Gallery photo']").ClickAsync();
        var save = cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Save gallery");

        // Act
        await save.ClickAsync();

        // Assert
        catalog.StateReads.ShouldBe([(tour, null), (tour, null)]);
        cut.Find("[role='status']").TextContent.ShouldBe("The gallery change was accepted. Refresh to check its status.");
        cut.FindAll("fieldset").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(CatalogGalleryPlacementStatusDto.Applied, "The gallery change was applied.")]
    [InlineData(CatalogGalleryPlacementStatusDto.RejectedAfterPublication, "Publication prevented the queued change from being applied.")]
    [InlineData(CatalogGalleryPlacementStatusDto.ClosedByRetirement, "The tour was retired before the queued change was applied.")]
    public void The_editor_explains_why_an_accepted_placement_resolved(CatalogGalleryPlacementStatusDto status, string message)
    {
        // Arrange
        var editable = status == CatalogGalleryPlacementStatusDto.Applied;
        var state = FakeGalleryEditorApiClient.LegacyState with
        {
            PhotoIds = [],
            AcceptedPlacementSequence = 7,
            ResolvedPlacementSequence = 7,
            CanEditGallery = editable,
            IsRetired = status == CatalogGalleryPlacementStatusDto.ClosedByRetirement,
            Outcome = new CatalogGalleryPlacementOutcomeDto(Guid.CreateVersion7(), 7, status)
        };
        catalog.StateHandler = _ => Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, state));
        var tour = Guid.CreateVersion7();

        // Act
        var cut = Render<EditGallery>(parameters => parameters.Add(page => page.Id, tour));

        // Assert
        cut.Find("[role='status']").TextContent.ShouldBe(message);
        var hasSaveAction = cut.FindAll("button").Any(button => button.TextContent.Trim() == "Save gallery");
        hasSaveAction.ShouldBe(editable);
    }

    [Fact]
    public async Task Removing_the_current_cover_queues_the_remaining_album_without_a_cover()
    {
        // Arrange
        var tour = Guid.CreateVersion7();
        var cover = Guid.CreateVersion7();
        var remainingPhoto = Guid.CreateVersion7();
        var state = FakeGalleryEditorApiClient.LegacyState with
        {
            PhotoIds = [cover, remainingPhoto],
            CoverId = cover
        };
        catalog.StateHandler = _ => Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, state));
        catalog.PhotoHandler = request =>
        {
            var photos = (request.IncludedPhotoIds ?? []).Select(id => new CatalogGalleryPhotoOptionDto(
                id, id == cover ? "Current cover" : "Remaining photo", null, null)).ToArray();
            return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, new CatalogGalleryPhotoOptionsDto(photos, photos.Length)));
        };
        var cut = Render<EditGallery>(parameters => parameters.Add(page => page.Id, tour));

        // Act
        await cut.Find("button[aria-label='Remove Current cover']").ClickAsync();
        var save = cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Save gallery");
        await save.ClickAsync();

        // Assert
        var queued = catalog.PlacementRequests.ShouldHaveSingleItem();
        queued.PhotoIds.ShouldBe([remainingPhoto]);
        queued.CoverId.ShouldBeNull();
    }

    [Fact]
    public async Task Refresh_recovers_the_selected_photos_after_the_initial_lookup_fails()
    {
        // Arrange
        var tour = Guid.CreateVersion7();
        var state = FakeGalleryEditorApiClient.LegacyState with
        {
            PhotoIds = [Guid.CreateVersion7()]
        };
        catalog.StateHandler = _ => Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, state));
        var attempts = 0;
        catalog.PhotoHandler = request => ++attempts == 1
            ? Task.FromResult(new HttpOutcome<CatalogGalleryPhotoOptionsDto> { Kind = HttpOutcomeKind.TransportFailure })
            : FakeGalleryEditorApiClient.CreatePhotoPage(request);
        var cut = Render<EditGallery>(parameters => parameters.Add(page => page.Id, tour));
        var initialAlert = cut.Find("[role='alert']").TextContent;
        var hadPickerWhileUnavailable = cut.FindAll("#gallery-photos").Count != 0;

        // Act
        var refresh = cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Refresh gallery");
        await refresh.ClickAsync();

        // Assert
        initialAlert.ShouldContain("The selected photos could not be loaded.", StringComparison.Ordinal);
        hadPickerWhileUnavailable.ShouldBeFalse();
        cut.FindAll("[role='alert']").ShouldBeEmpty();
        cut.Find("button[aria-label='Remove Gallery photo']").ShouldNotBeNull();
        catalog.PlacementRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_failed_initialization_leaves_the_legacy_gallery_available_for_retry()
    {
        // Arrange
        var tour = Guid.CreateVersion7();
        var authored = FakeGalleryEditorApiClient.LegacyState with
        {
            PhotoIds = []
        };
        catalog.StateHandler = _ => Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK,
            catalog.StateReads.Count == 1 ? FakeGalleryEditorApiClient.LegacyState : authored));
        var attempts = 0;
        catalog.InitializeHandler = _ => Task.FromResult(++attempts == 1
            ? HttpOutcomeMapper.Status(HttpOutcomeKind.ValidationProblem, HttpStatusCode.BadRequest)
            : HttpOutcomeMapper.Succeeded(HttpStatusCode.NoContent, location: null));
        var cut = Render<EditGallery>(parameters => parameters.Add(page => page.Id, tour));

        // Act
        var initialize = cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Initialize gallery");
        await initialize.ClickAsync();
        var failureMessage = cut.Find("[role='alert']").TextContent;
        var retry = cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Initialize gallery");
        var couldRetry = !retry.HasAttribute("disabled");
        await retry.ClickAsync();

        // Assert
        failureMessage.ShouldContain("The gallery could not be initialized.", StringComparison.Ordinal);
        couldRetry.ShouldBeTrue();
        cut.FindAll("[role='alert']").ShouldBeEmpty();
        catalog.StateReads.ShouldBe([(tour, null), (tour, null)]);
        cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Save gallery");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Picker_lookup_errors_are_visible_only_on_the_requesting_visit(bool changeVisit)
    {
        // Arrange
        var ct = Xunit.TestContext.Current.CancellationToken;
        var tour = Guid.CreateVersion7();
        var otherTour = Guid.CreateVersion7();
        var deferredPhoto = Guid.CreateVersion7();
        var tourParameters = ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(EditGallery.Id)] = tour });
        var otherParameters = ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(EditGallery.Id)] = otherTour });
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var response = new TaskCompletionSource<HttpOutcome<CatalogGalleryPhotoOptionsDto>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var editor = FakeGalleryEditorApiClient.LegacyState with
        {
            PhotoIds = [Guid.CreateVersion7()]
        };
        catalog.StateHandler = _ => Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, editor));
        catalog.PhotoHandler = request =>
        {
            if (request.IncludedPhotoIds?.Contains(deferredPhoto) == true)
            {
                started.TrySetResult();
                return response.Task;
            }

            return FakeGalleryEditorApiClient.CreatePhotoPage(request);
        };
        var cut = Render<EditGallery>(parameters => parameters.Add(page => page.Id, tour));
        var picker = cut.FindComponents<ImagePicker<Guid, CatalogGalleryPhotoOptionDto>>()
            .ShouldHaveSingleItem(component => component.Instance.Id == "gallery-photos");
        var lookup = picker.Instance.Lookup.ShouldNotBeNull();

        // Act
        var pending = cut.InvokeAsync(() => lookup([deferredPhoto], ct));
        await FakeGalleryEditorApiClient.ReleaseAfter(async () =>
        {
            await started.Task.WaitAsync(ct);
            if (changeVisit)
            {
                await cut.InvokeAsync(() => cut.Instance.SetParametersAsync(otherParameters));
                await cut.InvokeAsync(() => cut.Instance.SetParametersAsync(tourParameters));
            }
        }, () => response.TrySetResult(new HttpOutcome<CatalogGalleryPhotoOptionsDto> { Kind = HttpOutcomeKind.NotFound }));
        await pending;
        await cut.InvokeAsync(() => cut.Instance.SetParametersAsync(tourParameters));

        // Assert
        cut.FindAll("[role='alert']").Count.ShouldBe(changeVisit ? 0 : 1);
    }
}
