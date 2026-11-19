using ImageMagick;
using PandaBear.Results;
using PandaBear.Testing;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Testing.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests.Media;

[Trait(TestTraitNames.AreaName, TestTraits.CatalogArea)]
[Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.MediaCapability)]
public sealed class MediaImageUploadIntakeHardeningTests
{
    [Theory]
    [InlineData("IMAGE/JPEG", "image/jpeg")]
    [InlineData(" Image/Jpeg ", "image/jpeg")]
    public async Task Accept_stores_the_canonical_content_type_instead_of_the_declared_string(string declared, string canonical)
    {
        // Arrange
        var content = CatalogTestImages.CreateJpeg(64, 32);
        var imageStore = new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1));
        var intake = IntakeTestSetup.CreateIntake(MediaObjectStoreTestFactory.Create(), imageStore);

        // Act
        var result = await intake.Accept(IntakeTestSetup.CreateRequest(content, "photo.jpg", declared), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        imageStore.Current.ContentType.ShouldBe(canonical);
    }

    [Theory]
    [InlineData("photo.png", "image/png")]
    [InlineData("photo.webp", "image/webp")]
    [InlineData("photo.avif", "image/avif")]
    public async Task Accept_refuses_content_whose_signature_class_differs_from_the_declared_type_with_fixed_copy(
        string fileName, string declaredContentType)
    {
        // Arrange
        var content = CatalogTestImages.CreateJpeg(64, 32);
        var scanner = new StubMediaUploadScanner(MediaUploadScanResult.Passed);
        var objectStore = MediaObjectStoreTestFactory.Create();
        var intake = IntakeTestSetup.CreateIntake(objectStore, new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1)), scanner);

        // Act
        var result = await intake.Accept(IntakeTestSetup.CreateRequest(content, fileName, declaredContentType), TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var errors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        errors["Content"].ShouldBe(["File signature does not match the allowed content type."]);
        scanner.ScanCount.ShouldBe(0);
        objectStore.ObjectKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task Accept_refuses_a_file_that_only_starts_like_an_image()
    {
        // Arrange
        byte[] content = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];
        var scanner = new StubMediaUploadScanner(MediaUploadScanResult.Passed);
        var objectStore = MediaObjectStoreTestFactory.Create();
        var intake = IntakeTestSetup.CreateIntake(objectStore, new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1)), scanner);

        // Act
        var result = await intake.Accept(IntakeTestSetup.CreateRequest(content, "photo.jpg", "image/jpeg"), TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var errors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        errors["Content"].ShouldBe([PandaBear.UploadSanitizing.Images.ImageSanitizer.MalformedMessage]);
        scanner.ScanCount.ShouldBe(1);
        objectStore.ObjectKeys.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(MediaUploadScanStatus.Rejected, ResultStatus.Invalid)]
    [InlineData(MediaUploadScanStatus.Pending, ResultStatus.Invalid)]
    [InlineData(MediaUploadScanStatus.Failed, ResultStatus.Unavailable)]
    public async Task Accept_reads_only_the_bounded_signature_beyond_the_lease_pass_when_scan_refuses(
        MediaUploadScanStatus scanStatus,
        ResultStatus expected)
    {
        // Arrange
        var content = CatalogTestImages.CreateJpeg(320, 160);
        using var stream = new TrackingReadStream(content, seekable: true) { MaxBytesPerRead = 5 };
        var scanner = new StubMediaUploadScanner(new MediaUploadScanResult(scanStatus));
        var objectStore = MediaObjectStoreTestFactory.Create();
        var original = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1);
        var imageStore = new InMemoryPublicMediaImageStore(original);
        var intake = IntakeTestSetup.CreateIntake(objectStore, imageStore, scanner);
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(), stream, "photo.jpg", "image/jpeg", content.LongLength,
            "Cyclists in the mountains", [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(expected);
        scanner.ScanCount.ShouldBe(1);
        scanner.LastRequest.ShouldNotBeNull().Content.ShouldBeSameAs(stream);
        stream.BytesRead.ShouldBe(content.Length + 12);
        stream.Position.ShouldBe(0);
        stream.IsDisposed.ShouldBeFalse();
        objectStore.ObjectKeys.ShouldBeEmpty();
        imageStore.Current.Id.ShouldBe(original.Id);
    }

    [Fact]
    public async Task Accept_accepts_an_animated_webp_and_stores_its_canonical_type()
    {
        // Arrange
        var content = CatalogTestImages.CreateAnimatedWebP(64, 32);
        var imageStore = new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1));
        var intake = IntakeTestSetup.CreateIntake(MediaObjectStoreTestFactory.Create(), imageStore);

        // Act
        var result = await intake.Accept(IntakeTestSetup.CreateRequest(content, "photo.webp", "image/webp"), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        imageStore.Current.ContentType.ShouldBe("image/webp");
    }

    [Fact]
    public async Task Accept_accepts_a_still_png()
    {
        // Arrange
        var content = CatalogTestImages.CreatePng(64, 32);
        var imageStore = new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1));
        var intake = IntakeTestSetup.CreateIntake(MediaObjectStoreTestFactory.Create(), imageStore);

        // Act
        var result = await intake.Accept(IntakeTestSetup.CreateRequest(content, "photo.png", "image/png"), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        imageStore.Current.ContentType.ShouldBe("image/png");
    }

    [Fact]
    public async Task Accept_accepts_a_still_avif_after_scanning()
    {
        // Arrange
        using var image = new MagickImage(MagickColors.Blue, 64, 32) { Format = MagickFormat.Avif };
        var content = image.ToByteArray();
        var scanner = new StubMediaUploadScanner(MediaUploadScanResult.Passed);
        var objectStore = MediaObjectStoreTestFactory.Create();
        var imageStore = new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1));
        var intake = IntakeTestSetup.CreateIntake(objectStore, imageStore, scanner);

        // Act
        var result = await intake.Accept(IntakeTestSetup.CreateRequest(content, "photo.avif", "image/avif"), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        scanner.ScanCount.ShouldBe(1);
        imageStore.Current.ContentType.ShouldBe("image/avif");
    }

    [Fact]
    public async Task Accept_checks_the_avif_brands_after_scanning_a_candidate()
    {
        // Arrange
        byte[] content = [0, 0, 0, 16, .. "ftyp"u8, .. "mif1"u8, 0, 0, 0, 0];
        var scanner = new StubMediaUploadScanner(MediaUploadScanResult.Passed);
        var objectStore = MediaObjectStoreTestFactory.Create();
        var original = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1);
        var imageStore = new InMemoryPublicMediaImageStore(original);
        var intake = IntakeTestSetup.CreateIntake(objectStore, imageStore, scanner);

        // Act
        var result = await intake.Accept(IntakeTestSetup.CreateRequest(content, "photo.avif", "image/avif"), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        var errors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        errors["Content"].ShouldBe(["File signature does not match the allowed content type."]);
        scanner.ScanCount.ShouldBe(1);
        objectStore.ObjectKeys.ShouldBeEmpty();
        imageStore.Current.Id.ShouldBe(original.Id);
    }

    [Fact]
    public async Task Accept_still_deletes_the_stored_object_when_the_request_is_cancelled_after_the_write()
    {
        // Arrange
        var mediaImageId = Guid.CreateVersion7();
        var content = CatalogTestImages.CreateJpeg(64, 32);
        var objectStore = MediaObjectStoreTestFactory.Create();
        using var cancellation = new CancellationTokenSource();
        var imageStore = new CancelingPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(mediaImageId, 1), cancellation);
        var intake = IntakeTestSetup.CreateIntake(objectStore, imageStore);

        // Act
        Func<Task> action = async () => await intake.Accept(IntakeTestSetup.CreateRequest(content, "photo.jpg", "image/jpeg", mediaImageId), cancellation.Token);

        // Assert
        _ = await action.ShouldThrowAssignableTo<OperationCanceledException>();
        objectStore.ObjectKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task Accept_keeps_the_original_exception_when_the_compensating_delete_fails()
    {
        // Arrange
        var mediaImageId = Guid.CreateVersion7();
        var content = CatalogTestImages.CreateJpeg(64, 32);
        var objectStore = MediaObjectStoreTestFactory.Create();
        objectStore.FailAlways(ObjectStoreOperation.Delete, new IOException("delete failed"));
        var imageStore = new ThrowingPublicMediaImageStore(
            PublicMediaImageTestFactory.CreatePendingImage(mediaImageId, 1),
            new InvalidOperationException("database unavailable"));
        var intake = IntakeTestSetup.CreateIntake(objectStore, imageStore);

        // Act
        Func<Task> action = async () => await intake.Accept(IntakeTestSetup.CreateRequest(content, "photo.jpg", "image/jpeg", mediaImageId), TestContext.Current.CancellationToken);

        // Assert
        var thrown = await action.ShouldThrow<InvalidOperationException>();
        thrown.Message.ShouldBe("database unavailable");
        objectStore.CallCount(ObjectStoreOperation.Delete).ShouldBe(1);
    }
}
