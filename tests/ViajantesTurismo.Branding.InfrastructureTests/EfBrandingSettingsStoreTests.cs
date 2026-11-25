using Microsoft.EntityFrameworkCore;
using Npgsql;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Branding.InfrastructureTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.EndpointCategory)]
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
        var (HasOutbox, HasTransportPublisher, HasSerializer, OutboxRelayCount, OutboxSchema, TransportSchema, TransportExcludedFromMigrations) = services.GetMessagingRegistrations();

        // Assert
        HasOutbox.ShouldBeTrue();
        HasTransportPublisher.ShouldBeTrue();
        HasSerializer.ShouldBeTrue();
        OutboxRelayCount.ShouldBe(1);
        OutboxSchema.ShouldBe("branding");
        TransportSchema.ShouldBe("messaging");
        TransportExcludedFromMigrations.ShouldBeTrue();
    }

    [Fact]
    public void Branding_infrastructure_can_omit_the_runtime_outbox_relay()
    {
        // Arrange
        using var services = BrandingInfrastructureRegistrationScope.Create(addOutboxRelay: false);

        // Act
        var (HasOutbox, HasTransportPublisher, HasSerializer, OutboxRelayCount, OutboxSchema, TransportSchema, TransportExcludedFromMigrations) = services.GetMessagingRegistrations();

        // Assert
        HasOutbox.ShouldBeTrue();
        HasTransportPublisher.ShouldBeTrue();
        HasSerializer.ShouldBeTrue();
        OutboxRelayCount.ShouldBe(0);
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
        using var scenario = BrandingStoreScenario.Create();
        var store = scenario.CreateStore();

        // Act
        var settings = await store.GetSettings(TestContext.Current.CancellationToken);

        // Assert
        settings.ShouldBeNull();
    }

    [Fact]
    public async Task SaveSettings_persists_validated_branding_settings()
    {
        // Arrange
        using var scenario = BrandingStoreScenario.Create();
        var store = scenario.CreateStore();
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
            null,
            null);

        // Act
        var settings = record.ToSettings(BrandingFontFamilies.All);

        // Assert
        settings.ShouldBeNull();
    }

    [Fact]
    public async Task GetLogo_returns_verified_stored_bytes()
    {
        // Arrange
        using var scenario = BrandingStoreScenario.Create();
        var ct = TestContext.Current.CancellationToken;
        var bytes = "encoded logo test bytes"u8.ToArray();
        var logo = BrandingLogo.FromEncodedBytes(bytes);
        await scenario.SeedStoredLogo(logo, bytes, logo.ContentType, ct);
        var store = scenario.CreateStore();

        // Act
        var stored = await store.GetLogo(logo.Sha256, ct);

        // Assert
        stored.ShouldNotBeNull();
        stored.Sha256.ShouldBe(logo.Sha256);
        stored.ContentType.ShouldBe(logo.ContentType);
        stored.Content.Span.SequenceEqual(bytes).ShouldBeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetLogo_does_not_return_a_row_with_mismatched_bytes_or_media_type(bool corruptBytes)
    {
        // Arrange
        using var scenario = BrandingStoreScenario.Create();
        var ct = TestContext.Current.CancellationToken;
        var bytes = "encoded logo test bytes"u8.ToArray();
        var logo = BrandingLogo.FromEncodedBytes(bytes);
        var storedBytes = corruptBytes ? "other test bytes"u8.ToArray() : bytes;
        var storedContentType = corruptBytes ? logo.ContentType : "text/html";
        await scenario.SeedStoredLogo(logo, storedBytes, storedContentType, ct);
        var store = scenario.CreateStore();

        // Act
        var stored = await store.GetLogo(logo.Sha256, ct);

        // Assert
        stored.ShouldBeNull();
    }

    [Fact]
    public async Task SaveSettings_replaces_existing_settings()
    {
        // Arrange
        using var scenario = BrandingStoreScenario.Create();
        var store = scenario.CreateStore();
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
            LogoUri = null
        }, BrandingFontFamilies.All).Value;

        // Act
        await store.SaveSettings(initial, TestContext.Current.CancellationToken);
        await store.SaveSettings(replacement, TestContext.Current.CancellationToken);
        var saved = await store.GetSettings(TestContext.Current.CancellationToken);

        // Assert
        saved.ShouldNotBeNull();
        saved.BrandName.ShouldBe("Updated Brand");
        saved.PrimaryColor.ShouldBe("#112233");
        saved.LogoUri.ShouldBeNull();
    }
}
