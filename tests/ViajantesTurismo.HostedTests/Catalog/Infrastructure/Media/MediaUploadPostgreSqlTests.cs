using ImageMagick;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SharedKernel.Results;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Infrastructure.Media;
using ViajantesTurismo.Catalog.Infrastructure.Tours;
using ViajantesTurismo.Catalog.Testing.Infrastructure;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Media;

[Trait(TestTraitNames.CategoryName, SharedKernel.IntegrationTesting.IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class MediaUploadPostgreSqlTests(HostedTestFixture shared)
{
    [Fact]
    public async Task Fingerprint_migration_preserves_legacy_images_and_existing_tour_retirements()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await GalleryPlacementPostgreSqlScenario.Create(shared, ct);
        await scenario.MigrateToLatest(ct);
        var tourId = Guid.CreateVersion7();
        var image = PublicMediaImageTestFactory.CreateReadyImage(
            tourId, "legacy-original.jpg", "legacy-640.jpg", "sha256:legacy", "Legacy caption", 0, true);
        await using var seed = scenario.CreateDbContext();
        await new EfPublicMediaImageStore(seed).Upsert(image, ct);
        var retirement = new CatalogTourRetirementEntity
        {
            AdminTourId = Guid.CreateVersion7(),
            RetirementId = Guid.CreateVersion7(),
            WithdrawnAt = new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero)
        };
        seed.CatalogTourRetirements.Add(retirement);
        await seed.SaveChangesAsync(ct);

        // Act
        await using var migrations = scenario.CreateDbContext();
        var migrator = migrations.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20261119161057_RetainTourRetirements", ct);
        await migrator.MigrateAsync(cancellationToken: ct);
        await using var read = scenario.CreateDbContext();
        var retained = await new EfPublicMediaImageStore(read).GetImage(image.Id, ct);
        var fingerprint = await read.PublicMediaImages.Where(row => row.Id == image.Id)
            .Select(row => EF.Property<string?>(row, PublicMediaImageSchema.UploadFingerprint))
            .SingleAsync(ct);
        var retainedRetirement = await read.CatalogTourRetirements.SingleAsync(row => row.AdminTourId == retirement.AdminTourId, ct);

        // Assert
        retained.ShouldNotBeNull();
        retained.SourceObjectKey.ShouldBe(image.SourceObjectKey);
        retained.AltText.ShouldBe(image.AltText);
        retained.TourLinks.ShouldHaveSingleItem().CatalogTourId.ShouldBe(tourId);
        retained.ResponsiveVariants.ShouldHaveSingleItem().ObjectKey.ShouldBe("legacy-640.jpg");
        fingerprint.ShouldBeNull();
        retainedRetirement.RetirementId.ShouldBe(retirement.RetirementId);
        retainedRetirement.WithdrawnAt.ShouldBe(retirement.WithdrawnAt);
    }

    [Fact]
    public async Task Concurrent_identical_uploads_reuse_one_image_event_and_original()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await GalleryPlacementPostgreSqlScenario.Create(shared, ct);
        await scenario.MigrateToLatest(ct);
        var objects = MediaObjectStoreTestFactory.Create();
        var scanner = new ConcurrentUploadScanner(2);
        var tourId = Guid.CreateVersion7();
        using var image = new MagickImage(MagickColors.Blue, 64, 32);
        var bytes = image.ToByteArray(MagickFormat.Jpeg);
        using var firstContent = new MemoryStream(bytes);
        using var secondContent = new MemoryStream(bytes);
        var firstUpload = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(), firstContent, "first.jpg", "image/jpeg", bytes.Length, "Blue boat", [new(tourId, 0, true)]);
        var secondUpload = firstUpload with
        {
            MediaImageId = Guid.CreateVersion7(),
            Content = secondContent,
            FileName = "second.jpeg",
            AltText = " Blue  boat ",
            TourLinks = [new(tourId, 1, false)]
        };

        // Act
        var uploads = await Task.WhenAll(
            scenario.AcceptUpload(firstUpload, scanner, objects, ct).AsTask(),
            scenario.AcceptUpload(secondUpload, scanner, objects, ct).AsTask());
        var (images, events) = await scenario.ReadUploadCounts(ct);

        // Assert
        scanner.Scans.ShouldBe(2);
        uploads[0].IsSuccess.ShouldBeTrue();
        uploads[1].IsSuccess.ShouldBeTrue();
        uploads[1].Value.Image.Id.ShouldBe(uploads[0].Value.Image.Id);
        var enqueued = uploads.Where(upload => upload.Value.OriginalStoredEvent is not null);
        enqueued.ShouldHaveSingleItem();
        images.ShouldBe(1);
        events.ShouldBe(1L);
        objects.Puts.Count.ShouldBe(1);
        objects.ObjectKeys.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("blank-tag")]
    [InlineData("negative-order")]
    public async Task Matching_clean_bytes_do_not_reuse_an_image_when_editorial_metadata_is_invalid(string invalidField)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await GalleryPlacementPostgreSqlScenario.Create(shared, ct);
        await scenario.MigrateToLatest(ct);
        var objects = MediaObjectStoreTestFactory.Create();
        var scanner = new ConcurrentUploadScanner(1);
        var tourId = Guid.CreateVersion7();
        using var image = new MagickImage(MagickColors.Blue, 64, 32);
        var bytes = image.ToByteArray(MagickFormat.Jpeg);
        using var firstContent = new MemoryStream(bytes);
        using var secondContent = new MemoryStream(bytes);
        var first = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(), firstContent, "first.jpg", "image/jpeg", bytes.Length, "Blue boat",
            [new(tourId, 0, true)], Tags: ["river"]);
        var second = first with
        {
            MediaImageId = Guid.CreateVersion7(),
            Content = secondContent,
            Tags = invalidField == "blank-tag" ? ["river", " "] : first.Tags,
            TourLinks = invalidField == "negative-order" ? [new(tourId, -1, false)] : first.TourLinks,
        };

        // Act
        var accepted = await scenario.AcceptUpload(first, scanner, objects, ct);
        var rejected = await scenario.AcceptUpload(second, scanner, objects, ct);
        var (images, events) = await scenario.ReadUploadCounts(ct);

        // Assert
        accepted.IsSuccess.ShouldBeTrue();
        rejected.Status.ShouldBe(ResultStatus.Invalid);
        images.ShouldBe(1);
        events.ShouldBe(1L);
        objects.ObjectKeys.ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData("alt")]
    [InlineData("caption")]
    [InlineData("attribution")]
    [InlineData("copyright")]
    [InlineData("tags")]
    [InlineData("gallery")]
    [InlineData("bytes")]
    public async Task Changing_content_editorial_attributes_or_gallery_creates_independent_images(string difference)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await GalleryPlacementPostgreSqlScenario.Create(shared, ct);
        await scenario.MigrateToLatest(ct);
        var objects = MediaObjectStoreTestFactory.Create();
        var scanner = new ConcurrentUploadScanner(1);
        var tourId = Guid.CreateVersion7();
        using var blue = new MagickImage(MagickColors.Blue, 64, 32);
        var original = blue.ToByteArray(MagickFormat.Jpeg);
        blue.Comment = "The upload sanitizer removes this JPEG comment.";
        var changed = difference == "bytes" ? blue.ToByteArray(MagickFormat.Jpeg) : original;
        using var firstContent = new MemoryStream(original);
        using var secondContent = new MemoryStream(changed);
        var firstUpload = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(), firstContent, "first.jpg", "image/jpeg", original.Length, "Boat", [new(tourId, 0, true)]);
        var secondUpload = firstUpload with
        {
            MediaImageId = Guid.CreateVersion7(),
            Content = secondContent,
            Length = changed.Length,
            TourLinks = [new(tourId, 1, false)]
        };
        secondUpload = difference switch
        {
            "alt" => secondUpload with { AltText = "Sailing boat" },
            "caption" => secondUpload with { Caption = "On the lake" },
            "attribution" => secondUpload with { Attribution = "Tour operator" },
            "copyright" => secondUpload with { Copyright = "Creator" },
            "tags" => secondUpload with { Tags = ["sailing"] },
            "gallery" => secondUpload with { TourLinks = [new(Guid.CreateVersion7(), 0, true)] },
            _ => secondUpload
        };

        // Act
        var first = await scenario.AcceptUpload(firstUpload, scanner, objects, ct);
        var second = await scenario.AcceptUpload(secondUpload, scanner, objects, ct);
        var (images, events) = await scenario.ReadUploadCounts(ct);

        // Assert
        first.IsSuccess.ShouldBeTrue();
        second.IsSuccess.ShouldBeTrue();
        second.Value.Image.Id.ShouldBe(secondUpload.MediaImageId);
        second.Value.Image.Id.ShouldNotBe(first.Value.Image.Id);
        images.ShouldBe(2);
        events.ShouldBe(2L);
        objects.Puts.Count.ShouldBe(2);
        objects.ObjectKeys.Count.ShouldBe(2);
        if (difference == "bytes")
        {
            second.Value.Image.Checksum.ShouldBe(first.Value.Image.Checksum);
        }
    }

    [Fact]
    public async Task Failed_gallery_save_removes_the_candidate_object_and_allows_retry()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await GalleryPlacementPostgreSqlScenario.Create(shared, ct);
        await scenario.MigrateToLatest(ct);
        var objects = MediaObjectStoreTestFactory.Create();
        var scanner = new ConcurrentUploadScanner(1);
        var tourId = Guid.CreateVersion7();
        using var image = new MagickImage(MagickColors.Blue, 64, 32);
        var bytes = image.ToByteArray(MagickFormat.Jpeg);
        using var occupiedContent = new MemoryStream(bytes);
        using var failedContent = new MemoryStream(bytes);
        using var retryContent = new MemoryStream(bytes);
        var occupied = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(), occupiedContent, "boat.jpg", "image/jpeg", bytes.Length, "First caption", [new(tourId, 0, true)]);
        var candidate = occupied with
        {
            MediaImageId = Guid.CreateVersion7(),
            Content = failedContent,
            AltText = "Second caption"
        };
        var retry = candidate with
        {
            Content = retryContent,
            TourLinks = [new(tourId, 1, false)]
        };
        var accepted = await scenario.AcceptUpload(occupied, scanner, objects, ct);

        // Act
        var failed = await scenario.AcceptUpload(candidate, scanner, objects, ct);
        var retried = await scenario.AcceptUpload(retry, scanner, objects, ct);
        var (images, events) = await scenario.ReadUploadCounts(ct);

        // Assert
        accepted.IsSuccess.ShouldBeTrue();
        failed.Status.ShouldBe(ResultStatus.Conflict);
        retried.IsSuccess.ShouldBeTrue();
        retried.Value.Image.Id.ShouldBe(candidate.MediaImageId);
        images.ShouldBe(2);
        events.ShouldBe(2L);
        objects.Puts.Count.ShouldBe(3);
        objects.Deletes.Count.ShouldBe(1);
        objects.ObjectKeys.Count.ShouldBe(2);
        objects.ObjectKeys.ShouldContain(accepted.Value.Image.SourceObjectKey);
        objects.ObjectKeys.ShouldContain(retried.Value.Image.SourceObjectKey);
    }

    [Theory]
    [InlineData(MediaUploadScanStatus.Rejected, ResultStatus.Invalid)]
    [InlineData(MediaUploadScanStatus.Failed, ResultStatus.Unavailable)]
    public async Task Duplicate_upload_honors_the_current_scanner_verdict(MediaUploadScanStatus verdict, ResultStatus status)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await GalleryPlacementPostgreSqlScenario.Create(shared, ct);
        await scenario.MigrateToLatest(ct);
        var objects = MediaObjectStoreTestFactory.Create();
        var deniedScanner = new ConcurrentUploadScanner(1, verdict);
        using var image = new MagickImage(MagickColors.Blue, 64, 32);
        var bytes = image.ToByteArray(MagickFormat.Jpeg);
        using var firstContent = new MemoryStream(bytes);
        using var retryContent = new MemoryStream(bytes);
        var upload = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(), firstContent, "boat.jpg", "image/jpeg", bytes.Length, "Boat", [new(Guid.CreateVersion7(), 0, true)]);
        var prior = await scenario.AcceptUpload(upload, new ConcurrentUploadScanner(1), objects, ct);

        // Act
        var retry = await scenario.AcceptUpload(
            upload with
            {
                MediaImageId = Guid.CreateVersion7(),
                Content = retryContent
            }, deniedScanner, objects, ct);
        var (images, events) = await scenario.ReadUploadCounts(ct);

        // Assert
        prior.IsSuccess.ShouldBeTrue();
        retry.Status.ShouldBe(status);
        deniedScanner.Scans.ShouldBe(1);
        images.ShouldBe(1);
        events.ShouldBe(1L);
        objects.Puts.Count.ShouldBe(1);
        objects.ObjectKeys.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Replaying_after_processing_reuses_the_image_until_its_editorial_content_changes()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await GalleryPlacementPostgreSqlScenario.Create(shared, ct);
        await scenario.MigrateToLatest(ct);
        var objects = MediaObjectStoreTestFactory.Create();
        var scanner = new ConcurrentUploadScanner(1);
        var tourId = Guid.CreateVersion7();
        using var image = new MagickImage(MagickColors.Blue, 64, 32);
        var bytes = image.ToByteArray(MagickFormat.Jpeg);
        using var originalContent = new MemoryStream(bytes);
        using var replayContent = new MemoryStream(bytes);
        using var nextContent = new MemoryStream(bytes);
        var initial = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(), originalContent, "boat.jpg", "image/jpeg", bytes.Length, "Boat", [new(tourId, 0, true)]);
        var accepted = await scenario.AcceptUpload(initial, scanner, objects, ct);
        accepted.IsSuccess.ShouldBeTrue();
        var processed = accepted.Value.Image.FailProcessing();
        processed.IsSuccess.ShouldBeTrue();
        await using var processingContext = scenario.CreateDbContext();
        await new EfPublicMediaImageStore(processingContext).Upsert(processed.Value, ct);

        // Act
        var replay = await scenario.AcceptUpload(
            initial with
            {
                MediaImageId = Guid.CreateVersion7(),
                Content = replayContent,
                TourLinks = [new(tourId, 1, false)]
            },
            scanner, objects, ct);
        var edited = processed.Value.SetReviewedAccessibilityText(PublicContentCultures.Supported.Default, "Sailing boat", null, false);
        await using var editingContext = scenario.CreateDbContext();
        var imageStore = new EfPublicMediaImageStore(editingContext);
        await imageStore.Upsert(processed.Value, ct);
        var sameOriginalWithInitialText = await scenario.AcceptUpload(
            initial with
            {
                MediaImageId = Guid.CreateVersion7(),
                Content = nextContent,
                TourLinks = [new(tourId, 1, false)]
            },
            scanner, objects, ct);
        var storedEditedImage = await imageStore.GetImage(initial.MediaImageId, ct);
        var (images, events) = await scenario.ReadUploadCounts(ct);

        // Assert
        replay.IsSuccess.ShouldBeTrue();
        replay.Value.Image.Id.ShouldBe(initial.MediaImageId);
        replay.Value.Image.ProcessingStatus.ShouldBe(MediaImageProcessingStatus.Failed);
        edited.IsSuccess.ShouldBeTrue();
        sameOriginalWithInitialText.IsSuccess.ShouldBeTrue();
        sameOriginalWithInitialText.Value.Image.Id.ShouldNotBe(initial.MediaImageId);
        storedEditedImage.ShouldNotBeNull().AltText.ShouldBe("Sailing boat");
        images.ShouldBe(2);
        events.ShouldBe(2L);
        objects.ObjectKeys.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Duplicate_upload_honors_tighter_decode_limits()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await GalleryPlacementPostgreSqlScenario.Create(shared, ct);
        await scenario.MigrateToLatest(ct);
        var objects = MediaObjectStoreTestFactory.Create();
        var scanner = new ConcurrentUploadScanner(1);
        using var image = new MagickImage(MagickColors.Blue, 64, 32);
        var bytes = image.ToByteArray(MagickFormat.Jpeg);
        using var firstContent = new MemoryStream(bytes);
        using var retryContent = new MemoryStream(bytes);
        var upload = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(), firstContent, "boat.jpg", "image/jpeg", bytes.Length, "Boat", [new(Guid.CreateVersion7(), 0, true)]);
        var prior = await scenario.AcceptUpload(upload, scanner, objects, ct);

        // Act
        var retry = await scenario.AcceptUpload(
            upload with
            {
                MediaImageId = Guid.CreateVersion7(),
                Content = retryContent
            }, scanner, objects, ct,
            new MediaUploadValidationOptions { MaxDecodedPixelCount = 1024 });
        var (images, events) = await scenario.ReadUploadCounts(ct);

        // Assert
        prior.IsSuccess.ShouldBeTrue();
        retry.Status.ShouldBe(ResultStatus.Invalid);
        images.ShouldBe(1);
        events.ShouldBe(1L);
        objects.ObjectKeys.Count.ShouldBe(1);
    }
}
