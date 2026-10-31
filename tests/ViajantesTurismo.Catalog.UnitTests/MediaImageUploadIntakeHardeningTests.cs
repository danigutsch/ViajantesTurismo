using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.CatalogArea)]
[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.MediaCapability)]
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
        var intake = IntakeTestSetup.CreateIntake(new InMemoryMediaObjectStore(), imageStore);

        // Act
        var result = await intake.Accept(IntakeTestSetup.CreateRequest(content, "photo.jpg", declared), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        imageStore.Current.ContentType.ShouldBe(canonical);
    }

    [Fact]
    public async Task Accept_refuses_content_whose_signature_class_differs_from_the_declared_type_with_fixed_copy()
    {
        // Arrange
        var content = CatalogTestImages.CreateJpeg(64, 32);
        var scanner = new StubMediaUploadScanner(MediaUploadScanResult.Passed);
        var objectStore = new InMemoryMediaObjectStore();
        var intake = IntakeTestSetup.CreateIntake(objectStore, new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1)), scanner);

        // Act
        var result = await intake.Accept(IntakeTestSetup.CreateRequest(content, "photo.png", "image/png"), TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var errors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        errors["HeaderBytes"].ShouldBe(["File signature does not match the allowed content type."]);
        scanner.ScanCount.ShouldBe(0);
        objectStore.ObjectKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task Accept_accepts_an_animated_webp_and_stores_its_canonical_type()
    {
        // Arrange
        var content = CatalogTestImages.CreateAnimatedWebP(64, 32);
        var imageStore = new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1));
        var intake = IntakeTestSetup.CreateIntake(new InMemoryMediaObjectStore(), imageStore);

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
        var intake = IntakeTestSetup.CreateIntake(new InMemoryMediaObjectStore(), imageStore);

        // Act
        var result = await intake.Accept(IntakeTestSetup.CreateRequest(content, "photo.png", "image/png"), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        imageStore.Current.ContentType.ShouldBe("image/png");
    }

    [Fact]
    public async Task Accept_still_deletes_the_stored_object_when_the_request_is_cancelled_after_the_write()
    {
        // Arrange
        var mediaImageId = Guid.CreateVersion7();
        var content = CatalogTestImages.CreateJpeg(64, 32);
        var objectStore = new InMemoryMediaObjectStore();
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
        var objectStore = new FailingDeleteObjectStore(new InMemoryMediaObjectStore());
        var imageStore = new ThrowingPublicMediaImageStore(
            PublicMediaImageTestFactory.CreatePendingImage(mediaImageId, 1),
            new InvalidOperationException("database unavailable"));
        var intake = IntakeTestSetup.CreateIntake(objectStore, imageStore);

        // Act
        Func<Task> action = async () => await intake.Accept(IntakeTestSetup.CreateRequest(content, "photo.jpg", "image/jpeg", mediaImageId), TestContext.Current.CancellationToken);

        // Assert
        var thrown = await action.ShouldThrow<InvalidOperationException>();
        thrown.Message.ShouldBe("database unavailable");
        objectStore.DeleteCallCount.ShouldBe(1);
    }
}
