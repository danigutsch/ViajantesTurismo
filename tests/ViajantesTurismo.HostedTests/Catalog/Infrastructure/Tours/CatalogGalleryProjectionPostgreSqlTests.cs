using Microsoft.EntityFrameworkCore;
using Npgsql;
using PandaBear.EventSourcing;
using PandaBear.EventSourcing.Npgsql;
using PandaBear.Testing;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Domain.Tours;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure.Media;
using ViajantesTurismo.Catalog.Infrastructure.Tours;
using ViajantesTurismo.Catalog.Testing.Infrastructure;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Verifies durable gallery writes and their replay on the Catalog store.</summary>
[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class CatalogGalleryProjectionPostgreSqlTests(HostedTestFixture shared)
{
    [Fact]
    public async Task A_committed_gallery_write_is_recovered_by_replay_before_public_placement_changes()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var context = new CatalogDbContextDesignTimeFactory().CreateDbContext([]);
        context.Database.SetConnectionString(database.ConnectionString);
        await context.Database.MigrateAsync(ct);
        await using var dataSource = NpgsqlDataSource.Create(database.ConnectionString);
        await using var events = new PostgreSqlEventStore(dataSource, new CatalogEventSerializer());
        await using var checkpoints = new PostgreSqlProjectionCheckpointStore(dataSource);
        await events.Initialize(ct);

        var tour = CatalogTour.CreateDraft(Guid.CreateVersion7(), "andes", "Andes", Guid.CreateVersion7());
        tour.ChangePresentation(tour.Title, tour.Slug, "Ride the Andes.", "", "", "", "").IsSuccess.ShouldBeTrue();
        tour.Publish().IsSuccess.ShouldBeTrue();
        var stream = CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId);
        await events.Append(stream, ExpectedStreamRevision.NoStream, tour.GetUncommittedEvents(), ct);
        var store = new EfCatalogTourReadModelStore(context);
        var projection = new CatalogTourReadModelProjection(store);
        var initial = new CatalogProjectionRunner(events, checkpoints, [projection]);
        var initializedCount = await initial.Project(ct);
        initializedCount.ShouldBe(3);
        var legacy = await store.GetTour(tour.Id, ct);
        var images = new EfPublicMediaImageStore(context);
        var first = PublicMediaImageTestFactory.CreateReadyImage(tour.Id, "first.jpg", "first-640.jpg", "sha256:first", "First", 0, true);
        var second = PublicMediaImageTestFactory.CreateReadyImage(tour.Id, "second.jpg", "second-640.jpg", "sha256:second", "Second", 1, false);
        await images.Upsert(first, ct);
        await images.Upsert(second, ct);
        var mutationLock = new PostgreSqlCatalogTourMutationLock(dataSource);
        var writer = new CatalogTourPresentationService(
            events, store, new PostgreSqlCatalogTourSlugLock(dataSource), mutationLock, projection, images,
            new EfCatalogGalleryPlacementIntentStore(context, new TestIntegrationEventOutbox(), TimeProvider.System));
        await context.Database.ExecuteSqlRawAsync(
            """ALTER TABLE "CatalogTourReadModels" ADD CONSTRAINT "TestRejectGallery" CHECK ("GalleryPhotoIds" IS NULL)""", ct);

        // Act
        var denied = await writer.ChangeGallery(tour.Id, [second.Id], second.Id, 3, canPublish: false, ct);
        var outsideInventory = await writer.ChangeGallery(tour.Id, [Guid.CreateVersion7()], null, 3, canPublish: true, ct);
        Func<Task> edit = async () => await writer.ChangeGallery(tour.Id, [second.Id], second.Id, 3, canPublish: true, ct);
        await edit.ShouldThrow<CatalogTourProjectionPendingException>();
        Func<Task> sameEditBeforeReplay = async () => await writer.ChangeGallery(tour.Id, [second.Id], second.Id, 4, canPublish: true, ct);
        await sameEditBeforeReplay.ShouldThrow<CatalogTourProjectionPendingException>();
        var committed = await events.Load(stream, afterRevision: StreamRevision.From(3), ct);
        var afterFailure = await store.GetTour(tour.Id, ct);
        var beforeReplay = await new EfCatalogGalleryReadStore(context)
            .GetAlbum(tour.Slug, PublicContentCultures.EnglishUnitedStates, null, ct);

        await using var recoveredContext = new CatalogDbContextDesignTimeFactory().CreateDbContext([]);
        recoveredContext.Database.SetConnectionString(database.ConnectionString);
        await recoveredContext.Database.ExecuteSqlRawAsync("""ALTER TABLE "CatalogTourReadModels" DROP CONSTRAINT "TestRejectGallery" """, ct);
        var recoveredStore = new EfCatalogTourReadModelStore(recoveredContext);
        var recoveredProjection = new CatalogTourReadModelProjection(recoveredStore);
        var recovery = new CatalogProjectionRunner(events, checkpoints, [recoveredProjection]);
        var recoveredCount = await recovery.Project(ct);
        var repeatedCount = await recovery.Project(ct);
        var recovered = await recoveredStore.GetTour(tour.Id, ct);
        var sameEditAfterReplay = await writer.ChangeGallery(tour.Id, [second.Id], second.Id, 4, canPublish: true, ct);
        Func<Task> oldRevision = async () => await writer.ChangeGallery(tour.Id, [first.Id], first.Id, 3, canPublish: true, ct);
        await oldRevision.ShouldThrow<ExpectedStreamRevisionConflictException>();
        var afterRetry = await events.Load(stream, afterRevision: StreamRevision.From(3), ct);
        var appliedAlbum = await new EfCatalogGalleryReadStore(recoveredContext)
            .GetAlbum(tour.Slug, PublicContentCultures.EnglishUnitedStates, null, ct);

        // Assert
        legacy.ShouldNotBeNull().GalleryPhotoIds.ShouldBeNull();
        denied.IsFailure.ShouldBeTrue();
        outsideInventory.IsFailure.ShouldBeTrue();
        var galleryEvent = committed.ShouldHaveSingleItem();
        var placement = galleryEvent.Data.ShouldBeOfType<CatalogTourGalleryChanged>();
        placement.PhotoIds.ShouldBe([second.Id]);
        afterFailure.ShouldNotBeNull().StreamVersion.ShouldBe(3);
        afterFailure.GalleryPhotoIds.ShouldBeNull();
        recoveredCount.ShouldBe(1);
        repeatedCount.ShouldBe(0);
        sameEditAfterReplay.IsSuccess.ShouldBeTrue();
        sameEditAfterReplay.Value.StreamVersion.ShouldBe(4);
        afterRetry.ShouldHaveSingleItem().EventId.ShouldBe(galleryEvent.EventId);
        var row = recovered.ShouldNotBeNull();
        row.StreamVersion.ShouldBe(4);
        row.GalleryPosition.ShouldBe(galleryEvent.Position);
        row.GalleryPhotoIds.ShouldNotBeNull().ShouldBe([second.Id]);
        row.GalleryCoverId.ShouldBe(second.Id);
        row.IsPubliclyVisible.ShouldBeTrue();
        beforeReplay.ShouldNotBeNull().Photos.Select(photo => photo.Id).ShouldBe([first.Id, second.Id]);
        var appliedPhoto = appliedAlbum.ShouldNotBeNull().Photos.ShouldHaveSingleItem();
        appliedPhoto.Id.ShouldBe(second.Id);
        appliedPhoto.IsCover.ShouldBeTrue();
    }
}
