using PandaBear.ObjectStorage;
using ViajantesTurismo.Catalog.Application.Media;
using PandaBear.Testing.Logging;

namespace ViajantesTurismo.Catalog.UnitTests;

[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.CatalogArea)]
[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.MediaCapability)]
public sealed class MediaObjectReconciliationServiceTests
{
    [Fact]
    public async Task Reconcile_reports_metadata_keys_missing_from_storage()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1024);
        var objectStore = new InMemoryMediaObjectStore();
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var service = new MediaObjectReconciliationService(objectStore, imageStore);

        // Act
        var report = await service.Reconcile(deleteOrphans: false, TestContext.Current.CancellationToken);

        // Assert
        report.MissingObjectKeys.ShouldContain(image.SourceObjectKey);
        report.OrphanObjectKeys.ShouldBeEmpty();
        report.DeletedOrphanObjectKeys.ShouldBeEmpty();
        report.FailedDeleteObjectKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task Reconcile_reports_orphans_without_deleting_by_default()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1024);
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new ObjectWriteRequest(image.SourceObjectKey, new MemoryStream([1]), "image/png", 1),
            TestContext.Current.CancellationToken);
        await objectStore.Put(
            new ObjectWriteRequest("media/orphan.jpg", new MemoryStream([2]), "image/jpeg", 1),
            TestContext.Current.CancellationToken);
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var service = new MediaObjectReconciliationService(objectStore, imageStore);

        // Act
        var report = await service.Reconcile(deleteOrphans: false, TestContext.Current.CancellationToken);

        // Assert
        report.MissingObjectKeys.ShouldBeEmpty();
        report.OrphanObjectKeys.ShouldContain("media/orphan.jpg");
        report.DeletedOrphanObjectKeys.ShouldBeEmpty();
        report.FailedDeleteObjectKeys.ShouldBeEmpty();
        objectStore.ObjectKeys.ShouldContain("media/orphan.jpg");
    }

    [Fact]
    public async Task Reconcile_deletes_orphans_only_when_explicitly_requested()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1024);
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new ObjectWriteRequest(image.SourceObjectKey, new MemoryStream([1]), "image/png", 1),
            TestContext.Current.CancellationToken);
        await objectStore.Put(
            new ObjectWriteRequest("media/orphan.jpg", new MemoryStream([2]), "image/jpeg", 1),
            TestContext.Current.CancellationToken);
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var service = new MediaObjectReconciliationService(objectStore, imageStore);

        // Act
        var report = await service.Reconcile(deleteOrphans: true, TestContext.Current.CancellationToken);

        // Assert
        report.MissingObjectKeys.ShouldBeEmpty();
        report.OrphanObjectKeys.ShouldContain("media/orphan.jpg");
        report.DeletedOrphanObjectKeys.ShouldContain("media/orphan.jpg");
        report.FailedDeleteObjectKeys.ShouldBeEmpty();
        objectStore.ObjectKeys.ShouldNotContain("media/orphan.jpg");
        objectStore.ExistsCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Reconcile_preserves_recent_orphans_during_grace_period()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1024);
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new ObjectWriteRequest("media/recent-orphan.jpg", new MemoryStream([2]), "image/jpeg", 1),
            TestContext.Current.CancellationToken);
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var service = new MediaObjectReconciliationService(objectStore, imageStore);

        // Act
        var report = await service.Reconcile(deleteOrphans: true, TimeSpan.FromDays(1), TestContext.Current.CancellationToken);

        // Assert
        report.OrphanObjectKeys.ShouldContain("media/recent-orphan.jpg");
        report.DeletedOrphanObjectKeys.ShouldBeEmpty();
        report.FailedDeleteObjectKeys.ShouldBeEmpty();
        objectStore.ObjectKeys.ShouldContain("media/recent-orphan.jpg");
    }

    [Fact]
    public async Task Reconcile_deletes_orphans_after_grace_period()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1024);
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new ObjectWriteRequest("media/old-orphan.jpg", new MemoryStream([2]), "image/jpeg", 1),
            TestContext.Current.CancellationToken);
        objectStore.SetLastModified("media/old-orphan.jpg", TimeProvider.System.GetUtcNow().AddDays(-2));
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var service = new MediaObjectReconciliationService(objectStore, imageStore);

        // Act
        var report = await service.Reconcile(deleteOrphans: true, TimeSpan.FromDays(1), TestContext.Current.CancellationToken);

        // Assert
        report.OrphanObjectKeys.ShouldContain("media/old-orphan.jpg");
        report.DeletedOrphanObjectKeys.ShouldContain("media/old-orphan.jpg");
        report.FailedDeleteObjectKeys.ShouldBeEmpty();
        objectStore.ObjectKeys.ShouldNotContain("media/old-orphan.jpg");
    }

    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, PandaBear.Testing.TestTraitValues.SecurityCategory)]
    public async Task Reconcile_records_delete_failures_and_can_retry_later()
    {
        // Arrange
        const string objectKey = "media/traveler@example.com/private.jpg";
        var image = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1024);
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new ObjectWriteRequest(objectKey, new MemoryStream([2]), "image/jpeg", 1),
            TestContext.Current.CancellationToken);
        objectStore.FailNextDelete(objectKey);
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var logger = new RecordingLogger<MediaObjectReconciliationService>();
        var service = new MediaObjectReconciliationService(objectStore, imageStore, logger: logger);

        // Act
        var failedReport = await service.Reconcile(deleteOrphans: true, TestContext.Current.CancellationToken);
        var retryReport = await service.Reconcile(deleteOrphans: true, TestContext.Current.CancellationToken);

        // Assert
        failedReport.FailedDeleteObjectKeys.ShouldContain(objectKey);
        failedReport.DeletedOrphanObjectKeys.ShouldBeEmpty();
        retryReport.DeletedOrphanObjectKeys.ShouldContain(objectKey);
        retryReport.FailedDeleteObjectKeys.ShouldBeEmpty();
        objectStore.ObjectKeys.ShouldNotContain(objectKey);
        var logMessage = logger.Records.Select(record => record.Message).ShouldHaveSingleItem();
        logMessage.ShouldContain(nameof(IOException), StringComparison.Ordinal);
        logMessage.ShouldNotContain(objectKey, StringComparison.Ordinal);
        logger.Records.SelectMany(record => record.State).ShouldNotContain(value => string.Equals(value.Value as string, objectKey, StringComparison.Ordinal));
        logger.Records.Where(record => record.Exception is not null).ShouldBeEmpty();
    }

    [Fact]
    public async Task Reconcile_rejects_negative_orphan_grace_period()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1024);
        var service = new MediaObjectReconciliationService(new InMemoryMediaObjectStore(), new InMemoryPublicMediaImageStore(image));

        // Act
        var action = () => service.Reconcile(deleteOrphans: true, TimeSpan.FromTicks(-1), TestContext.Current.CancellationToken).AsTask();

        // Assert
        var exception = await action.ShouldThrow<ArgumentOutOfRangeException>();
        exception.ParamName.ShouldBe("orphanGracePeriod");
    }
}
