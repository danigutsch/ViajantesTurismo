using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;
using ViajantesTurismo.Branding.Infrastructure;
using TestTraits = ViajantesTurismo.Branding.ApiServiceTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Branding.ApiServiceTests;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
public sealed class BrandingSettingsRevisionTests
{
    [Fact]
    public async Task First_save_creates_revision_one_with_history_and_one_event()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scenario = BrandingStoreScenario.Create();
        var settings = BrandingSettingsTestData.CreateSettings();

        // Act
        await scenario.CreateStore().SaveSettings(settings, ct);

        // Assert
        var snapshot = scenario.ReadSnapshot();
        var current = snapshot.Current.ShouldNotBeNull();
        current.SourceEpoch.ShouldNotBe(Guid.Empty);
        current.SourceRevision.ShouldBe(1);
        var revision = snapshot.Revisions.ShouldHaveSingleItem();
        revision.SourceEpoch.ShouldBe(current.SourceEpoch);
        revision.SourceRevision.ShouldBe(1);
        revision.BrandName.ShouldBe(settings.BrandName);
        var envelope = snapshot.Outbox.ShouldHaveSingleItem();
        envelope.EventType.ShouldBe(BrandingSettingsChangedIntegrationEvent.EventType);
        envelope.EventVersion.ShouldBe(BrandingSettingsChangedIntegrationEvent.EventVersion);
    }

    [Fact]
    public async Task Changed_save_appends_the_next_revision_in_the_same_epoch()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scenario = BrandingStoreScenario.Create();
        var first = BrandingSettingsTestData.CreateSettings();
        var second = BrandingSettingsTestData.CreateSettings("Viajantes 2");
        await scenario.CreateStore().SaveSettings(first, ct);

        // Act
        await scenario.CreateStore().SaveSettings(second, ct);

        // Assert
        var snapshot = scenario.ReadSnapshot();
        var current = snapshot.Current.ShouldNotBeNull();
        current.SourceRevision.ShouldBe(2);
        current.BrandName.ShouldBe("Viajantes 2");
        snapshot.Revisions.Select(revision => revision.SourceRevision).ShouldBe([1L, 2L]);
        snapshot.Revisions.ShouldAllSatisfy(revision => revision.SourceEpoch.ShouldBe(current.SourceEpoch));
        snapshot.Outbox.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Saving_the_same_settings_writes_no_revision_or_event()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scenario = BrandingStoreScenario.Create();
        var settings = BrandingSettingsTestData.CreateSettings();
        await scenario.CreateStore().SaveSettings(settings, ct);

        // Act
        await scenario.CreateStore().SaveSettings(settings, ct);

        // Assert
        var snapshot = scenario.ReadSnapshot();
        snapshot.Current.ShouldNotBeNull().SourceRevision.ShouldBe(1);
        snapshot.Revisions.Count.ShouldBe(1);
        snapshot.Outbox.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Conditional_save_writes_nothing_when_the_precondition_is_false()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scenario = BrandingStoreScenario.Create();
        await scenario.CreateStore().SaveSettings(BrandingSettingsTestData.CreateSettings(), ct);
        BrandingSettingsSnapshot? observed = null;

        // Act
        var saved = await scenario.CreateStore().SaveIfMatch(
            BrandingSettingsTestData.CreateSettings("Rejected"),
            current =>
            {
                observed = current;
                return false;
            },
            ct);

        // Assert
        saved.ShouldBeNull();
        observed.ShouldNotBeNull().SourceRevision.ShouldBe(1);
        var snapshot = scenario.ReadSnapshot();
        snapshot.Current.ShouldNotBeNull().BrandName.ShouldBe(BrandingSettingsTestData.CreateSettings().BrandName);
        snapshot.Revisions.Count.ShouldBe(1);
        snapshot.Outbox.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Conditional_save_returns_the_new_revision_when_the_precondition_holds()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scenario = BrandingStoreScenario.Create();
        await scenario.CreateStore().SaveSettings(BrandingSettingsTestData.CreateSettings(), ct);

        // Act
        var saved = await scenario.CreateStore().SaveIfMatch(
            BrandingSettingsTestData.CreateSettings("Accepted"),
            current => current is { SourceRevision: 1 },
            ct);

        // Assert
        var snapshot = saved.ShouldNotBeNull();
        snapshot.SourceRevision.ShouldBe(2);
        snapshot.Settings.BrandName.ShouldBe("Accepted");
        scenario.ReadSnapshot().Outbox.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Republishing_the_current_snapshot_keeps_the_revision_under_a_new_event_identity()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scenario = BrandingStoreScenario.Create();
        await scenario.CreateStore().SaveSettings(BrandingSettingsTestData.CreateSettings(), ct);
        var before = scenario.ReadSnapshot();

        // Act
        var publication = await scenario.CreateStore().RepublishCurrent("projection-lost", "INC-1", "operator@example.test", ct);

        // Assert
        var after = scenario.ReadSnapshot();
        var current = after.Current.ShouldNotBeNull();
        publication.SourceEpoch.ShouldBe(current.SourceEpoch);
        publication.SourceRevision.ShouldBe(1);
        after.Revisions.Count.ShouldBe(1);
        after.Outbox.Count.ShouldBe(2);
        after.Outbox.Select(envelope => envelope.EventId).ShouldContain(publication.EventId.ToString());
        before.Outbox.ShouldHaveSingleItem().EventId.ShouldNotBe(publication.EventId.ToString());
    }

    [Fact]
    public async Task Rotating_the_epoch_restarts_the_revision_and_publishes_the_restored_snapshot()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scenario = BrandingStoreScenario.Create();
        await scenario.CreateStore().SaveSettings(BrandingSettingsTestData.CreateSettings(), ct);
        await scenario.CreateStore().SaveSettings(BrandingSettingsTestData.CreateSettings("Second"), ct);
        var oldEpoch = scenario.ReadSnapshot().Current.ShouldNotBeNull().SourceEpoch;

        // Act
        var publication = await scenario.CreateStore().RotateEpoch("source-restore", "BACKUP-9", "operator@example.test", ct);

        // Assert
        var snapshot = scenario.ReadSnapshot();
        var current = snapshot.Current.ShouldNotBeNull();
        current.SourceEpoch.ShouldNotBe(oldEpoch);
        current.SourceRevision.ShouldBe(1);
        current.BrandName.ShouldBe("Second");
        publication.SourceEpoch.ShouldBe(current.SourceEpoch);
        publication.SourceRevision.ShouldBe(1);
        snapshot.Revisions.ShouldContain(revision => revision.SourceEpoch == current.SourceEpoch && revision.SourceRevision == 1);
        snapshot.Outbox.Count.ShouldBe(3);
    }

    [Fact]
    public async Task A_writer_with_a_stale_revision_conflicts()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scenario = BrandingStoreScenario.Create();
        var initial = BrandingSettingsTestData.CreateSettings();
        await scenario.CreateStore().SaveSettings(initial, ct);
        var writerA = scenario.CreateStore();
        var writerB = scenario.CreateStore();
        _ = await writerA.GetSettings(ct);
        _ = await writerB.GetSettings(ct);
        await writerA.SaveSettings(
            BrandingSettingsTestData.CreateSettings("From A"), ct);

        // Act
        var save = () => writerB.SaveSettings(
            BrandingSettingsTestData.CreateSettings("From B"), ct);

        // Assert
        // The in-memory provider has no transactions; BrandingRevisionPostgreSqlTests proves no partial write.
        await save.ShouldThrow<DbUpdateConcurrencyException>();
        scenario.ReadSnapshot().Current.ShouldNotBeNull().BrandName.ShouldBe("From A");
    }

    [Fact]
    public async Task Bootstrap_turns_an_existing_valid_row_into_revision_one_once()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scenario = BrandingStoreScenario.Create();
        var settings = BrandingSettingsTestData.CreateSettings();
        await scenario.SeedUnrevisionedSettings(settings, ct);

        // Act
        await scenario.CreateStore().Bootstrap(ct);
        await scenario.CreateStore().Bootstrap(ct);

        // Assert
        var snapshot = scenario.ReadSnapshot();
        var current = snapshot.Current.ShouldNotBeNull();
        current.SourceRevision.ShouldBe(1);
        current.SourceEpoch.ShouldNotBe(Guid.Empty);
        current.BrandName.ShouldBe(settings.BrandName);
        snapshot.Revisions.ShouldHaveSingleItem().SourceRevision.ShouldBe(1);
        snapshot.Outbox.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Bootstrap_materializes_defaults_as_revision_one_when_no_row_exists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scenario = BrandingStoreScenario.Create();

        // Act
        await scenario.CreateStore().Bootstrap(ct);

        // Assert
        var snapshot = scenario.ReadSnapshot();
        var current = snapshot.Current.ShouldNotBeNull();
        current.SourceRevision.ShouldBe(1);
        current.BrandName.ShouldBe(BrandingDefaults.CreateDto().BrandName);
        snapshot.Revisions.ShouldHaveSingleItem();
        snapshot.Outbox.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Bootstrap_fails_visibly_for_invalid_persisted_settings()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scenario = BrandingStoreScenario.Create();
        await scenario.SeedInvalidUnrevisionedSettings(ct);

        // Act
        var bootstrap = () => scenario.CreateStore().Bootstrap(ct);

        // Assert
        await bootstrap.ShouldThrow<InvalidOperationException>();
        var snapshot = scenario.ReadSnapshot();
        snapshot.Current.ShouldNotBeNull().SourceRevision.ShouldBe(0);
        snapshot.Revisions.ShouldBeEmpty();
        snapshot.Outbox.ShouldBeEmpty();
    }
}
