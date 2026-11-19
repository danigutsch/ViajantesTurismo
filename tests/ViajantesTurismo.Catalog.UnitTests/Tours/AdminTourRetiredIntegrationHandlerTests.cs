using PandaBear.EventSourcing;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;
using ViajantesTurismo.Catalog.Testing.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests.Tours;

public sealed class AdminTourRetiredIntegrationHandlerTests
{
    [Fact]
    public async Task Publication_prepared_before_the_retirement_cannot_append_after_it()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var eventStore = new TestEventStore();
        var retire = new AdminTourRetiredIntegrationHandler(eventStore, new TestCatalogTourReadModelStore());
        var now = TimeProvider.System.GetUtcNow();
        var adminTourId = Guid.CreateVersion7();
        var streamId = CatalogTourStreamIds.FromAdminTourId(adminTourId);
        var authored = CatalogTour.CreateDraft(adminTourId, "andes", "Andes", Guid.CreateVersion7());
        authored.ChangePresentation("Andes", "andes", "Ride summary", "", "", "", "").IsSuccess.ShouldBeTrue();
        await eventStore.Append(streamId, ExpectedStreamRevision.NoStream, authored.GetUncommittedEvents(), ct);
        authored.ClearUncommittedEvents();
        var publishedFrom = ExpectedStreamRevision.From(StreamRevision.From(authored.Version));
        authored.Publish().IsSuccess.ShouldBeTrue();
        await retire.Handle(new AdminTourRetiredIntegrationEvent(Guid.CreateVersion7(), now, adminTourId, Guid.CreateVersion7()), ct);

        // Act
        Func<Task> stalePublish = async () =>
            _ = await eventStore.Append(streamId, publishedFrom, authored.GetUncommittedEvents(), ct);

        // Assert
        await stalePublish.ShouldThrow<ExpectedStreamRevisionConflictException>();
        var stream = await eventStore.Load(streamId, afterRevision: null, ct);
        stream.Select(envelope => envelope.Data).OfType<CatalogTourPublished>().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Retirement_closes_the_stream_in_either_delivery_order(bool createFirst)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var eventStore = new TestEventStore();
        var retirements = new TestCatalogTourReadModelStore();
        var create = new AdminTourCreatedIntegrationHandler(eventStore, new TestCatalogTourSlugLock());
        var retire = new AdminTourRetiredIntegrationHandler(eventStore, retirements);
        var now = TimeProvider.System.GetUtcNow();
        var adminTourId = Guid.CreateVersion7();
        var created = new AdminTourCreatedIntegrationEvent(Guid.CreateVersion7(), now, adminTourId, "andes", "Andes");
        var retired = new AdminTourRetiredIntegrationEvent(Guid.CreateVersion7(), now, adminTourId, Guid.CreateVersion7());

        // Act
        if (createFirst)
        {
            await create.Handle(created, ct);
        }

        await retire.Handle(retired, ct);
        await create.Handle(created, ct);
        await retire.Handle(retired, ct);
        var stream = await eventStore.Load(CatalogTourStreamIds.FromAdminTourId(adminTourId), afterRevision: null, ct);
        var rehydrated = CatalogTour.Rehydrate(stream.OrderBy(envelope => envelope.Revision.Value).Select(envelope => envelope.Data));

        // Assert
        stream.Select(envelope => envelope.Data).OfType<CatalogTourDraftCreated>().Count().ShouldBe(createFirst ? 1 : 0);
        var retirement = stream.Select(envelope => envelope.Data).OfType<CatalogTourRetired>().ShouldHaveSingleItem();
        retirement.AdminTourId.ShouldBe(adminTourId);
        retirement.RetirementId.ShouldBe(retired.RetirementId);
        rehydrated.IsPublished.ShouldBeFalse();
        rehydrated.Publish().IsFailure.ShouldBeTrue();
        rehydrated.ChangePresentation("Other", "other", "Summary", "", "", "", "").IsFailure.ShouldBeTrue();
        rehydrated.CanEditGallery(canPublish: true, isPubliclyVisible: false, rehydrated.Version).ShouldBeFalse();
        retirements.RetirementCount.ShouldBe(1);
    }
}
