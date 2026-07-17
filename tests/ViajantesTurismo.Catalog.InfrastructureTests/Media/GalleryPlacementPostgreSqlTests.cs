using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Catalog.Testing.Infrastructure;

namespace ViajantesTurismo.Catalog.InfrastructureTests.Media;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
public sealed class GalleryPlacementPostgreSqlTests : IAsyncLifetime
{
    private GalleryPlacementPostgreSqlScenario? scenario;

    private GalleryPlacementPostgreSqlScenario Scenario =>
        scenario ?? throw new InvalidOperationException("Test scenario is not initialized.");

    public async ValueTask InitializeAsync()
    {
        scenario = await GalleryPlacementPostgreSqlScenario.Create(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        var currentScenario = scenario;
        scenario = null;

        if (currentScenario is not null)
        {
            await currentScenario.DisposeAsync();
        }
    }

    [Fact]
    public async Task Store_translates_gallery_placement_unique_constraints()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tourId = Guid.CreateVersion7();
        await Scenario.MigrateToLatest(ct);
        var first = PublicMediaImageTestFactory.CreateReadyImage(tourId, "first.jpg", "first-640.jpg", "sha256:first", "First", 0, true);
        await using var firstContext = Scenario.CreateDbContext();
        await new EfPublicMediaImageStore(firstContext).Upsert(first, ct);
        var indexNames = await Scenario.GetGalleryIndexNames(ct);

        // Assert
        indexNames.Count.ShouldBe(2);
        indexNames.ShouldContain(PublicMediaImageSchema.GalleryCoverUniqueIndex);
        indexNames.ShouldContain(PublicMediaImageSchema.GalleryDisplayOrderUniqueIndex);

        // Act and assert display-order conflict.
        var displayOrderConflict = PublicMediaImageTestFactory.CreateReadyImage(tourId, "conflict-order.jpg", "conflict-order-640.jpg", "sha256:order", "Order", 0, false);
        await using var displayOrderContext = Scenario.CreateDbContext();
        Func<Task> saveDisplayOrderConflict = () => new EfPublicMediaImageStore(displayOrderContext).Upsert(displayOrderConflict, ct).AsTask();
        await saveDisplayOrderConflict.ShouldThrow<MediaGalleryPlacementConflictException>();

        // Act and assert cover conflict.
        var coverConflict = PublicMediaImageTestFactory.CreateReadyImage(tourId, "conflict-cover.jpg", "conflict-cover-640.jpg", "sha256:cover", "Cover", 2, true);
        await using var coverContext = Scenario.CreateDbContext();
        Func<Task> saveCoverConflict = () => new EfPublicMediaImageStore(coverContext).Upsert(coverConflict, ct).AsTask();
        await saveCoverConflict.ShouldThrow<MediaGalleryPlacementConflictException>();
    }
}
