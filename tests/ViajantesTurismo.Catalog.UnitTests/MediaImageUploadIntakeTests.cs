using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Media;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Testing.Infrastructure;
using PandaBear.Results;
using PandaBear.Testing;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class MediaImageUploadIntakeTests
{
    [Fact]
    [Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
    public async Task Accept_does_not_log_raw_scanner_messages()
    {
        // Arrange
        const string scannerMessage = "traveler@example.com in media/customer/private-image.jpg";
        var originalImage = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1);
        var content = CatalogTestImages.CreateJpeg(320, 160);
        var logger = new CollectingLogger<MediaImageUploadIntake>();
        var intake = MediaImageUploadIntakeTestFactory.Create(
            new StubMediaUploadScanner(new MediaUploadScanResult(MediaUploadScanStatus.Rejected, scannerMessage)),
            new InMemoryMediaObjectStore(),
            new InMemoryPublicMediaImageStore(originalImage),
            logger: logger);
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(),
            new MemoryStream(content),
            "photo.jpg",
            "image/jpeg",
            content.Length,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var message = logger.Messages.ShouldHaveSingleItem();
        message.ShouldContain(nameof(MediaUploadScanStatus.Rejected), StringComparison.Ordinal);
        message.ShouldNotContain(scannerMessage, StringComparison.Ordinal);
        logger.StructuredValues.ShouldNotContain(value => string.Equals(value.Value as string, scannerMessage, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Accept_logs_the_scan_status_when_the_scanner_message_is_missing()
    {
        // Arrange
        var originalImage = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1);
        var content = CatalogTestImages.CreateJpeg(320, 160);
        var logger = new CollectingLogger<MediaImageUploadIntake>();
        var intake = MediaImageUploadIntakeTestFactory.Create(
            new StubMediaUploadScanner(new MediaUploadScanResult(MediaUploadScanStatus.Rejected)),
            new InMemoryMediaObjectStore(),
            new InMemoryPublicMediaImageStore(originalImage),
            logger: logger);
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(),
            new MemoryStream(content),
            "photo.jpg",
            "image/jpeg",
            content.Length,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var message = logger.Messages.ShouldHaveSingleItem();
        message.ShouldContain(nameof(MediaUploadScanStatus.Rejected), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Accept_stores_metadata_with_generated_object_key_when_scan_passes()
    {
        // Arrange
        var mediaImageId = Guid.CreateVersion7();
        var content = CatalogTestImages.CreateJpeg(640, 320);
        var objectStore = new InMemoryMediaObjectStore();
        var imageStore = new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1));
        var scanner = new StubMediaUploadScanner(MediaUploadScanResult.Passed);
        var intake = MediaImageUploadIntakeTestFactory.Create(scanner, objectStore, imageStore,
            timeProvider: new ControlledTimeProvider(new DateTimeOffset(2030, 2, 3, 14, 0, 0, TimeSpan.Zero)));
        var tourLink = new MediaImageTourLink(Guid.CreateVersion7(), 0, true);
        var request = new MediaImageUploadIntakeRequest(
            mediaImageId,
            new MemoryStream(content),
            "client-photo.jpg",
            "image/jpeg",
            content.Length,
            "Cyclists in the mountains",
            [tourLink]);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ScanStatus.ShouldBe(MediaUploadScanStatus.Passed);
        imageStore.Current.Id.ShouldBe(mediaImageId);
        imageStore.Current.ProcessingStatus.ShouldBe(MediaImageProcessingStatus.Pending);
        imageStore.Current.Dimensions.Width.ShouldBe(640);
        imageStore.Current.Dimensions.Height.ShouldBe(320);
        var storedObjectKey = objectStore.ObjectKeys.ShouldHaveSingleItem();
        storedObjectKey.ShouldStartWith($"media/{mediaImageId:N}/original-", StringComparison.Ordinal);
        storedObjectKey.ShouldEndWith(".jpg", StringComparison.Ordinal);
        objectStore.ObjectKeys.ShouldNotContain("client-photo.jpg");
        result.Value.OriginalStoredEvent.SourceObjectKey.ShouldBe(storedObjectKey);
        result.Value.OriginalStoredEvent.OccurredAt.ShouldBe(new DateTimeOffset(2030, 2, 3, 14, 0, 0, TimeSpan.Zero));
        scanner.LastRequest.ShouldNotBeNull();
        scanner.LastRequest.ObjectKey.ShouldBe(storedObjectKey);
    }

    [Fact]
    public async Task Accept_uses_a_distinct_original_object_key_for_each_upload_attempt()
    {
        // Arrange
        var mediaImageId = Guid.CreateVersion7();
        var content = CatalogTestImages.CreateJpeg(320, 160);
        var objectStore = new InMemoryMediaObjectStore();
        var intake = MediaImageUploadIntakeTestFactory.Create(
            new StubMediaUploadScanner(MediaUploadScanResult.Passed),
            objectStore,
            new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1)));
        var request = new MediaImageUploadIntakeRequest(
            mediaImageId,
            new MemoryStream(content),
            "photo.jpg",
            "image/jpeg",
            content.Length,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        var first = await intake.Accept(request, TestContext.Current.CancellationToken);
        var retry = await intake.Accept(request with
        {
            Content = new MemoryStream(content)
        }, TestContext.Current.CancellationToken);

        // Assert
        first.IsSuccess.ShouldBeTrue();
        retry.IsSuccess.ShouldBeTrue();
        first.Value.Image.SourceObjectKey.ShouldNotBe(retry.Value.Image.SourceObjectKey);
        objectStore.ObjectKeys.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Accept_allows_disabled_scanner_for_development_intake()
    {
        // Arrange
        var mediaImageId = Guid.CreateVersion7();
        var content = CatalogTestImages.CreateJpeg(320, 160);
        var imageStore = new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1));
        var intake = MediaImageUploadIntakeTestFactory.Create(
            new StubMediaUploadScanner(new MediaUploadScanResult(MediaUploadScanStatus.Disabled)),
            new InMemoryMediaObjectStore(),
            imageStore);
        var request = new MediaImageUploadIntakeRequest(
            mediaImageId,
            new MemoryStream(content),
            "photo.jpg",
            "image/jpeg",
            content.Length,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ScanStatus.ShouldBe(MediaUploadScanStatus.Disabled);
        imageStore.Current.Id.ShouldBe(mediaImageId);
    }

    [Fact]
    public async Task Accept_enqueues_original_stored_event_with_media_metadata()
    {
        // Arrange
        var mediaImageId = Guid.CreateVersion7();
        var content = CatalogTestImages.CreateJpeg(320, 160);
        var outbox = new CapturingIntegrationEventOutbox();
        var intake = MediaImageUploadIntakeTestFactory.Create(
            new StubMediaUploadScanner(MediaUploadScanResult.Passed),
            new InMemoryMediaObjectStore(),
            new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1)),
            outbox);
        var request = new MediaImageUploadIntakeRequest(
            mediaImageId,
            new MemoryStream(content),
            "photo.jpg",
            "image/jpeg",
            content.Length,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var integrationEvent = outbox.IntegrationEvent.ShouldBeOfType<MediaImageOriginalStoredIntegrationEvent>();
        integrationEvent.MediaImageId.ShouldBe(mediaImageId);
        integrationEvent.SourceObjectKey.ShouldBe(result.Value.OriginalStoredEvent.SourceObjectKey);
    }

    [Fact]
    public async Task Accept_deletes_stored_object_when_metadata_validation_fails()
    {
        // Arrange
        var mediaImageId = Guid.CreateVersion7();
        var originalImage = PublicMediaImageTestFactory.CreatePendingImage(mediaImageId, 1);
        var content = CatalogTestImages.CreateJpeg(320, 160);
        var objectStore = new InMemoryMediaObjectStore();
        var imageStore = new InMemoryPublicMediaImageStore(originalImage);
        var intake = MediaImageUploadIntakeTestFactory.Create(
            new StubMediaUploadScanner(MediaUploadScanResult.Passed),
            objectStore,
            imageStore);
        var request = new MediaImageUploadIntakeRequest(
            mediaImageId,
            new MemoryStream(content),
            "photo.jpg",
            "image/jpeg",
            content.Length,
            "Cyclists in the mountains",
            []);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBe(true);
        imageStore.Current.Id.ShouldBe(originalImage.Id);
        objectStore.ObjectKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task Accept_deletes_stored_object_when_metadata_persistence_fails()
    {
        // Arrange
        var mediaImageId = Guid.CreateVersion7();
        var originalImage = PublicMediaImageTestFactory.CreatePendingImage(mediaImageId, 1);
        var content = CatalogTestImages.CreateJpeg(320, 160);
        var objectStore = new InMemoryMediaObjectStore();
        var imageStore = new ThrowingPublicMediaImageStore(originalImage, new InvalidOperationException("database unavailable"));
        var intake = MediaImageUploadIntakeTestFactory.Create(
            new StubMediaUploadScanner(MediaUploadScanResult.Passed),
            objectStore,
            imageStore);
        var request = new MediaImageUploadIntakeRequest(
            mediaImageId,
            new MemoryStream(content),
            "photo.jpg",
            "image/jpeg",
            content.Length,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        Func<Task> action = async () => await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        await action.ShouldThrow<InvalidOperationException>();
        objectStore.ObjectKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task Accept_returns_a_retryable_conflict_when_gallery_placement_is_taken_concurrently()
    {
        // Arrange
        var mediaImageId = Guid.CreateVersion7();
        var originalImage = PublicMediaImageTestFactory.CreatePendingImage(mediaImageId, 1);
        var content = CatalogTestImages.CreateJpeg(320, 160);
        var objectStore = new InMemoryMediaObjectStore();
        var imageStore = new ThrowingPublicMediaImageStore(originalImage, new MediaGalleryPlacementConflictException());
        var intake = MediaImageUploadIntakeTestFactory.Create(
            new StubMediaUploadScanner(MediaUploadScanResult.Passed),
            objectStore,
            imageStore);
        var request = new MediaImageUploadIntakeRequest(
            mediaImageId,
            new MemoryStream(content),
            "photo.jpg",
            "image/jpeg",
            content.Length,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Conflict);
        objectStore.ObjectKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task Accept_rejects_upload_when_scanner_rejects()
    {
        // Arrange
        var originalImage = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1);
        var content = CatalogTestImages.CreateJpeg(320, 160);
        var objectStore = new InMemoryMediaObjectStore();
        var imageStore = new InMemoryPublicMediaImageStore(originalImage);
        var intake = MediaImageUploadIntakeTestFactory.Create(
            new StubMediaUploadScanner(new MediaUploadScanResult(MediaUploadScanStatus.Rejected, "malware detected")),
            objectStore,
            imageStore);
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(),
            new MemoryStream(content),
            "photo.jpg",
            "image/jpeg",
            content.Length,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBe(true);
        var rejection = result.ErrorDetails.ShouldNotBeNull();
        rejection.Detail.ShouldBe("Media upload is invalid.");
        var scanErrors = rejection.ValidationErrors.ShouldNotBeNull()["Status"];
        scanErrors.ShouldBe(["Media upload did not pass security scanning."]);
        imageStore.Current.Id.ShouldBe(originalImage.Id);
        objectStore.ObjectKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task Accept_fails_closed_when_scanner_is_unavailable()
    {
        // Arrange
        var originalImage = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1);
        var content = CatalogTestImages.CreateJpeg(320, 160);
        var objectStore = new InMemoryMediaObjectStore();
        var imageStore = new InMemoryPublicMediaImageStore(originalImage);
        var intake = MediaImageUploadIntakeTestFactory.Create(
            new StubMediaUploadScanner(new MediaUploadScanResult(MediaUploadScanStatus.Failed, "scanner unavailable")),
            objectStore,
            imageStore);
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(),
            new MemoryStream(content),
            "photo.jpg",
            "image/jpeg",
            content.Length,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.ErrorDetails.ShouldNotBeNull();
        result.ErrorDetails.Detail.ShouldBe("Media upload scanner is unavailable.");
        imageStore.Current.Id.ShouldBe(originalImage.Id);
        objectStore.ObjectKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task Accept_fails_closed_when_scanner_throws()
    {
        // Arrange
        var originalImage = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1);
        var content = CatalogTestImages.CreateJpeg(320, 160);
        var objectStore = new InMemoryMediaObjectStore();
        var imageStore = new InMemoryPublicMediaImageStore(originalImage);
        var intake = MediaImageUploadIntakeTestFactory.Create(
            new StubMediaUploadScanner(MediaUploadScanResult.Passed, new TimeoutException("scanner timeout")),
            objectStore,
            imageStore);
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(),
            new MemoryStream(content),
            "photo.jpg",
            "image/jpeg",
            content.Length,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.ErrorDetails.ShouldNotBeNull();
        result.ErrorDetails.Detail.ShouldBe("Media upload scanner is unavailable.");
        imageStore.Current.Id.ShouldBe(originalImage.Id);
        objectStore.ObjectKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task Accept_fails_closed_when_scanner_throws_an_unexpected_exception()
    {
        // Arrange
        var originalImage = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1);
        var content = CatalogTestImages.CreateJpeg(320, 160);
        var objectStore = new InMemoryMediaObjectStore();
        var imageStore = new InMemoryPublicMediaImageStore(originalImage);
        var intake = MediaImageUploadIntakeTestFactory.Create(
            new StubMediaUploadScanner(MediaUploadScanResult.Passed, new FormatException("scanner crashed")),
            objectStore,
            imageStore);
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(),
            new MemoryStream(content),
            "photo.jpg",
            "image/jpeg",
            content.Length,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.ErrorDetails.ShouldNotBeNull();
        result.ErrorDetails.Detail.ShouldBe("Media upload scanner is unavailable.");
        imageStore.Current.Id.ShouldBe(originalImage.Id);
        objectStore.ObjectKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task Accept_rejects_oversized_uploads_before_scanning_or_metadata()
    {
        // Arrange
        var originalImage = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1);
        var content = new byte[] { 0xFF, 0xD8, 0xFF, 0x00 };
        var objectStore = new InMemoryMediaObjectStore();
        var imageStore = new InMemoryPublicMediaImageStore(originalImage);
        var scanner = new StubMediaUploadScanner(MediaUploadScanResult.Passed);
        var intake = MediaImageUploadIntakeTestFactory.Create(
            scanner,
            objectStore,
            imageStore,
            new MediaUploadValidationOptions { MaxLengthBytes = 3 });
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(),
            new MemoryStream(content),
            "photo.jpg",
            "image/jpeg",
            content.Length,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBe(true);
        scanner.ScanCount.ShouldBe(0);
        imageStore.Current.Id.ShouldBe(originalImage.Id);
        objectStore.ObjectKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task Accept_rejects_length_mismatch_before_scanning_or_metadata()
    {
        // Arrange
        var originalImage = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1);
        var content = CatalogTestImages.CreateJpeg(320, 160);
        var objectStore = new InMemoryMediaObjectStore();
        var imageStore = new InMemoryPublicMediaImageStore(originalImage);
        var scanner = new StubMediaUploadScanner(MediaUploadScanResult.Passed);
        var intake = MediaImageUploadIntakeTestFactory.Create(scanner, objectStore, imageStore);
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(),
            new MemoryStream(content),
            "photo.jpg",
            "image/jpeg",
            content.Length + 1,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBe(true);
        scanner.ScanCount.ShouldBe(0);
        imageStore.Current.Id.ShouldBe(originalImage.Id);
        objectStore.ObjectKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task Accept_rejects_path_like_file_names_before_scanning_or_metadata()
    {
        // Arrange
        var originalImage = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1);
        var content = CatalogTestImages.CreateJpeg(320, 160);
        var objectStore = new InMemoryMediaObjectStore();
        var imageStore = new InMemoryPublicMediaImageStore(originalImage);
        var scanner = new StubMediaUploadScanner(MediaUploadScanResult.Passed);
        var intake = MediaImageUploadIntakeTestFactory.Create(scanner, objectStore, imageStore);
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(),
            new MemoryStream(content),
            "../photo.jpg",
            "image/jpeg",
            content.Length,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBe(true);
        scanner.ScanCount.ShouldBe(0);
        imageStore.Current.Id.ShouldBe(originalImage.Id);
        objectStore.ObjectKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task Accept_rejects_pending_scan_results_before_storage_or_metadata()
    {
        // Arrange
        var originalImage = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1);
        var content = CatalogTestImages.CreateJpeg(320, 160);
        var objectStore = new InMemoryMediaObjectStore();
        var imageStore = new InMemoryPublicMediaImageStore(originalImage);
        var intake = MediaImageUploadIntakeTestFactory.Create(
            new StubMediaUploadScanner(new MediaUploadScanResult(MediaUploadScanStatus.Pending, "scan pending")),
            objectStore,
            imageStore);
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(),
            new MemoryStream(content),
            "photo.jpg",
            "image/jpeg",
            content.Length,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBe(true);
        imageStore.Current.Id.ShouldBe(originalImage.Id);
        objectStore.ObjectKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task Accept_rejects_malformed_images_before_metadata_is_created()
    {
        // Arrange
        var originalImage = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1);
        var imageStore = new InMemoryPublicMediaImageStore(originalImage);
        var scanner = new StubMediaUploadScanner(MediaUploadScanResult.Passed);
        var intake = MediaImageUploadIntakeTestFactory.Create(scanner, new InMemoryMediaObjectStore(), imageStore);
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(),
            new MemoryStream([0xFF, 0xD8, 0xFF, 0x00]),
            "photo.jpg",
            "image/jpeg",
            4,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBe(true);
        imageStore.Current.Id.ShouldBe(originalImage.Id);
        scanner.ScanCount.ShouldBe(1);
    }

    [Fact]
    public async Task Accept_rejects_decoded_images_over_configured_limits()
    {
        // Arrange
        var originalImage = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1);
        var content = CatalogTestImages.CreateJpeg(320, 160);
        var imageStore = new InMemoryPublicMediaImageStore(originalImage);
        var scanner = new StubMediaUploadScanner(MediaUploadScanResult.Passed);
        var intake = MediaImageUploadIntakeTestFactory.Create(
            scanner,
            new InMemoryMediaObjectStore(),
            imageStore,
            new MediaUploadValidationOptions { MaxDecodedWidth = 100 });
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(),
            new MemoryStream(content),
            "photo.jpg",
            "image/jpeg",
            content.Length,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBe(true);
        imageStore.Current.Id.ShouldBe(originalImage.Id);
        scanner.ScanCount.ShouldBe(1);
    }

    [Fact]
    public async Task Accept_propagates_cancellation_during_bounded_buffering_without_scanning_or_persistence()
    {
        // Arrange
        var originalImage = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1);
        var content = CatalogTestImages.CreateJpeg(320, 160);
        using var stream = new GatedCountingReadStream(content);
        var objectStore = new InMemoryMediaObjectStore();
        var imageStore = new InMemoryPublicMediaImageStore(originalImage);
        var scanner = new StubMediaUploadScanner(MediaUploadScanResult.Passed);
        var intake = MediaImageUploadIntakeTestFactory.Create(scanner, objectStore, imageStore);
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(),
            stream,
            "photo.jpg",
            "image/jpeg",
            content.LongLength,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);
        using var requestCancellation = new CancellationTokenSource();

        // Act
        var accept = intake.Accept(request, requestCancellation.Token).AsTask();
        await stream.ReadStarted.WaitAsync(TestContext.Current.CancellationToken);
        await requestCancellation.CancelAsync();
        Func<Task> action = async () => await accept;

        // Assert
        _ = await action.ShouldThrow<TaskCanceledException>();
        scanner.ScanCount.ShouldBe(0);
        stream.BytesRead.ShouldBe(0);
        objectStore.ObjectKeys.ShouldBeEmpty();
        imageStore.Current.Id.ShouldBe(originalImage.Id);
    }

    [Fact]
    public async Task Accept_passes_a_seekable_upload_through_to_the_scanner_and_the_store_from_position_zero()
    {
        // Arrange
        var originalImage = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1);
        var content = CatalogTestImages.CreateJpeg(320, 160);
        using var stream = new GatedCountingReadStream(content);
        stream.Release();
        var scanner = new StubMediaUploadScanner(MediaUploadScanResult.Passed);
        var objectStore = new DiscardingMediaObjectStore();
        var intake = MediaImageUploadIntakeTestFactory.Create(
            scanner,
            objectStore,
            new InMemoryPublicMediaImageStore(originalImage));
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(),
            stream,
            "photo.jpg",
            "image/jpeg",
            content.LongLength,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        scanner.ScanCount.ShouldBe(1);
        scanner.LastRequest.ShouldNotBeNull().Content.ShouldBeSameAs(stream);
        objectStore.LastWriteStartPosition.ShouldBe(0);
        objectStore.LastWriteBytes.ShouldBe(content.LongLength);
        objectStore.LastWriteDeclaredLength.ShouldBe(content.LongLength);
        stream.CanRead.ShouldBeTrue();
    }

    [Fact]
    public async Task Accept_does_not_allocate_a_payload_sized_buffer_for_a_large_seekable_upload()
    {
        // Arrange
        const int payloadBytes = 4 * 1024 * 1024;
        var content = new byte[payloadBytes];
        CatalogTestImages.CreateJpeg(640, 320).CopyTo(content, 0);
        var objectStore = new DiscardingMediaObjectStore();
        var intake = MediaImageUploadIntakeTestFactory.Create(
            new StubMediaUploadScanner(MediaUploadScanResult.Passed),
            objectStore,
            new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1)));
        var warmUp = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(),
            new MemoryStream(content),
            "photo.jpg",
            "image/jpeg",
            payloadBytes,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);
        var measured = warmUp with
        {
            MediaImageId = Guid.CreateVersion7(),
            Content = new MemoryStream(content)
        };
        _ = await intake.Accept(warmUp, TestContext.Current.CancellationToken);

        // Act
        var before = GC.GetAllocatedBytesForCurrentThread();
        var result = await intake.Accept(measured, TestContext.Current.CancellationToken);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Assert
        result.IsSuccess.ShouldBeTrue();
        objectStore.LastWriteBytes.ShouldBe(payloadBytes);
        allocated.ShouldBeLessThan(payloadBytes / 4);
    }

    [Fact]
    public async Task Accept_buffers_a_slow_non_seekable_upload_chunk_by_chunk_and_scans_only_after_the_last_chunk()
    {
        // Arrange
        var content = CatalogTestImages.CreateJpeg(320, 160);
        using var stream = new GatedChunkReadStream(content, 300);
        var scanner = new StubMediaUploadScanner(MediaUploadScanResult.Passed);
        var intake = MediaImageUploadIntakeTestFactory.Create(
            scanner,
            new InMemoryMediaObjectStore(),
            new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1)));
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(),
            stream,
            "photo.jpg",
            "image/jpeg",
            content.LongLength,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);
        var chunkReads = (content.Length + 299) / 300;

        // Act
        var accept = intake.Accept(request, TestContext.Current.CancellationToken).AsTask();
        for (var i = 0; i < chunkReads; i++)
        {
            await stream.WaitForPendingRead(TestContext.Current.CancellationToken);
            scanner.ScanCount.ShouldBe(0);
            stream.ReleaseRead();
        }

        await stream.WaitForPendingRead(TestContext.Current.CancellationToken);
        stream.ReleaseRead();
        var result = await accept;

        // Assert
        result.IsSuccess.ShouldBeTrue();
        scanner.ScanCount.ShouldBe(1);
        scanner.LastRequest.ShouldNotBeNull().Content.ShouldNotBeSameAs(stream);
        stream.BytesRead.ShouldBe(content.LongLength);
        stream.LargestRequestedRead.ShouldBeLessThanOrEqualTo(128 * 1024);
    }

    [Fact]
    public async Task Accept_accepts_a_non_seekable_upload_exactly_at_the_size_limit()
    {
        // Arrange
        var content = CatalogTestImages.CreateJpeg(320, 160);
        using var stream = new GatedChunkReadStream(content, 4096);
        stream.ReleaseAll();
        var scanner = new StubMediaUploadScanner(MediaUploadScanResult.Passed);
        var intake = MediaImageUploadIntakeTestFactory.Create(
            scanner,
            new InMemoryMediaObjectStore(),
            new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1)),
            new MediaUploadValidationOptions { MaxLengthBytes = content.Length });
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(),
            stream,
            "photo.jpg",
            "image/jpeg",
            content.LongLength,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        scanner.ScanCount.ShouldBe(1);
    }

    [Fact]
    public async Task Accept_rejects_a_non_seekable_upload_one_byte_over_the_limit_without_scanning_even_when_the_declared_length_is_within_it()
    {
        // Arrange
        var originalImage = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1);
        var content = CatalogTestImages.CreateJpeg(320, 160);
        using var stream = new GatedChunkReadStream(content, 4096);
        stream.ReleaseAll();
        var objectStore = new InMemoryMediaObjectStore();
        var imageStore = new InMemoryPublicMediaImageStore(originalImage);
        var scanner = new StubMediaUploadScanner(MediaUploadScanResult.Passed);
        var intake = MediaImageUploadIntakeTestFactory.Create(
            scanner,
            objectStore,
            imageStore,
            new MediaUploadValidationOptions { MaxLengthBytes = content.Length - 1 });
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(),
            stream,
            "photo.jpg",
            "image/jpeg",
            content.Length - 1,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        scanner.ScanCount.ShouldBe(0);
        objectStore.ObjectKeys.ShouldBeEmpty();
        imageStore.Current.Id.ShouldBe(originalImage.Id);
    }

    [Fact]
    public async Task Accept_rejects_a_declared_length_smaller_than_the_received_bytes_before_scanning_or_metadata()
    {
        // Arrange
        var originalImage = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1);
        var content = CatalogTestImages.CreateJpeg(320, 160);
        using var stream = new GatedChunkReadStream(content, 4096);
        stream.ReleaseAll();
        var objectStore = new InMemoryMediaObjectStore();
        var imageStore = new InMemoryPublicMediaImageStore(originalImage);
        var scanner = new StubMediaUploadScanner(MediaUploadScanResult.Passed);
        var intake = MediaImageUploadIntakeTestFactory.Create(scanner, objectStore, imageStore);
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(),
            stream,
            "photo.jpg",
            "image/jpeg",
            content.Length - 1,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        scanner.ScanCount.ShouldBe(0);
        objectStore.ObjectKeys.ShouldBeEmpty();
        imageStore.Current.Id.ShouldBe(originalImage.Id);
    }

    [Fact]
    public async Task Accept_cancels_a_slow_upload_after_partial_data_without_scanning_or_persistence()
    {
        // Arrange
        var originalImage = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1);
        var content = CatalogTestImages.CreateJpeg(320, 160);
        using var stream = new GatedChunkReadStream(content, 300);
        var objectStore = new InMemoryMediaObjectStore();
        var imageStore = new InMemoryPublicMediaImageStore(originalImage);
        var scanner = new StubMediaUploadScanner(MediaUploadScanResult.Passed);
        var intake = MediaImageUploadIntakeTestFactory.Create(scanner, objectStore, imageStore);
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(),
            stream,
            "photo.jpg",
            "image/jpeg",
            content.LongLength,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);
        using var requestCancellation = new CancellationTokenSource();

        // Act
        var accept = intake.Accept(request, requestCancellation.Token).AsTask();
        await stream.WaitForPendingRead(TestContext.Current.CancellationToken);
        stream.ReleaseRead();
        await stream.WaitForPendingRead(TestContext.Current.CancellationToken);
        await requestCancellation.CancelAsync();
        Func<Task> action = async () => await accept;

        // Assert
        _ = await action.ShouldThrow<OperationCanceledException>();
        scanner.ScanCount.ShouldBe(0);
        stream.BytesRead.ShouldBe(300);
        objectStore.ObjectKeys.ShouldBeEmpty();
        imageStore.Current.Id.ShouldBe(originalImage.Id);
    }

    [Fact]
    public async Task Accept_reports_the_scanner_rejection_instead_of_a_decode_failure_when_the_content_is_malformed()
    {
        // Arrange
        var content = CatalogTestImages.CreateJpeg(320, 160)[..64];
        var scanner = new StubMediaUploadScanner(new MediaUploadScanResult(MediaUploadScanStatus.Rejected, "malware detected"));
        var intake = MediaImageUploadIntakeTestFactory.Create(
            scanner,
            new InMemoryMediaObjectStore(),
            new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1)));
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(),
            new MemoryStream(content),
            "photo.jpg",
            "image/jpeg",
            content.Length,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var scanErrors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull()["Status"];
        scanErrors.ShouldBe(["Media upload did not pass security scanning."]);
        scanner.ScanCount.ShouldBe(1);
    }
}
