using SharedKernel.Testing;
using SharedKernel.Branding;
using ViajantesTurismo.Resources;
using ViajantesTurismo.Branding.Infrastructure;

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
        await using var dbContext = scenario.CreateDbContext();
        var store = new EfBrandingSettingsStore(dbContext);

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
        await using (var setupContext = scenario.CreateDbContext())
        {
            var setupStore = new EfBrandingSettingsStore(setupContext);
            await setupStore.SaveSettings(settings, TestContext.Current.CancellationToken);
        }
        var migrationHistoryBeforeReset = await scenario.GetMigrationHistory(TestContext.Current.CancellationToken);

        // Act
        await scenario.ResetSchemas(TestContext.Current.CancellationToken);
        var migrationHistoryAfterReset = await scenario.GetMigrationHistory(TestContext.Current.CancellationToken);
        await scenario.ApplyMigrations(TestContext.Current.CancellationToken);
        await using var verificationContext = scenario.CreateDbContext();
        var verificationStore = new EfBrandingSettingsStore(verificationContext);
        var saved = await verificationStore.GetSettings(TestContext.Current.CancellationToken);

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
