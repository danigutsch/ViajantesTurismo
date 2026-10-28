using System.Net.Http.Headers;
using PandaBear.Testing;
using PandaBear.EventSourcing;
using ViajantesTurismo.Catalog.Domain.Tours;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
public sealed class CatalogGalleryPublicationBoundaryTests
{
    [Theory]
    [InlineData("Operator", true, false, HttpStatusCode.Forbidden)]
    [InlineData("Operator", false, false, HttpStatusCode.Created)]
    [InlineData("Admin", true, false, HttpStatusCode.Created)]
    [InlineData("Operator", false, true, HttpStatusCode.Forbidden)]
    public async Task Uploading_live_gallery_content_requires_a_publisher(string role, bool published, bool publicationPending, HttpStatusCode expectedStatus)
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tours = new TestCatalogTourReadModelStore();
        var tour = new CatalogTourDraftReadModel(tourId, Guid.CreateVersion7(), "TOUR-BOUNDARY", "Boundary Tour", "boundary-tour", published, 1, TimeProvider.System.GetUtcNow());
        await tours.UpsertDraft(tour, TestContext.Current.CancellationToken);
        var events = new TestEventStore();
        events.SeedTour(tour);
        if (publicationPending)
        {
            _ = await events.Append(CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId), ExpectedStreamRevision.From(StreamRevision.From(1)),
                [new CatalogTourPublished(tourId)], TestContext.Current.CancellationToken);
        }
        var images = new TestPublicMediaImageStore();
        await using var factory = CatalogApiTestHost.Create(tours, images, events);
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, role);
        using var content = new MultipartFormDataContent();
        using var file = new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAD0lEQVR4nGNgSDsDRXAWAEMEBy263W6BAAAAAElFTkSuQmCC"));
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "tour.png");
        content.Add(new StringContent("New gallery content"), "altText");

        // Act
        using var response = await client.PostAsync(new Uri($"/api/v1/catalog/tours/{tourId}/images", UriKind.Relative), content, TestContext.Current.CancellationToken);
        var stored = await images.ListByTour(tourId, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(expectedStatus);
        stored.Count.ShouldBe(expectedStatus == HttpStatusCode.Created ? 1 : 0);
    }

    [Theory]
    [InlineData("Operator", true, false, PublicContentLanguageDto.EnUs, HttpStatusCode.Forbidden)]
    [InlineData("Operator", true, false, PublicContentLanguageDto.PtBr, HttpStatusCode.Forbidden)]
    [InlineData("Operator", false, false, PublicContentLanguageDto.EnUs, HttpStatusCode.OK)]
    [InlineData("Admin", true, false, PublicContentLanguageDto.EnUs, HttpStatusCode.OK)]
    [InlineData("Operator", false, true, PublicContentLanguageDto.EnUs, HttpStatusCode.Forbidden)]
    public async Task Reviewing_live_gallery_content_requires_a_publisher(string role, bool published, bool publicationPending, PublicContentLanguageDto language, HttpStatusCode expectedStatus)
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tours = new TestCatalogTourReadModelStore();
        var tour = new CatalogTourDraftReadModel(tourId, Guid.CreateVersion7(), "TOUR-BOUNDARY", "Boundary Tour", "boundary-tour", published, 1, TimeProvider.System.GetUtcNow());
        await tours.UpsertDraft(tour, TestContext.Current.CancellationToken);
        var events = new TestEventStore();
        events.SeedTour(tour);
        if (publicationPending)
        {
            _ = await events.Append(CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId), ExpectedStreamRevision.From(StreamRevision.From(1)),
                [new CatalogTourPublished(tourId)], TestContext.Current.CancellationToken);
        }
        var image = PublicMediaImageTestFactory.CreateReadyImage(tourId, "source.jpg", "public.jpg", "sha256:boundary", "Existing text", 0, true);
        var images = new TestPublicMediaImageStore();
        await images.Upsert(image, TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tours, images, events);
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, role);

        // Act
        using var response = await client.PutAsJsonAsync(new Uri($"/api/v1/catalog/media/images/{image.Id}/accessibility-review", UriKind.Relative),
            new PublicMediaImageAccessibilityReviewRequest { Language = language, AltText = "Replacement public text", Caption = "Replacement caption" }, TestContext.Current.CancellationToken);
        var saved = await images.GetImage(image.Id, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(expectedStatus);
        saved.ShouldNotBeNull();
        if (expectedStatus == HttpStatusCode.Forbidden)
        {
            saved.AltText.ShouldBe("Existing text");
            saved.Caption.ShouldBeNull();
            saved.AccessibilityTexts.Count.ShouldBe(1);
        }
    }
    [Theory]
    [InlineData(PublicContentLanguageDto.EnUs)]
    [InlineData(PublicContentLanguageDto.PtBr)]
    public async Task Generating_a_draft_cannot_replace_live_metadata_without_a_publisher(PublicContentLanguageDto language)
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tours = new TestCatalogTourReadModelStore();
        await tours.UpsertDraft(new CatalogTourDraftReadModel(tourId, Guid.CreateVersion7(), "TOUR-DRAFT", "Draft Tour", "draft-tour", true, 1, TimeProvider.System.GetUtcNow()), TestContext.Current.CancellationToken);
        var image = PublicMediaImageTestFactory.CreateReadyImage(tourId, "source.jpg", "public.jpg", "sha256:draft", "Existing text", 0, true);
        var images = new TestPublicMediaImageStore();
        await images.Upsert(image, TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tours, images, new TestMediaObjectStore());
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, "Operator");

        // Act
        using var response = await client.PostAsJsonAsync(new Uri($"/api/v1/catalog/media/images/{image.Id}/accessibility-draft", UriKind.Relative),
            new PublicMediaImageAccessibilityDraftRequest { Language = language }, TestContext.Current.CancellationToken);
        var saved = await images.GetImage(image.Id, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        saved.ShouldNotBeNull();
        saved.AltText.ShouldBe("Existing text");
        saved.RequiresHumanReview.ShouldBeFalse();
        saved.AccessibilityTexts.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Reviewing_a_shared_image_requires_publication_permission_for_every_linked_tour(bool publishedFirst)
    {
        // Arrange
        var draftId = Guid.CreateVersion7();
        var publishedId = Guid.CreateVersion7();
        var tours = new TestCatalogTourReadModelStore();
        await tours.UpsertDraft(new CatalogTourDraftReadModel(draftId, Guid.CreateVersion7(), "DRAFT", "Draft", "draft", false, 1, TimeProvider.System.GetUtcNow()), TestContext.Current.CancellationToken);
        await tours.UpsertDraft(new CatalogTourDraftReadModel(publishedId, Guid.CreateVersion7(), "PUBLISHED", "Published", "published", true, 2, TimeProvider.System.GetUtcNow()), TestContext.Current.CancellationToken);
        var imageResult = PublicMediaImage.Create(new PublicMediaImageMetadata
        {
            Id = Guid.CreateVersion7(),
            SourceObjectKey = "source.jpg",
            Checksum = "sha256:shared",
            ContentType = "image/jpeg",
            FileSizeBytes = 100,
            Dimensions = new MediaImageDimensions(100, 100),
            ProcessingStatus = MediaImageProcessingStatus.Ready,
            AltText = "Shared image"
        }, [new MediaImageResponsiveVariant("public.jpg", 100, 100, "image/jpeg", 100)], [],
            publishedFirst ? [new MediaImageTourLink(publishedId, 0, true), new MediaImageTourLink(draftId, 0, true)] : [new MediaImageTourLink(draftId, 0, true), new MediaImageTourLink(publishedId, 0, true)]);
        imageResult.IsSuccess.ShouldBeTrue();
        var image = imageResult.Value;
        var images = new TestPublicMediaImageStore();
        await images.Upsert(image, TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tours, images, new TestMediaObjectStore());
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, "Operator");

        // Act
        using var response = await client.PutAsJsonAsync(new Uri($"/api/v1/catalog/media/images/{image.Id}/accessibility-review", UriKind.Relative),
            new PublicMediaImageAccessibilityReviewRequest { AltText = "Unapproved public replacement" }, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        image.AltText.ShouldBe("Shared image");
    }

    [Fact]
    public async Task Upload_waits_for_publication_and_checks_the_committed_stream_after_acquiring_the_lease()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tour = new CatalogTourDraftReadModel(tourId, Guid.CreateVersion7(), "TOUR-RACE", "Race", "race", false, 1, TimeProvider.System.GetUtcNow());
        var tours = new TestCatalogTourReadModelStore();
        await tours.UpsertDraft(tour, TestContext.Current.CancellationToken);
        var events = new TestEventStore();
        events.SeedTour(tour);
        var images = new TestPublicMediaImageStore();
        var mutationLock = new TestCatalogTourMutationLock();
        var observed = new ObservedCatalogTourMutationLock(mutationLock);
        await using var held = await mutationLock.Acquire([tourId], TestContext.Current.CancellationToken);
        await using var baseFactory = CatalogApiTestHost.Create(tours, images, events);
        await using var factory = baseFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.Replace(ServiceDescriptor.Singleton<ICatalogTourMutationLock>(observed))));
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, "Operator");
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent([1]), "file", "tour.png");
        content.Add(new StringContent("Unapproved image"), "altText");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"/api/v1/catalog/tours/{tourId}/images", UriKind.Relative)) { Content = content };
        await using var pending = new PendingCatalogMutationRequest(client, request, held);
        pending.Start(timeout.Token);
        await observed.AcquisitionStarted.WaitAsync(timeout.Token);
        _ = await events.Append(CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId), ExpectedStreamRevision.From(StreamRevision.From(1)),
            [new CatalogTourPublished(tourId)], timeout.Token);

        // Assert
        pending.IsCompleted.ShouldBeFalse();

        // Act
        using var response = await pending.Complete();
        var saved = await images.ListByTour(tourId, timeout.Token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        saved.ShouldBeEmpty();
    }

    [Fact]
    public async Task Publication_waits_for_an_active_gallery_mutation_lease()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tour = new CatalogTourDraftReadModel(tourId, Guid.CreateVersion7(), "TOUR-PUBLISH-RACE", "Publish Race", "publish-race", false, 1, TimeProvider.System.GetUtcNow());
        var tours = new TestCatalogTourReadModelStore();
        await tours.UpsertDraft(tour, TestContext.Current.CancellationToken);
        var mutationLock = new TestCatalogTourMutationLock();
        var observed = new ObservedCatalogTourMutationLock(mutationLock);
        await using var baseFactory = CatalogApiTestHost.Create(tours, new TestPublicContentStore());
        await using var factory = baseFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.Replace(ServiceDescriptor.Singleton<ICatalogTourMutationLock>(observed))));
        using var client = factory.CreateClient();
        using var presentation = await client.PutAsJsonAsync(new Uri($"/api/v1/catalog/tours/{tourId}/presentation", UriKind.Relative),
            new UpsertCatalogTourPresentationRequest { Title = "Publish Race", Slug = "publish-race", Summary = "Ready to publish", ExpectedVersion = 1 }, TestContext.Current.CancellationToken);
        presentation.StatusCode.ShouldBe(HttpStatusCode.OK);
        await using var held = await mutationLock.Acquire([tourId], TestContext.Current.CancellationToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"/api/v1/catalog/tours/{tourId}/publish", UriKind.Relative))
        {
            Content = JsonContent.Create(new CatalogTourPublicationRequest { ExpectedVersion = 2 })
        };
        await using var pending = new PendingCatalogMutationRequest(client, request, held);
        pending.Start(timeout.Token);
        await observed.AcquisitionStarted.WaitAsync(timeout.Token);

        // Assert
        pending.IsCompleted.ShouldBeFalse();

        // Act
        using var response = await pending.Complete();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Theory]
    [InlineData("upload", true, false, HttpStatusCode.Forbidden)]
    [InlineData("review", true, false, HttpStatusCode.Forbidden)]
    [InlineData("draft", true, false, HttpStatusCode.Forbidden)]
    [InlineData("upload", false, false, HttpStatusCode.Forbidden)]
    [InlineData("review", false, false, HttpStatusCode.Forbidden)]
    [InlineData("draft", false, false, HttpStatusCode.Forbidden)]
    [InlineData("upload", true, true, HttpStatusCode.Created)]
    [InlineData("review", true, true, HttpStatusCode.OK)]
    public async Task Gallery_writer_edits_require_a_current_hidden_public_projection(string operation, bool visible, bool caughtUp, HttpStatusCode expectedStatus)
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tour = new CatalogTourDraftReadModel(tourId, Guid.CreateVersion7(), "UNPUBLISH-PENDING", "Unpublish Pending", "unpublish-pending", visible, 1, TimeProvider.System.GetUtcNow());
        var tours = new TestCatalogTourReadModelStore();
        await tours.UpsertDraft(tour, TestContext.Current.CancellationToken);
        var events = new TestEventStore();
        events.SeedTour(tour);
        _ = await events.Append(CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId), ExpectedStreamRevision.From(StreamRevision.From(visible ? 3 : 1)),
            visible ? [new CatalogTourUnpublished(tourId)] : [new CatalogTourPublished(tourId), new CatalogTourUnpublished(tourId)], TestContext.Current.CancellationToken);
        if (caughtUp)
        {
            _ = await tours.SetPublicationStatus(tourId, false, 4, 4, TimeProvider.System.GetUtcNow(), TestContext.Current.CancellationToken);
        }
        var image = PublicMediaImageTestFactory.CreateReadyImage(tourId, "source.jpg", "public.jpg", "sha256:pending", "Existing published text", 0, true);
        var images = new TestPublicMediaImageStore();
        await images.Upsert(image, TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tours, images, events);
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, "Operator");
        using var content = new MultipartFormDataContent();
        using var file = new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAD0lEQVR4nGNgSDsDRXAWAEMEBy263W6BAAAAAElFTkSuQmCC"));
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "tour.png");
        content.Add(new StringContent("Unapproved public image"), "altText");

        // Act
        using var response = operation switch
        {
            "upload" => await client.PostAsync(new Uri($"/api/v1/catalog/tours/{tourId}/images", UriKind.Relative), content, TestContext.Current.CancellationToken),
            "review" => await client.PutAsJsonAsync(new Uri($"/api/v1/catalog/media/images/{image.Id}/accessibility-review", UriKind.Relative),
                new PublicMediaImageAccessibilityReviewRequest { AltText = "Unapproved public replacement" }, TestContext.Current.CancellationToken),
            _ => await client.PostAsJsonAsync(new Uri($"/api/v1/catalog/media/images/{image.Id}/accessibility-draft", UriKind.Relative),
                new PublicMediaImageAccessibilityDraftRequest { Language = PublicContentLanguageDto.EnUs }, TestContext.Current.CancellationToken)
        };
        var saved = await images.ListByTour(tourId, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(expectedStatus);
        if (expectedStatus == HttpStatusCode.Forbidden)
        {
            saved.ShouldHaveSingleItem().AltText.ShouldBe("Existing published text");
        }
    }

}
