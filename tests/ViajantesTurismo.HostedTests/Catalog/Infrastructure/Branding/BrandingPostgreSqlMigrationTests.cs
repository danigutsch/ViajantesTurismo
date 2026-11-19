using Npgsql;
using PandaBear.Testing;
using PandaBear.Branding;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Branding;

[Trait(TestTraitNames.AreaName, Admin.Api.Infrastructure.TestTraits.BrandingArea)]
[TestScope(TestScope.Integration)]
public sealed class BrandingPostgreSqlMigrationTests(BrandingPostgreSqlMigrationScenario scenario)
    : IClassFixture<BrandingPostgreSqlMigrationScenario>
{
    [Fact]
    public async Task A_competing_same_hash_insert_reuses_the_committed_logo_and_finishes_the_revision()
    {
        // Arrange
        var testCancellation = TestContext.Current.CancellationToken;
        await scenario.ResetSchemas(testCancellation);
        await scenario.ApplyMigrations(testCancellation);
        await using (var bootstrap = scenario.CreateStore())
        {
            await bootstrap.Store.Bootstrap(testCancellation);
        }
        var logo = BrandingLogo.FromEncodedBytes(new byte[] { 1, 2, 3 });
        var race = new BrandingLogoInsertRace(ct => scenario.InsertLogo(logo, DateTimeOffset.UnixEpoch, ct));
        await using var writer = scenario.CreateStore(race);

        // Act
        var result = await writer.Store.ReplaceLogoIfMatch(logo, _ => true, testCancellation);

        // Assert
        race.WasInjected.ShouldBeTrue();
        result.ShouldNotBeNull().SourceRevision.ShouldBe(2);
        var logoCount = await scenario.LogoCount(testCancellation);
        logoCount.ShouldBe(1);
        var counts = await scenario.ReadRevisionCounts(testCancellation);
        counts.ShouldBe((2L, 2L, 2L));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(40 * 1024 + 1)]
    public async Task Logo_storage_rejects_bytes_outside_the_encoded_size_range(int length)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await scenario.ResetSchemas(ct);
        await scenario.ApplyMigrations(ct);
        var logo = BrandingLogo.FromEncodedBytes(new byte[length]);

        // Act
        var insert = () => scenario.InsertLogo(logo, DateTimeOffset.UnixEpoch, ct);

        // Assert
        var error = await insert.ShouldThrow<PostgresException>();
        error.SqlState.ShouldBe(PostgresErrorCodes.CheckViolation);
        var count = await scenario.LogoCount(ct);
        count.ShouldBe(0);
    }

    [Fact]
    public async Task The_outbox_table_rejects_an_update_over_the_utf8_event_budget()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await scenario.ResetSchemas(ct);
        await scenario.ApplyMigrations(ct);
        await using (var bootstrap = scenario.CreateStore())
        {
            await bootstrap.Store.Bootstrap(ct);
        }
        await scenario.ReplaceOutboxPayload(new string('é', 32 * 1024), ct);

        // Act
        var update = () => scenario.ReplaceOutboxPayload(new string('é', (32 * 1024) + 1), ct);

        // Assert
        var error = await update.ShouldThrow<PostgresException>();
        error.SqlState.ShouldBe(PostgresErrorCodes.CheckViolation);
    }

    [Fact]
    public async Task A_failed_revision_rolls_back_its_new_logo_and_outbox_row()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await scenario.ResetSchemas(ct);
        await scenario.ApplyMigrations(ct);
        await using (var bootstrap = scenario.CreateStore())
        {
            await bootstrap.Store.Bootstrap(ct);
        }
        await scenario.InsertCompetingRevision(revision: 2, ct);
        await using var writer = scenario.CreateStore();
        var logo = BrandingLogo.FromEncodedBytes(new byte[] { 1, 3, 5 });

        // Act
        var replace = () => writer.Store.ReplaceLogoIfMatch(logo, _ => true, ct);

        // Assert
        await replace.ShouldThrow<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>();
        var logoCount = await scenario.LogoCount(ct);
        logoCount.ShouldBe(0);
        var counts = await scenario.ReadRevisionCounts(ct);
        counts.ShouldBe((1L, 2L, 1L));
    }

    [Fact]
    public async Task Reclamation_deletes_old_orphans_in_batches_and_preserves_current_historic_and_young_rows()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await scenario.ResetSchemas(ct);
        await scenario.ApplyMigrations(ct);
        await using var store = scenario.CreateStore();
        await store.Store.Bootstrap(ct);
        var history = BrandingLogo.FromEncodedBytes(new byte[] { 1 });
        var current = BrandingLogo.FromEncodedBytes(new byte[] { 2 });
        var oldest = BrandingLogo.FromEncodedBytes(new byte[] { 3 });
        var older = BrandingLogo.FromEncodedBytes(new byte[] { 4 });
        var young = BrandingLogo.FromEncodedBytes(new byte[] { 5 });
        var cutoff = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        var old = cutoff.AddDays(-1);
        await scenario.InsertLogo(history, old, ct);
        await scenario.InsertLogo(current, old, ct);
        await scenario.InsertLogo(oldest, old.AddDays(-1), ct);
        await scenario.InsertLogo(older, old, ct);
        await scenario.InsertLogo(young, cutoff, ct);
        await store.Store.ReplaceLogoIfMatch(history, _ => true, ct);
        await store.Store.ReplaceLogoIfMatch(current, _ => true, ct);

        // Act
        var first = await scenario.ReclaimLogos(cutoff, batchSize: 1, ct);
        var firstSurvivor = await store.Store.GetLogo(older.Sha256, ct);
        var firstDeleted = await store.Store.GetLogo(oldest.Sha256, ct);
        var second = await scenario.ReclaimLogos(cutoff, batchSize: 1, ct);
        var exhausted = await scenario.ReclaimLogos(cutoff, batchSize: 1, ct);
        var nextDeleted = await store.Store.GetLogo(older.Sha256, ct);
        var historicLogo = await store.Store.GetLogo(history.Sha256, ct);
        var currentLogo = await store.Store.GetLogo(current.Sha256, ct);
        var youngLogo = await store.Store.GetLogo(young.Sha256, ct);

        // Assert
        first.ShouldBe(1);
        firstSurvivor.ShouldNotBeNull();
        firstDeleted.ShouldBeNull();
        second.ShouldBe(1);
        exhausted.ShouldBe(0);
        nextDeleted.ShouldBeNull();
        historicLogo.ShouldNotBeNull();
        currentLogo.ShouldNotBeNull();
        youngLogo.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    public async Task A_competing_sweep_preserves_a_logo_reference_or_allows_the_writer_to_reinsert_it(
        bool afterReferenceLock, int expectedDeletions)
    {
        // Arrange
        var testCancellation = TestContext.Current.CancellationToken;
        await scenario.ResetSchemas(testCancellation);
        await scenario.ApplyMigrations(testCancellation);
        await using (var bootstrap = scenario.CreateStore())
        {
            await bootstrap.Store.Bootstrap(testCancellation);
        }
        var logo = BrandingLogo.FromEncodedBytes(new byte[] { 6 });
        var cutoff = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        await scenario.InsertLogo(logo, cutoff.AddDays(-1), testCancellation);
        var race = new BrandingLogoSweepRace(afterReferenceLock, ct => scenario.ReclaimLogos(cutoff, batchSize: 1, ct));
        await using var writer = scenario.CreateStore(race);

        // Act
        var written = await writer.Store.ReplaceLogoIfMatch(logo, _ => true, testCancellation);

        // Assert
        race.RemovedRows.ShouldBe(expectedDeletions);
        written.ShouldNotBeNull().SourceRevision.ShouldBe(2);
        var count = await scenario.LogoCount(testCancellation);
        count.ShouldBe(1);
        var saved = await writer.Store.GetLogo(logo.Sha256, testCancellation);
        saved.ShouldNotBeNull().Content.ToArray().ShouldBe(logo.Content.ToArray());
    }

    [Fact]
    public async Task Initial_branding_migration_creates_the_dedicated_schema_and_store_round_trips_settings()
    {
        // Arrange
        await scenario.ResetSchemas(TestContext.Current.CancellationToken);
        await scenario.ApplyMigrations(TestContext.Current.CancellationToken);
        var settings = BrandingSettings.Create(new BrandingSettingsDto
        {
            BrandName = "Viajantes Turismo",
            PrimaryColor = "#0F766E",
            AccentColor = "#F97316",
            BackgroundColor = "#FFFBF5",
            TextColor = "#1F2937",
            HeadingFontFamily = "Georgia",
            BodyFontFamily = "system-ui",
        }, BrandingFontFamilies.All).Value;
        await using var lease = scenario.CreateStore();
        var store = lease.Store;

        // Act
        await store.SaveSettings(settings, TestContext.Current.CancellationToken);
        var saved = await store.GetSettings(TestContext.Current.CancellationToken);
        var brandingTableExists = await scenario.BrandingSettingsTableExists(TestContext.Current.CancellationToken);
        var publicTableExists = await scenario.PublicBrandingSettingsTableExists(TestContext.Current.CancellationToken);

        // Assert
        brandingTableExists.ShouldBeTrue();
        publicTableExists.ShouldBeFalse();
        saved.ShouldNotBeNull();
        saved.BrandName.ShouldBe("Viajantes Turismo");
    }

    [Fact]
    public async Task A_stale_branding_writer_conflicts_without_writing_history_or_events()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await scenario.ResetSchemas(ct);
        await scenario.ApplyMigrations(ct);
        await using (var bootstrap = scenario.CreateStore())
        {
            await bootstrap.Store.Bootstrap(ct);
        }
        await using var writerA = scenario.CreateStore();
        await using var writerB = scenario.CreateStore();
        _ = await writerA.Store.GetSettings(ct);
        _ = await writerB.Store.GetSettings(ct);
        await writerA.Store.SaveSettings(BrandingPostgreSqlTestData.CreateSettings("From A"), ct);

        // Act
        var save = () => writerB.Store.SaveSettings(BrandingPostgreSqlTestData.CreateSettings("From B"), ct);

        // Assert
        await save.ShouldThrow<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>();
        var counts = await scenario.ReadRevisionCounts(ct);
        counts.ShouldBe((2L, 2L, 2L));
    }

    [Fact]
    public async Task A_revision_key_collision_is_reported_as_a_conflict_without_a_partial_write()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await scenario.ResetSchemas(ct);
        await scenario.ApplyMigrations(ct);
        await using (var bootstrap = scenario.CreateStore())
        {
            await bootstrap.Store.Bootstrap(ct);
        }
        await scenario.InsertCompetingRevision(revision: 2, ct);
        await using var writer = scenario.CreateStore();

        // Act
        var save = () => writer.Store.SaveSettings(BrandingPostgreSqlTestData.CreateSettings("Collides"), ct);

        // Assert
        await save.ShouldThrow<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>();
        var counts = await scenario.ReadRevisionCounts(ct);
        counts.ShouldBe((1L, 2L, 1L));
    }

    [Fact]
    public async Task Branding_migrations_own_the_outbox_without_claiming_transport_tables()
    {
        // Arrange
        await scenario.ApplyMigrations(TestContext.Current.CancellationToken);

        // Act
        var outboxTableExists = await scenario.BrandingOutboxTableExists(TestContext.Current.CancellationToken);
        var brandingTransportTableExists = await scenario.BrandingTransportTableExists(TestContext.Current.CancellationToken);
        var sharedTransportTableExists = await scenario.SharedTransportTableExists(TestContext.Current.CancellationToken);

        // Assert
        outboxTableExists.ShouldBeTrue();
        brandingTransportTableExists.ShouldBeFalse();
        sharedTransportTableExists.ShouldBeFalse();
    }

    [Fact]
    public async Task Schema_reset_preserves_migration_history_and_clears_branding_settings()
    {
        // Arrange
        await scenario.ResetSchemas(TestContext.Current.CancellationToken);
        await scenario.ApplyMigrations(TestContext.Current.CancellationToken);
        var settings = BrandingSettings.Create(new BrandingSettingsDto
        {
            BrandName = "Viajantes Turismo",
            PrimaryColor = "#0F766E",
            AccentColor = "#F97316",
            BackgroundColor = "#FFFBF5",
            TextColor = "#1F2937",
            HeadingFontFamily = "Georgia",
            BodyFontFamily = "system-ui",
        }, BrandingFontFamilies.All).Value;
        await using (var setupLease = scenario.CreateStore())
        {
            await setupLease.Store.SaveSettings(settings, TestContext.Current.CancellationToken);
        }
        var migrationHistoryBeforeReset = await scenario.GetMigrationHistory(TestContext.Current.CancellationToken);

        // Act
        await scenario.ResetSchemas(TestContext.Current.CancellationToken);
        var migrationHistoryAfterReset = await scenario.GetMigrationHistory(TestContext.Current.CancellationToken);
        await scenario.ApplyMigrations(TestContext.Current.CancellationToken);
        await using var verificationLease = scenario.CreateStore();
        var saved = await verificationLease.Store.GetSettings(TestContext.Current.CancellationToken);

        // Assert
        saved.ShouldBeNull();
        var expectedMigrationHistory = migrationHistoryBeforeReset
            .Select<string, Action<string>>(migration => actual => actual.ShouldBe(migration))
            .ToArray();
        migrationHistoryAfterReset.ShouldMatchCollection(expectedMigrationHistory);
        var brandingTableExists = await scenario.BrandingSettingsTableExists(TestContext.Current.CancellationToken);
        brandingTableExists.ShouldBeTrue();
    }
}
