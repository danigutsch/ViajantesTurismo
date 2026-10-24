using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.Admin.UnitTests.Application.Branding;

[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CategoryName, Infrastructure.TestTraits.CoreBehaviorCategory)]
[Trait(PandaBear.Testing.SharedKernelTestTraitNames.ScopeName, Infrastructure.TestTraits.UnitScope)]
public sealed class BrandingProjectionApplierTests
{
    [Fact]
    public async Task A_newer_revision_replaces_the_projection_and_records_the_event_in_one_save()
    {
        // Arrange
        var store = new FakeBrandingProjectionStore();
        var applier = new BrandingProjectionApplier(store, TimeProvider.System);
        var incoming = BrandingProjectionTestData.CreateEvent(revision: 1);

        // Act
        await applier.Apply(incoming, TestContext.Current.CancellationToken);

        // Assert
        var current = store.Current.ShouldNotBeNull();
        current.SourceRevision.ShouldBe(1);
        current.BrandName.ShouldBe("Viajantes");
        var entry = store.Inbox.ShouldHaveSingleItem();
        entry.EventId.ShouldBe(incoming.EventId);
        entry.Outcome.ShouldBe(BrandingInboxOutcome.Applied);
        store.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Redelivery_of_a_processed_event_changes_nothing()
    {
        // Arrange
        var store = new FakeBrandingProjectionStore();
        var applier = new BrandingProjectionApplier(store, TimeProvider.System);
        var incoming = BrandingProjectionTestData.CreateEvent(revision: 1);
        await applier.Apply(incoming, TestContext.Current.CancellationToken);

        // Act
        await applier.Apply(incoming, TestContext.Current.CancellationToken);

        // Assert
        store.Inbox.ShouldHaveSingleItem();
        store.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task An_older_revision_is_acknowledged_without_replacing_the_projection()
    {
        // Arrange
        var store = new FakeBrandingProjectionStore();
        var applier = new BrandingProjectionApplier(store, TimeProvider.System);
        await applier.Apply(BrandingProjectionTestData.CreateEvent(revision: 3, brandName: "Current"), TestContext.Current.CancellationToken);

        // Act
        await applier.Apply(BrandingProjectionTestData.CreateEvent(revision: 2, brandName: "Older"), TestContext.Current.CancellationToken);

        // Assert
        store.Current.ShouldNotBeNull().BrandName.ShouldBe("Current");
        store.Inbox[^1].Outcome.ShouldBe(BrandingInboxOutcome.Stale);
    }

    [Fact]
    public async Task An_unexpected_epoch_is_rejected_without_replacing_the_projection()
    {
        // Arrange
        var store = new FakeBrandingProjectionStore();
        var applier = new BrandingProjectionApplier(store, TimeProvider.System);
        await applier.Apply(BrandingProjectionTestData.CreateEvent(revision: 3, brandName: "Current"), TestContext.Current.CancellationToken);

        // Act
        await applier.Apply(
            BrandingProjectionTestData.CreateEvent(revision: 4, brandName: "Other lineage", epoch: Guid.CreateVersion7()),
            TestContext.Current.CancellationToken);

        // Assert
        store.Current.ShouldNotBeNull().BrandName.ShouldBe("Current");
        var rejected = store.Inbox[^1];
        rejected.Outcome.ShouldBe(BrandingInboxOutcome.Rejected);
        rejected.ReasonCode.ShouldBe("epoch-mismatch");
    }

    [Fact]
    public async Task A_conflicting_payload_for_an_applied_revision_is_rejected()
    {
        // Arrange
        var store = new FakeBrandingProjectionStore();
        var applier = new BrandingProjectionApplier(store, TimeProvider.System);
        await applier.Apply(BrandingProjectionTestData.CreateEvent(revision: 3, brandName: "Current"), TestContext.Current.CancellationToken);

        // Act
        await applier.Apply(BrandingProjectionTestData.CreateEvent(revision: 3, brandName: "Different"), TestContext.Current.CancellationToken);

        // Assert
        store.Current.ShouldNotBeNull().BrandName.ShouldBe("Current");
        store.Inbox[^1].ReasonCode.ShouldBe("revision-conflict");
    }

    [Fact]
    public async Task An_invalid_envelope_is_rejected_without_a_projection()
    {
        // Arrange
        var store = new FakeBrandingProjectionStore();
        var applier = new BrandingProjectionApplier(store, TimeProvider.System);

        // Act
        await applier.Apply(BrandingProjectionTestData.CreateEvent(revision: 0), TestContext.Current.CancellationToken);

        // Assert
        store.Current.ShouldBeNull();
        store.Inbox.ShouldHaveSingleItem().ReasonCode.ShouldBe("invalid-envelope");
    }
}
