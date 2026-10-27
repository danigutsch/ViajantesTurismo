using System.Diagnostics.Metrics;
using ViajantesTurismo.Branding.AspNetCore;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Branding;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(PandaBear.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class BrandingCacheTelemetryTests
{
    private const string PublicSettingsPath = $"/api/v1/{BrandingRoutes.PublicSettingsPath}";

    [Fact]
    public async Task Public_reads_count_one_miss_then_one_hit_without_tags_beyond_the_outcome()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.CreateAnonymous(TestBrandingSettingsStore.Seeded());
        using var client = factory.CreateClient();
        using var recorder = new BrandingMetricRecorder(factory.Services.GetRequiredService<IMeterFactory>());

        // Act
        using var first = await client.GetAsync(new Uri(PublicSettingsPath, UriKind.Relative), TestContext.Current.CancellationToken);
        using var second = await client.GetAsync(new Uri(PublicSettingsPath, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        recorder.Sum(BrandingTelemetry.CacheRequests, "outcome", "miss").ShouldBe(1);
        recorder.Sum(BrandingTelemetry.CacheRequests, "outcome", "hit").ShouldBe(1);
        recorder.TagNames(BrandingTelemetry.CacheRequests).ShouldBe(["outcome"]);
    }

    [Fact]
    public async Task Committed_write_counts_a_successful_invalidation_and_exposes_the_source_revision()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.CreateAnonymous(store);
        using var client = factory.CreateClient();
        BrandingApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        using var recorder = new BrandingMetricRecorder(factory.Services.GetRequiredService<IMeterFactory>());
        var request = new BrandingSettingsDto
        {
            BrandName = "Updated",
            PrimaryColor = "#112233",
            AccentColor = "#445566",
            BackgroundColor = "#FFFFFF",
            TextColor = "#000000",
            HeadingFontFamily = "Inter",
            BodyFontFamily = "Verdana",
            LogoUri = null
        };

        // Act
        using var write = await BrandingManagementRequests.PutSettings(client, request, "*");
        recorder.Observe();

        // Assert
        write.StatusCode.ShouldBe(HttpStatusCode.OK);
        recorder.Sum(BrandingTelemetry.CacheInvalidations, "outcome", "succeeded").ShouldBe(1);
        recorder.Last(BrandingTelemetry.SourceRevision).ShouldBe(store.SourceRevision);
    }

    [Fact]
    public async Task Failed_eviction_counts_a_failed_invalidation_and_keeps_the_write_successful()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        var cacheStore = new RecordingOutputCacheStore { EvictionException = new InvalidOperationException("synthetic") };
        await using var factory = BrandingApiTestHost.Create(store, cacheStore);
        using var client = factory.CreateClient();
        using var recorder = new BrandingMetricRecorder(factory.Services.GetRequiredService<IMeterFactory>());
        var request = new BrandingSettingsDto
        {
            BrandName = "Updated",
            PrimaryColor = "#112233",
            AccentColor = "#445566",
            BackgroundColor = "#FFFFFF",
            TextColor = "#000000",
            HeadingFontFamily = "Inter",
            BodyFontFamily = "Verdana",
            LogoUri = null
        };

        // Act
        using var write = await BrandingManagementRequests.PutSettings(client, request, "*");

        // Assert
        write.StatusCode.ShouldBe(HttpStatusCode.OK);
        recorder.Sum(BrandingTelemetry.CacheInvalidations, "outcome", "failed").ShouldBe(1);
    }
}
