using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

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
    }

    [Fact]
    public async Task Reconcile_reports_orphans_without_deleting_by_default()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1024);
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new MediaObjectWriteRequest(image.SourceObjectKey, new MemoryStream([1]), "image/png", 1),
            TestContext.Current.CancellationToken);
        await objectStore.Put(
            new MediaObjectWriteRequest("media/orphan.jpg", new MemoryStream([2]), "image/jpeg", 1),
            TestContext.Current.CancellationToken);
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var service = new MediaObjectReconciliationService(objectStore, imageStore);

        // Act
        var report = await service.Reconcile(deleteOrphans: false, TestContext.Current.CancellationToken);

        // Assert
        report.MissingObjectKeys.ShouldBeEmpty();
        report.OrphanObjectKeys.ShouldContain("media/orphan.jpg");
        report.DeletedOrphanObjectKeys.ShouldBeEmpty();
        objectStore.ObjectKeys.ShouldContain("media/orphan.jpg");
    }

    [Fact]
    public async Task Reconcile_deletes_orphans_only_when_explicitly_requested()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1024);
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new MediaObjectWriteRequest(image.SourceObjectKey, new MemoryStream([1]), "image/png", 1),
            TestContext.Current.CancellationToken);
        await objectStore.Put(
            new MediaObjectWriteRequest("media/orphan.jpg", new MemoryStream([2]), "image/jpeg", 1),
            TestContext.Current.CancellationToken);
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var service = new MediaObjectReconciliationService(objectStore, imageStore);

        // Act
        var report = await service.Reconcile(deleteOrphans: true, TestContext.Current.CancellationToken);

        // Assert
        report.MissingObjectKeys.ShouldBeEmpty();
        report.OrphanObjectKeys.ShouldContain("media/orphan.jpg");
        report.DeletedOrphanObjectKeys.ShouldContain("media/orphan.jpg");
        objectStore.ObjectKeys.ShouldNotContain("media/orphan.jpg");
        objectStore.ExistsCallCount.ShouldBe(0);
    }
}
