using Microsoft.EntityFrameworkCore;
using Npgsql;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.Resources;
using TestTraits = ViajantesTurismo.Branding.ApiServiceTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Branding.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
public sealed class EfBrandingSettingsStoreTests
{
    [Fact]
    public void Branding_infrastructure_marker_exposes_entry_assembly()
    {
        // Arrange
        var markerAssembly = BrandingInfrastructureMarker.Assembly;

        // Act
        var infrastructureAssembly = typeof(BrandingDbContext).Assembly;

        // Assert
        markerAssembly.ShouldBe(infrastructureAssembly);
    }

    [Fact]
    public void Branding_infrastructure_registers_store_with_catalog_database_configuration()
    {
        // Arrange
        using var services = BrandingInfrastructureRegistrationScope.Create();

        // Act
        var store = services.GetBrandingSettingsStore();

        // Assert
        store.ShouldNotBeNull();
    }

    [Fact]
    public void Branding_infrastructure_registers_its_context_qualified_messaging_services()
    {
        // Arrange
        using var services = BrandingInfrastructureRegistrationScope.Create();

        // Act
        var registrations = services.GetMessagingRegistrations();

        // Assert
        registrations.HasOutbox.ShouldBeTrue();
        registrations.HasTransportPublisher.ShouldBeTrue();
        registrations.HasSerializer.ShouldBeTrue();
        registrations.OutboxRelayCount.ShouldBe(1);
        registrations.OutboxSchema.ShouldBe("branding");
        registrations.TransportSchema.ShouldBe("messaging");
        registrations.TransportExcludedFromMigrations.ShouldBeTrue();
    }

    [Fact]
    public void Branding_infrastructure_can_omit_the_runtime_outbox_relay()
    {
        // Arrange
        using var services = BrandingInfrastructureRegistrationScope.Create(addOutboxRelay: false);

        // Act
        var registrations = services.GetMessagingRegistrations();

        // Assert
        registrations.HasOutbox.ShouldBeTrue();
        registrations.HasTransportPublisher.ShouldBeTrue();
        registrations.HasSerializer.ShouldBeTrue();
        registrations.OutboxRelayCount.ShouldBe(0);
    }

    [Fact]
    public void Design_time_factory_configures_branding_postgresql_provider()
    {
        // Arrange
        var factory = new BrandingDbContextDesignTimeFactory();

        // Act
        using var dbContext = factory.CreateDbContext([]);
        var providerName = dbContext.Database.ProviderName;
        var connectionString = dbContext.Database.GetDbConnection().ConnectionString;
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);

        // Assert
        providerName.ShouldBe("Npgsql.EntityFrameworkCore.PostgreSQL");
        connectionStringBuilder.Host.ShouldBe("localhost");
        connectionStringBuilder.Database.ShouldBe("branding-design-time");
    }

    [Fact]
    public async Task GetSettings_returns_null_when_no_settings_have_been_saved()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BrandingDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;
        await using var dbContext = new BrandingDbContext(options);
        var store = new EfBrandingSettingsStore(dbContext);

        // Act
        var settings = await store.GetSettings(TestContext.Current.CancellationToken);

        // Assert
        settings.ShouldBeNull();
    }

    [Fact]
    public async Task SaveSettings_persists_validated_branding_settings()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BrandingDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;
        await using var dbContext = new BrandingDbContext(options);
        var store = new EfBrandingSettingsStore(dbContext);
        var settings = BrandingSettings.Create(new BrandingSettingsDto
        {
            BrandName = "Viajantes Turismo",
            PrimaryColor = "#0F766E",
            AccentColor = "#F97316",
            BackgroundColor = "#FFFBF5",
            TextColor = "#1F2937",
            HeadingFontFamily = "Georgia",
            BodyFontFamily = "system-ui"
        }, BrandingFontFamilies.All).Value;

        // Act
        await store.SaveSettings(settings, TestContext.Current.CancellationToken);
        var saved = await store.GetSettings(TestContext.Current.CancellationToken);

        // Assert
        saved.ShouldNotBeNull();
        saved.BrandName.ShouldBe("Viajantes Turismo");
        saved.PrimaryColor.ShouldBe("#0F766E");
    }

    [Fact]
    public void ToSettings_returns_null_when_stored_row_is_invalid()
    {
        // Arrange
        var record = new BrandingSettingsRecord(
            BrandingSettingsRecord.SettingsId,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            null);

        // Act
        var settings = record.ToSettings(BrandingFontFamilies.All);

        // Assert
        settings.ShouldBeNull();
    }

    [Fact]
    public async Task SaveSettings_replaces_existing_settings()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BrandingDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;
        await using var dbContext = new BrandingDbContext(options);
        var store = new EfBrandingSettingsStore(dbContext);
        var initial = BrandingSettings.Create(new BrandingSettingsDto
        {
            BrandName = "Viajantes Turismo",
            PrimaryColor = "#0F766E",
            AccentColor = "#F97316",
            BackgroundColor = "#FFFBF5",
            TextColor = "#1F2937",
            HeadingFontFamily = "Georgia",
            BodyFontFamily = "system-ui"
        }, BrandingFontFamilies.All).Value;
        var replacement = BrandingSettings.Create(new BrandingSettingsDto
        {
            BrandName = "Updated Brand",
            PrimaryColor = "#112233",
            AccentColor = "#445566",
            BackgroundColor = "#FFFFFF",
            TextColor = "#000000",
            HeadingFontFamily = "Inter",
            BodyFontFamily = "Verdana",
            LogoUri = "/assets/logo.svg"
        }, BrandingFontFamilies.All).Value;

        // Act
        await store.SaveSettings(initial, TestContext.Current.CancellationToken);
        await store.SaveSettings(replacement, TestContext.Current.CancellationToken);
        var saved = await store.GetSettings(TestContext.Current.CancellationToken);

        // Assert
        saved.ShouldNotBeNull();
        saved.BrandName.ShouldBe("Updated Brand");
        saved.PrimaryColor.ShouldBe("#112233");
        saved.LogoUri.ShouldBe("/assets/logo.svg");
    }
}
