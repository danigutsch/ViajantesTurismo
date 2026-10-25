using PandaBear.EventSourcing;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.UnitTests;

[Trait(PandaBear.Testing.SharedKernelTestTraitNames.ScopeName, PandaBear.Testing.SharedKernelTestTraitNames.UnitScope)]
public sealed class CatalogTourReadModelProjectionTests
{
    [Fact]
    public void Name_identifies_the_catalog_tour_read_model_checkpoint()
    {
        // Arrange
        var projection = new CatalogTourReadModelProjection(new RecordingCatalogTourReadModelStore());

        // Assert
        projection.Name.ShouldBe("catalog.tours.read-model");
    }

    [Fact]
    public async Task Apply_rejects_a_missing_envelope()
    {
        // Arrange
        var projection = new CatalogTourReadModelProjection(new RecordingCatalogTourReadModelStore());
        Func<Task> apply = async () => await projection.Apply(null!, TestContext.Current.CancellationToken);

        // Act
        var exception = await apply.ShouldThrow<ArgumentNullException>();

        // Assert
        exception.ParamName.ShouldBe("envelope");
    }

    [Fact]
    public async Task Apply_updates_the_presentation_of_the_read_model_row()
    {
        // Arrange
        var store = new RecordingCatalogTourReadModelStore();
        var projection = new CatalogTourReadModelProjection(store);
        var catalogTourId = Guid.CreateVersion7();
        var recordedAt = TimeProvider.System.GetUtcNow();
        var presentationChanged = new CatalogTourPresentationChanged(
            catalogTourId,
            "Camino Norte",
            "camino-norte",
            "Ride the Camino Norte.",
            "A detailed customer-facing description.",
            "Day one: depart by bicycle.",
            "Camino Norte cycling tour",
            "Cycle the Camino Norte with Viajantes Turismo.");
        var envelope = new EventEnvelope(
            CatalogTourStreamIds.FromAdminTourId(Guid.CreateVersion7()),
            42,
            StreamRevision.From(3),
            Guid.CreateVersion7(),
            nameof(CatalogTourPresentationChanged),
            presentationChanged,
            recordedAt);

        // Act
        await projection.Apply(envelope, TestContext.Current.CancellationToken);

        // Assert
        var update = store.PresentationUpdates.ShouldHaveSingleItem();
        update.CatalogTourId.ShouldBe(catalogTourId);
        update.Update.ShouldBe(new CatalogTourPresentationUpdate(
            "Camino Norte",
            "camino-norte",
            "Ride the Camino Norte.",
            "A detailed customer-facing description.",
            "Day one: depart by bicycle.",
            "Camino Norte cycling tour",
            "Cycle the Camino Norte with Viajantes Turismo."));
        update.StreamVersion.ShouldBe(3);
        update.Position.ShouldBe(42);
        update.UpdatedAt.ShouldBe(recordedAt);
        store.PublicationChanges.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Apply_records_publication_changes_in_the_read_model_row(bool published)
    {
        // Arrange
        var store = new RecordingCatalogTourReadModelStore();
        var projection = new CatalogTourReadModelProjection(store);
        var catalogTourId = Guid.CreateVersion7();
        var recordedAt = TimeProvider.System.GetUtcNow();
        object domainEvent = published ? new CatalogTourPublished(catalogTourId) : new CatalogTourUnpublished(catalogTourId);
        var envelope = new EventEnvelope(
            CatalogTourStreamIds.FromAdminTourId(Guid.CreateVersion7()),
            7,
            StreamRevision.From(5),
            Guid.CreateVersion7(),
            domainEvent.GetType().Name,
            domainEvent,
            recordedAt);

        // Act
        await projection.Apply(envelope, TestContext.Current.CancellationToken);

        // Assert
        var change = store.PublicationChanges.ShouldHaveSingleItem();
        change.CatalogTourId.ShouldBe(catalogTourId);
        change.IsPublished.ShouldBe(published);
        change.StreamVersion.ShouldBe(5);
        change.Position.ShouldBe(7);
        change.UpdatedAt.ShouldBe(recordedAt);
        store.PresentationUpdates.ShouldBeEmpty();
    }
}
