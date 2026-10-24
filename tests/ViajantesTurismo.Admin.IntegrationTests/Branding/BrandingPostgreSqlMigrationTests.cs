using PandaBear.Testing;
using PandaBear.Branding;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.IntegrationTests.Branding;

[Trait(TestTraitNames.ScopeName, TestTraits.DatabaseIntegrationScope)]
[Trait(TestTraitNames.AreaName, TestTraits.BrandingArea)]
public sealed class BrandingPostgreSqlMigrationTests(BrandingPostgreSqlMigrationScenario scenario)
    : IClassFixture<BrandingPostgreSqlMigrationScenario>
{
    [Fact]
    public async Task Initial_branding_migration_creates_the_dedicated_schema_and_store_round_trips_settings()
    {
        // Arrange
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
