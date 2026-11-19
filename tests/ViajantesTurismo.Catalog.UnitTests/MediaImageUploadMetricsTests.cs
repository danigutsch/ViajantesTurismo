using PandaBear.Results;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Testing.Infrastructure;
using ViajantesTurismo.Catalog.UnitTests.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class MediaImageUploadMetricsTests
{
    [Theory]
    [InlineData(MediaUploadScanStatus.Passed, false, ResultStatus.Ok, "accepted", "scan,store")]
    [InlineData(MediaUploadScanStatus.Passed, true, ResultStatus.Invalid, "invalid_file", "")]
    [InlineData(MediaUploadScanStatus.Rejected, false, ResultStatus.Invalid, "scan_rejected", "scan")]
    [InlineData(MediaUploadScanStatus.Pending, false, ResultStatus.Invalid, "scan_rejected", "scan")]
    [InlineData(MediaUploadScanStatus.Failed, false, ResultStatus.Unavailable, "scanner_unavailable", "scan")]
    public async Task Accept_records_one_bounded_outcome_and_only_the_stages_that_run(
        MediaUploadScanStatus scanStatus,
        bool mismatchedLength,
        ResultStatus expectedStatus,
        string expectedOutcome,
        string expectedStages)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(expectedStages);
        var data = CatalogTestImages.CreateJpeg(320, 160);
        using var content = new MemoryStream(data);
        var request = MediaImageUploadMetricsTestSupport.CreateRequest(content, mismatchedLength ? data.Length - 1 : data.Length);
        var intake = MediaImageUploadIntakeTestFactory.Create(
            new StubMediaUploadScanner(new MediaUploadScanResult(scanStatus, "private-scanner-message")),
            MediaObjectStoreTestFactory.Create(),
            new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1)));
        using var recorder = new MediaImageUploadMetricsTestSupport();

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(expectedStatus);
        recorder.AssertMeasurements(expectedOutcome, expectedStages.Split(',', StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public async Task Accept_records_a_storage_failure_and_propagates_the_original_exception()
    {
        // Arrange
        var data = CatalogTestImages.CreateJpeg(320, 160);
        using var content = new MemoryStream(data);
        var request = MediaImageUploadMetricsTestSupport.CreateRequest(content, data.Length);
        var objectStore = MediaObjectStoreTestFactory.Create();
        var failure = new IOException("private-store-error");
        objectStore.FailNext(ObjectStoreOperation.Put, failure);
        var intake = MediaImageUploadIntakeTestFactory.Create(
            new StubMediaUploadScanner(MediaUploadScanResult.Passed),
            objectStore,
            new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1)));
        using var recorder = new MediaImageUploadMetricsTestSupport();

        // Act
        Func<Task> accept = async () => await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        var exception = await accept.ShouldThrow<IOException>();
        exception.ShouldBeSameAs(failure);
        recorder.AssertMeasurements("storage_failed", ["scan", "store"]);
        objectStore.ObjectKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task Accept_records_gallery_conflict_after_compensating_the_stored_object()
    {
        // Arrange
        var data = CatalogTestImages.CreateJpeg(320, 160);
        using var content = new MemoryStream(data);
        var request = MediaImageUploadMetricsTestSupport.CreateRequest(content, data.Length);
        var objectStore = MediaObjectStoreTestFactory.Create();
        var current = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1);
        var intake = MediaImageUploadIntakeTestFactory.Create(
            new StubMediaUploadScanner(MediaUploadScanResult.Passed),
            objectStore,
            new ThrowingPublicMediaImageStore(current, new MediaGalleryPlacementConflictException()));
        using var recorder = new MediaImageUploadMetricsTestSupport();

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Conflict);
        objectStore.ObjectKeys.ShouldBeEmpty();
        recorder.AssertMeasurements("gallery_conflict", ["scan", "store"]);
    }
}
