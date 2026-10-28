using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Branding;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(PandaBear.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class BrandingCacheFreshnessTests
{
    private const string PublicSettingsPath = $"/api/v1/{BrandingRoutes.PublicSettingsPath}";

    [Fact]
    public async Task Public_branding_response_advertises_the_accepted_60_second_fresh_300_second_stale_bound()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.Create(TestBrandingSettingsStore.Seeded());
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri(PublicSettingsPath, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.Public.ShouldBeTrue();
        cacheControl.MaxAge.ShouldBe(TimeSpan.FromSeconds(60));
        cacheControl.ToString().ShouldContain("stale-while-revalidate=300", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Concurrent_public_reads_after_the_first_cold_load_reuse_the_cached_response()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.CreateAnonymous(store);
        using var client = factory.CreateClient();
        using var cold = await client.GetAsync(new Uri(PublicSettingsPath, UriKind.Relative), TestContext.Current.CancellationToken);
        var loadsAfterColdRead = store.ReadCount;

        // Act
        var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ =>
            client.GetAsync(new Uri(PublicSettingsPath, UriKind.Relative), TestContext.Current.CancellationToken)));

        // Assert
        cold.StatusCode.ShouldBe(HttpStatusCode.OK);
        var okCount = responses.Count(response => response.StatusCode == HttpStatusCode.OK);
        okCount.ShouldBe(responses.Length);
        loadsAfterColdRead.ShouldBe(1);
        store.ReadCount.ShouldBe(loadsAfterColdRead);
    }

    [Fact]
    public async Task Remote_instance_serves_bounded_stale_data_while_the_writer_instance_and_persistence_stay_current()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var writer = BrandingApiTestHost.CreateAnonymous(store);
        await using var remote = BrandingApiTestHost.CreateAnonymous(store);
        using var writerClient = writer.CreateClient();
        BrandingApiTestHost.ConfigureAuthenticatedClient(writerClient, "Admin");
        using var remoteClient = remote.CreateClient();
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
        using var warm = await remoteClient.GetAsync(new Uri(PublicSettingsPath, UriKind.Relative), TestContext.Current.CancellationToken);

        // Act
        using var write = await BrandingManagementRequests.PutSettings(writerClient, request, "*");
        var writerRead = await writerClient.GetFromJsonAsync<BrandingSettingsDto>(
            new Uri(PublicSettingsPath, UriKind.Relative),
            TestContext.Current.CancellationToken);
        var remoteRead = await remoteClient.GetFromJsonAsync<BrandingSettingsDto>(
            new Uri(PublicSettingsPath, UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        write.StatusCode.ShouldBe(HttpStatusCode.OK);
        store.Settings.ShouldNotBeNull().BrandName.ShouldBe("Updated");
        writerRead.ShouldNotBeNull().BrandName.ShouldBe("Updated");
        remoteRead.ShouldNotBeNull().BrandName.ShouldBe("Seeded");
    }
}
