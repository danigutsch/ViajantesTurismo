using ViajantesTurismo.Admin.Application.Branding;
using ViajantesTurismo.Admin.Testing.Builders;

namespace ViajantesTurismo.Admin.UnitTests.Application.Branding;

[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CategoryName, Infrastructure.TestTraits.CoreBehaviorCategory)]
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
    public async Task An_unexpected_epoch_enters_recovery_and_leaves_the_event_pending()
    {
        // Arrange
        var store = new FakeBrandingProjectionStore();
        var applier = new BrandingProjectionApplier(store, TimeProvider.System);
        await applier.Apply(BrandingProjectionTestData.CreateEvent(revision: 3, brandName: "Current"), TestContext.Current.CancellationToken);

        // Act
        var apply = () => applier.Apply(
            BrandingProjectionTestData.CreateEvent(revision: 4, brandName: "Other lineage", epoch: Guid.CreateVersion7()),
            TestContext.Current.CancellationToken);

        // Assert
        await apply.ShouldThrow<BrandingProjectionRecoveringException>();
        store.Current.ShouldNotBeNull().BrandName.ShouldBe("Current");
        store.Inbox.ShouldHaveSingleItem();
        var recovery = store.Recovery.ShouldNotBeNull();
        recovery.Status.ShouldBe(BrandingProjectionStatus.Recovering);
        recovery.Reason.ShouldBe("epoch-mismatch");
    }

    [Fact]
    public async Task A_conflicting_payload_for_an_applied_revision_enters_recovery()
    {
        // Arrange
        var store = new FakeBrandingProjectionStore();
        var applier = new BrandingProjectionApplier(store, TimeProvider.System);
        await applier.Apply(BrandingProjectionTestData.CreateEvent(revision: 3, brandName: "Current"), TestContext.Current.CancellationToken);

        // Act
        var apply = () => applier.Apply(BrandingProjectionTestData.CreateEvent(revision: 3, brandName: "Different"), TestContext.Current.CancellationToken);

        // Assert
        await apply.ShouldThrow<BrandingProjectionRecoveringException>();
        store.Current.ShouldNotBeNull().BrandName.ShouldBe("Current");
        store.Recovery.ShouldNotBeNull().Reason.ShouldBe("revision-conflict");
    }

    [Fact]
    public async Task Ordinary_delivery_stays_fenced_while_recovering()
    {
        // Arrange
        var store = new FakeBrandingProjectionStore();
        var applier = new BrandingProjectionApplier(store, TimeProvider.System);
        await applier.Apply(BrandingProjectionTestData.CreateEvent(revision: 3, brandName: "Current"), TestContext.Current.CancellationToken);
        store.Recovery = BrandingProjectionRecovery.Start("operator", DateTimeOffset.UnixEpoch);

        // Act
        var apply = () => applier.Apply(BrandingProjectionTestData.CreateEvent(revision: 4, brandName: "Newer"), TestContext.Current.CancellationToken);

        // Assert
        await apply.ShouldThrow<BrandingProjectionRecoveringException>();
        store.Current.ShouldNotBeNull().BrandName.ShouldBe("Current");
        store.Inbox.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task The_authorized_epoch_transition_reseeds_audits_and_returns_to_ready()
    {
        // Arrange
        var store = new FakeBrandingProjectionStore();
        var applier = new BrandingProjectionApplier(store, TimeProvider.System);
        await applier.Apply(BrandingProjectionTestData.CreateEvent(revision: 7, brandName: "Before restore"), TestContext.Current.CancellationToken);
        var newEpoch = Guid.CreateVersion7();
        var reseed = BrandingProjectionTestData.CreateEvent(revision: 1, brandName: "Restored", epoch: newEpoch);
        store.Recovery = BrandingProjectionRecovery.Start("source-restore", DateTimeOffset.UnixEpoch)
            .Authorize(BrandingProjectionTestData.CreateAuthorization(reseed));

        // Act
        await applier.Apply(reseed, TestContext.Current.CancellationToken);

        // Assert
        var current = store.Current.ShouldNotBeNull();
        current.SourceEpoch.ShouldBe(newEpoch);
        current.SourceRevision.ShouldBe(1);
        current.BrandName.ShouldBe("Restored");
        store.Recovery.ShouldNotBeNull().Status.ShouldBe(BrandingProjectionStatus.Ready);
        store.Recovery.Authorization.ShouldBeNull();
        var inbox = store.Inbox[^1];
        inbox.EventId.ShouldBe(reseed.EventId);
        inbox.Outcome.ShouldBe(BrandingInboxOutcome.Applied);
        inbox.ReasonCode.ShouldBe("reseed");
        var audit = store.Audit.ShouldHaveSingleItem();
        audit.Kind.ShouldBe(BrandingRecoveryAuditKind.EpochTransition);
        audit.OldEpoch.ShouldBe(BrandingProjectionTestData.Epoch);
        audit.OldRevision.ShouldBe(7);
        audit.NewEpoch.ShouldBe(newEpoch);
        audit.NewRevision.ShouldBe(1);
        audit.Actor.ShouldBe("operator@example.test");
    }

    [Fact]
    public async Task The_authorized_same_epoch_reseed_rebuilds_a_corrupt_projection_at_the_equal_revision()
    {
        // Arrange
        var store = new FakeBrandingProjectionStore();
        var applier = new BrandingProjectionApplier(store, TimeProvider.System);
        await applier.Apply(BrandingProjectionTestData.CreateEvent(revision: 5, brandName: "Corrupt"), TestContext.Current.CancellationToken);
        var reseed = BrandingProjectionTestData.CreateEvent(revision: 5, brandName: "Source of truth");
        store.Recovery = BrandingProjectionRecovery.Start("projection-corrupt", DateTimeOffset.UnixEpoch)
            .Authorize(BrandingProjectionTestData.CreateAuthorization(reseed));

        // Act
        await applier.Apply(reseed, TestContext.Current.CancellationToken);

        // Assert
        store.Current.ShouldNotBeNull().BrandName.ShouldBe("Source of truth");
        store.Audit.ShouldHaveSingleItem().Kind.ShouldBe(BrandingRecoveryAuditKind.SameEpochReseed);
        store.Recovery.ShouldNotBeNull().Status.ShouldBe(BrandingProjectionStatus.Ready);
    }

    [Fact]
    public async Task An_unauthorized_event_cannot_use_the_reseed_path()
    {
        // Arrange
        var store = new FakeBrandingProjectionStore();
        var applier = new BrandingProjectionApplier(store, TimeProvider.System);
        await applier.Apply(BrandingProjectionTestData.CreateEvent(revision: 5), TestContext.Current.CancellationToken);
        var authorized = BrandingProjectionTestData.CreateEvent(revision: 5, brandName: "Authorized");
        store.Recovery = BrandingProjectionRecovery.Start("projection-corrupt", DateTimeOffset.UnixEpoch)
            .Authorize(BrandingProjectionTestData.CreateAuthorization(authorized));

        // Act
        var apply = () => applier.Apply(
            BrandingProjectionTestData.CreateEvent(revision: 5, brandName: "Impostor"),
            TestContext.Current.CancellationToken);

        // Assert
        await apply.ShouldThrow<BrandingProjectionRecoveringException>();
        store.Current.ShouldNotBeNull().BrandName.ShouldBe("Viajantes");
        store.Recovery.ShouldNotBeNull().Status.ShouldBe(BrandingProjectionStatus.Recovering);
    }

    [Fact]
    public async Task Pending_events_from_a_retired_epoch_are_quarantined_without_reentering_recovery()
    {
        // Arrange
        var store = new FakeBrandingProjectionStore();
        var applier = new BrandingProjectionApplier(store, TimeProvider.System);
        await applier.Apply(BrandingProjectionTestData.CreateEvent(revision: 7), TestContext.Current.CancellationToken);
        var reseed = BrandingProjectionTestData.CreateEvent(revision: 1, epoch: Guid.CreateVersion7());
        store.Recovery = BrandingProjectionRecovery.Start("source-restore", DateTimeOffset.UnixEpoch)
            .Authorize(BrandingProjectionTestData.CreateAuthorization(reseed));
        await applier.Apply(reseed, TestContext.Current.CancellationToken);

        // Act
        await applier.Apply(BrandingProjectionTestData.CreateEvent(revision: 8, brandName: "Old lineage"), TestContext.Current.CancellationToken);

        // Assert
        store.Current.ShouldNotBeNull().SourceEpoch.ShouldBe(reseed.SourceEpoch);
        var quarantined = store.Inbox[^1];
        quarantined.Outcome.ShouldBe(BrandingInboxOutcome.Rejected);
        quarantined.ReasonCode.ShouldBe("superseded-epoch");
        store.Recovery.ShouldNotBeNull().Status.ShouldBe(BrandingProjectionStatus.Ready);
    }

    [Fact]
    public async Task Replaying_the_committed_reseed_is_idempotent()
    {
        // Arrange
        var store = new FakeBrandingProjectionStore();
        var applier = new BrandingProjectionApplier(store, TimeProvider.System);
        await applier.Apply(BrandingProjectionTestData.CreateEvent(revision: 7), TestContext.Current.CancellationToken);
        var reseed = BrandingProjectionTestData.CreateEvent(revision: 1, epoch: Guid.CreateVersion7());
        store.Recovery = BrandingProjectionRecovery.Start("source-restore", DateTimeOffset.UnixEpoch)
            .Authorize(BrandingProjectionTestData.CreateAuthorization(reseed));
        await applier.Apply(reseed, TestContext.Current.CancellationToken);
        var saves = store.SaveCount;

        // Act
        await applier.Apply(reseed, TestContext.Current.CancellationToken);

        // Assert
        store.SaveCount.ShouldBe(saves);
        store.Audit.ShouldHaveSingleItem();
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

    [Fact]
    public async Task A_valid_logo_is_stored_with_the_projection()
    {
        // Arrange
        var store = new FakeBrandingProjectionStore();
        var applier = new BrandingProjectionApplier(store, TimeProvider.System);
        var incoming = BrandingProjectionTestData.CreateEventWithLogo(1, DocumentLogoTestData.Content);

        // Act
        await applier.Apply(incoming, TestContext.Current.CancellationToken);

        // Assert
        var logo = store.Current.ShouldNotBeNull().Logo.ShouldNotBeNull();
        logo.Sha256.ShouldBe(DocumentLogoTestData.Sha256);
        logo.ContentType.ShouldBe("image/webp");
        logo.Content.ShouldBe(DocumentLogoTestData.Content);
    }

    [Fact]
    public async Task An_invalid_logo_is_rejected_without_replacing_the_projection()
    {
        // Arrange
        var store = new FakeBrandingProjectionStore();
        var applier = new BrandingProjectionApplier(store, TimeProvider.System);
        var incoming = BrandingProjectionTestData.CreateEvent(
            1,
            logoSha256: new string('a', 64),
            logoContentType: "image/webp",
            logoContent: DocumentLogoTestData.Content);

        // Act
        await applier.Apply(incoming, TestContext.Current.CancellationToken);

        // Assert
        store.Current.ShouldBeNull();
        store.Inbox.ShouldHaveSingleItem().ReasonCode.ShouldBe("invalid-logo");
    }

    [Fact]
    public async Task An_authorized_reseed_keeps_the_logo_bytes()
    {
        // Arrange
        var store = new FakeBrandingProjectionStore();
        var applier = new BrandingProjectionApplier(store, TimeProvider.System);
        await applier.Apply(BrandingProjectionTestData.CreateEvent(revision: 7), TestContext.Current.CancellationToken);
        var newEpoch = Guid.CreateVersion7();
        var reseed = BrandingProjectionTestData.CreateEventWithLogo(1, DocumentLogoTestData.Content, newEpoch);
        store.Recovery = BrandingProjectionRecovery.Start("source-restore", DateTimeOffset.UnixEpoch)
            .Authorize(BrandingProjectionTestData.CreateAuthorization(reseed));

        // Act
        await applier.Apply(reseed, TestContext.Current.CancellationToken);

        // Assert
        var logo = store.Current.ShouldNotBeNull().Logo.ShouldNotBeNull();
        logo.Content.ShouldBe(DocumentLogoTestData.Content);
        store.Recovery.ShouldNotBeNull().Status.ShouldBe(BrandingProjectionStatus.Ready);
    }

    [Fact]
    public async Task An_invalid_logo_on_the_authorized_reseed_is_rejected_and_the_projection_stays_fenced()
    {
        // Arrange
        var store = new FakeBrandingProjectionStore();
        var applier = new BrandingProjectionApplier(store, TimeProvider.System);
        var reseed = BrandingProjectionTestData.CreateEvent(1, logoSha256: new string('a', 64));
        store.Recovery = BrandingProjectionRecovery.Start("source-restore", DateTimeOffset.UnixEpoch)
            .Authorize(BrandingProjectionTestData.CreateAuthorization(reseed));

        // Act
        await applier.Apply(reseed, TestContext.Current.CancellationToken);

        // Assert
        store.Current.ShouldBeNull();
        store.Inbox.ShouldHaveSingleItem().ReasonCode.ShouldBe("invalid-logo");
        store.Recovery.ShouldNotBeNull().Status.ShouldNotBe(BrandingProjectionStatus.Ready);
    }
}
