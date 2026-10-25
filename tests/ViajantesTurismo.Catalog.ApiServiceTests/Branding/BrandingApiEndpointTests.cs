using System.Reflection;
using Microsoft.AspNetCore.Http.HttpResults;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;
using ViajantesTurismo.Branding.AspNetCore;
using ViajantesTurismo.Branding.Contracts.Http;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Branding;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(PandaBear.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class BrandingApiEndpointTests
{
    private const string PublicSettingsPath = $"/api/v1/{BrandingRoutes.PublicSettingsPath}";

    [Fact]
    public void Branding_mutation_handler_exposes_an_explicit_typed_result_union()
    {
        // Arrange
        var endpointsType = typeof(BrandingEndpoints);

        // Act
        var method = endpointsType.GetMethod("SaveSettings", BindingFlags.Static | BindingFlags.NonPublic);

        // Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(Task<Results<Ok<BrandingManagementSettingsDto>, ProblemHttpResult>>));
    }

    [Fact]
    public async Task Public_branding_endpoint_returns_defaults_when_no_settings_are_saved()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.Create();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri(PublicSettingsPath, UriKind.Relative), TestContext.Current.CancellationToken);
        var settings = await response.Content.ReadFromJsonAsync<BrandingSettingsDto>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.Public.ShouldBeTrue();
        settings.ShouldNotBeNull();
        settings.BrandName.ShouldBe("Viajantes Turismo");
        settings.PrimaryColor.ShouldBe("#0F766E");
        settings.AccentColor.ShouldBe("#F97316");
        settings.BackgroundColor.ShouldBe("#FFFBF5");
        settings.TextColor.ShouldBe("#1F2937");
        settings.HeadingFontFamily.ShouldBe("Georgia");
        settings.BodyFontFamily.ShouldBe("system-ui");
        settings.LogoUri.ShouldBeNull();
    }

    [Fact]
    public async Task Branding_management_endpoint_saves_and_public_endpoint_reads_settings()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();
        var request = new BrandingSettingsDto
        {
            BrandName = "Viajantes",
            PrimaryColor = "#112233",
            AccentColor = "#445566",
            BackgroundColor = "#FFFFFF",
            TextColor = "#000000",
            HeadingFontFamily = "Inter",
            BodyFontFamily = "Verdana",
            LogoUri = "https://cdn.example.test/logo.svg"
        };

        // Act
        using var writeResponse = await BrandingManagementRequests.PutSettings(client, request, "*");
        using var readResponse = await client.GetAsync(new Uri(PublicSettingsPath, UriKind.Relative), TestContext.Current.CancellationToken);
        var settings = await readResponse.Content.ReadFromJsonAsync<BrandingSettingsDto>(TestContext.Current.CancellationToken);

        // Assert
        writeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var writeCacheControl = writeResponse.Headers.CacheControl.ShouldNotBeNull();
        writeCacheControl.NoStore.ShouldBeTrue();
        readResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        settings.ShouldNotBeNull();
        settings.BrandName.ShouldBe("Viajantes");
        settings.PrimaryColor.ShouldBe("#112233");
        settings.LogoUri.ShouldBe("https://cdn.example.test/logo.svg");
    }

    [Fact]
    public async Task Branding_management_endpoint_uses_detached_post_commit_cache_eviction()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        var outputCacheStore = new RecordingOutputCacheStore();
        await using var factory = BrandingApiTestHost.Create(store, outputCacheStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await BrandingManagementRequests.PutSettings(client, BrandingSettingsTestData.CreateDto(), "*");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        outputCacheStore.EvictionObserved.ShouldBeTrue();
        outputCacheStore.EvictionTag.ShouldBe(BrandingHttpCache.PublicBrandingTag);
        outputCacheStore.EvictionCancellationToken.ShouldBe(CancellationToken.None);
    }

    [Fact]
    public async Task Branding_management_endpoint_returns_success_and_logs_bounded_cache_failure_metadata()
    {
        // Arrange
        const string sensitiveFailureMessage = "synthetic cache message must not be logged";
        var store = TestBrandingSettingsStore.Seeded();
        var outputCacheStore = new RecordingOutputCacheStore
        {
            EvictionException = new IOException(sensitiveFailureMessage)
        };
        var logger = new CollectingLogger<BrandingAspNetCoreMarker>();
        await using var factory = BrandingApiTestHost.Create(store, outputCacheStore, logger);
        using var client = factory.CreateClient();

        // Act
        using var response = await BrandingManagementRequests.PutSettings(client, BrandingSettingsTestData.CreateDto(), "*");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        store.Settings.ShouldNotBeNull();
        outputCacheStore.EvictionObserved.ShouldBeTrue();
        logger.StructuredValues.ShouldContain(value =>
            value.Key == "CacheArea" && Equals(value.Value, BrandingHttpCache.PublicBrandingArea));
        logger.StructuredValues.ShouldContain(value =>
            value.Key == "FailureType" && Equals(value.Value, nameof(IOException)));
        logger.Messages.ShouldNotContain(message => message.Contains(sensitiveFailureMessage, StringComparison.Ordinal));
        logger.Exceptions.ShouldBeEmpty();
    }

    [Fact]
    public async Task Branding_management_endpoint_does_not_evict_cache_when_settings_save_fails()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        store.SaveException = new IOException("synthetic settings failure");
        var outputCacheStore = new RecordingOutputCacheStore();
        await using var factory = BrandingApiTestHost.Create(store, outputCacheStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await BrandingManagementRequests.PutSettings(client, BrandingSettingsTestData.CreateDto(), "*");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        outputCacheStore.EvictionObserved.ShouldBeFalse();
    }

    [Fact]
    public async Task Branding_management_endpoint_returns_precondition_failed_when_another_writer_commits_first()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        store.SaveException = new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException("synthetic stale revision");
        var outputCacheStore = new RecordingOutputCacheStore();
        await using var factory = BrandingApiTestHost.Create(store, outputCacheStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await BrandingManagementRequests.PutSettings(client, BrandingSettingsTestData.CreateDto(), "*");
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
        body.ShouldContain(BrandingManagementProblemCodes.PreconditionFailed, StringComparison.Ordinal);
        body.ShouldNotContain("synthetic stale revision", StringComparison.Ordinal);
        outputCacheStore.EvictionObserved.ShouldBeFalse();
    }

    [Fact]
    public async Task Branding_management_read_returns_the_revision_and_a_strong_entity_tag()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();

        // Act
        using var response = await BrandingManagementRequests.GetSettings(client);
        var settings = await response.Content.ReadFromJsonAsync<BrandingManagementSettingsDto>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var entityTag = response.Headers.ETag.ShouldNotBeNull();
        entityTag.IsWeak.ShouldBeFalse();
        entityTag.Tag.ShouldBe(BrandingEntityTags.Format(store.SourceEpoch, 1));
        settings.ShouldNotBeNull();
        settings.SourceEpoch.ShouldBe(store.SourceEpoch);
        settings.SourceRevision.ShouldBe(1);
        settings.BrandName.ShouldBe("Seeded");
    }

    [Fact]
    public async Task Branding_management_read_fails_closed_before_revisions_start()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.Create(new TestBrandingSettingsStore());
        using var client = factory.CreateClient();

        // Act
        using var response = await BrandingManagementRequests.GetSettings(client);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        response.Headers.ETag.ShouldBeNull();
    }

    [Fact]
    public async Task Branding_management_write_requires_if_match()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();

        // Act
        using var response = await BrandingManagementRequests.PutSettings(client, BrandingSettingsTestData.CreateDto());
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe((HttpStatusCode)StatusCodes.Status428PreconditionRequired);
        body.ShouldContain(BrandingManagementProblemCodes.PreconditionRequired, StringComparison.Ordinal);
        store.WriteCount.ShouldBe(0);
    }

    [Theory]
    [InlineData("not-a-tag")]
    [InlineData("*, \"abc\"")]
    [InlineData("\"unterminated")]
    public async Task Branding_management_write_rejects_malformed_if_match(string ifMatch)
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();

        // Act
        using var response = await BrandingManagementRequests.PutSettings(client, BrandingSettingsTestData.CreateDto(), ifMatch);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        body.ShouldContain(BrandingManagementProblemCodes.InvalidIfMatch, StringComparison.Ordinal);
        store.WriteCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Branding_management_write_rejects_a_condition_without_a_strong_current_match(bool weak, bool stale)
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        var outputCacheStore = new RecordingOutputCacheStore();
        await using var factory = BrandingApiTestHost.Create(store, outputCacheStore);
        using var client = factory.CreateClient();
        var tag = stale
            ? BrandingEntityTags.Format(Guid.CreateVersion7(), 1)
            : BrandingEntityTags.Format(store.SourceEpoch, weak ? 1 : 2);
        var ifMatch = weak ? "W/" + BrandingEntityTags.Format(store.SourceEpoch, 1) : tag;

        // Act
        using var response = await BrandingManagementRequests.PutSettings(client, BrandingSettingsTestData.CreateDto("Changed"), ifMatch);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
        body.ShouldContain(BrandingManagementProblemCodes.PreconditionFailed, StringComparison.Ordinal);
        store.WriteCount.ShouldBe(0);
        store.Settings.ShouldNotBeNull().BrandName.ShouldBe("Seeded");
        outputCacheStore.EvictionObserved.ShouldBeFalse();
    }

    [Fact]
    public async Task Branding_management_write_accepts_a_list_with_the_current_tag_and_omits_the_validator()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();
        var ifMatch = $"W/{BrandingEntityTags.Format(store.SourceEpoch, 1)}, \"other\", {BrandingEntityTags.Format(store.SourceEpoch, 1)}";

        // Act
        using var response = await BrandingManagementRequests.PutSettings(client, BrandingSettingsTestData.CreateDto("Changed"), ifMatch);
        var saved = await response.Content.ReadFromJsonAsync<BrandingManagementSettingsDto>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.ETag.ShouldBeNull();
        saved.ShouldNotBeNull();
        saved.SourceRevision.ShouldBe(2);
        saved.BrandName.ShouldBe("Changed");
    }

    [Fact]
    public async Task Second_editor_with_the_earlier_tag_cannot_overwrite_the_first_editor()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();
        using var read = await BrandingManagementRequests.GetSettings(client);
        var sharedTag = read.Headers.ETag.ShouldNotBeNull().Tag;

        // Act
        using var first = await BrandingManagementRequests.PutSettings(client, BrandingSettingsTestData.CreateDto("Editor A"), sharedTag);
        using var second = await BrandingManagementRequests.PutSettings(client, BrandingSettingsTestData.CreateDto("Editor B"), sharedTag);
        using var refreshed = await BrandingManagementRequests.GetSettings(client);

        // Assert
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
        store.Settings.ShouldNotBeNull().BrandName.ShouldBe("Editor A");
        refreshed.Headers.ETag.ShouldNotBeNull().Tag.ShouldBe(BrandingEntityTags.Format(store.SourceEpoch, 2));
    }

    [Fact]
    public async Task Branding_management_write_with_a_wildcard_fails_when_no_settings_exist()
    {
        // Arrange
        var store = new TestBrandingSettingsStore();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();

        // Act
        using var response = await BrandingManagementRequests.PutSettings(client, BrandingSettingsTestData.CreateDto(), "*");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
        store.WriteCount.ShouldBe(0);
    }

    [Fact]
    public async Task Branding_management_endpoint_rejects_unsafe_values()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.Create();
        using var client = factory.CreateClient();
        var request = new BrandingSettingsDto
        {
            BrandName = "Viajantes Turismo",
            PrimaryColor = "red; background:url(javascript:alert(1))",
            AccentColor = "#445566",
            BackgroundColor = "#FFFFFF",
            TextColor = "#000000",
            HeadingFontFamily = "Papyrus",
            BodyFontFamily = "Verdana",
            LogoUri = "javascript:alert(1)"
        };

        // Act
        using var response = await BrandingManagementRequests.PutSettings(client, request, "*");
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/problem+json");
        problem.ShouldNotBeNull();
        problem.Status.ShouldBe(StatusCodes.Status400BadRequest);
        problem.Title.ShouldBe("One or more validation errors occurred.");
        problem.Errors.Keys.ShouldContain(nameof(BrandingSettingsDto.PrimaryColor));
        problem.Errors.Keys.ShouldContain(nameof(BrandingSettingsDto.HeadingFontFamily));
        problem.Errors.Keys.ShouldContain(nameof(BrandingSettingsDto.LogoUri));
    }
}
