using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class MediaImageUploadIntakeTests
{
    [Fact]
    public async Task Accept_stores_metadata_with_generated_object_key_when_scan_passes()
    {
        // Arrange
        var mediaImageId = Guid.CreateVersion7();
        var content = CatalogTestImages.CreateJpeg(640, 320);
        var objectStore = new InMemoryMediaObjectStore();
        var imageStore = new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1));
        var scanner = new StubMediaUploadScanner(MediaUploadScanResult.Passed);
        var intake = new MediaImageUploadIntake(new MediaUploadValidator(), scanner, objectStore, imageStore);
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
        objectStore.ObjectKeys.ShouldContain($"media/{mediaImageId:N}/original.jpg");
        objectStore.ObjectKeys.ShouldNotContain("client-photo.jpg");
        result.Value.OriginalStoredEvent.SourceObjectKey.ShouldBe($"media/{mediaImageId:N}/original.jpg");
        scanner.LastRequest.ShouldNotBeNull();
        scanner.LastRequest.ObjectKey.ShouldBe($"media/{mediaImageId:N}/original.jpg");
    }

    [Fact]
    public async Task Accept_allows_disabled_scanner_for_development_intake()
    {
        // Arrange
        var mediaImageId = Guid.CreateVersion7();
        var content = CatalogTestImages.CreateJpeg(320, 160);
        var imageStore = new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1));
        var intake = new MediaImageUploadIntake(
            new MediaUploadValidator(),
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
    public async Task Accept_rejects_upload_when_scanner_rejects()
    {
        // Arrange
        var originalImage = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1);
        var content = CatalogTestImages.CreateJpeg(320, 160);
        var objectStore = new InMemoryMediaObjectStore();
        var imageStore = new InMemoryPublicMediaImageStore(originalImage);
        var intake = new MediaImageUploadIntake(
            new MediaUploadValidator(),
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
        var intake = new MediaImageUploadIntake(
            new MediaUploadValidator(),
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
        result.ErrorDetails.Detail.ShouldBe("scanner unavailable");
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
        var intake = new MediaImageUploadIntake(
            new MediaUploadValidator(),
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
        result.ErrorDetails.Detail.ShouldBe("scanner timeout");
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
        var intake = new MediaImageUploadIntake(
            new MediaUploadValidator(),
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
        result.ErrorDetails.Detail.ShouldBe("scanner crashed");
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
        var intake = new MediaImageUploadIntake(
            new MediaUploadValidator(new MediaUploadValidationOptions { MaxLengthBytes = 3 }),
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
        var intake = new MediaImageUploadIntake(new MediaUploadValidator(), scanner, objectStore, imageStore);
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
        var intake = new MediaImageUploadIntake(new MediaUploadValidator(), scanner, objectStore, imageStore);
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
        var intake = new MediaImageUploadIntake(
            new MediaUploadValidator(),
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
        var intake = new MediaImageUploadIntake(new MediaUploadValidator(), scanner, new InMemoryMediaObjectStore(), imageStore);
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
        scanner.ScanCount.ShouldBe(0);
    }

    [Fact]
    public async Task Accept_rejects_decoded_images_over_configured_limits()
    {
        // Arrange
        var originalImage = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1);
        var content = CatalogTestImages.CreateJpeg(320, 160);
        var imageStore = new InMemoryPublicMediaImageStore(originalImage);
        var scanner = new StubMediaUploadScanner(MediaUploadScanResult.Passed);
        var intake = new MediaImageUploadIntake(
            new MediaUploadValidator(),
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
        scanner.ScanCount.ShouldBe(0);
    }
}
