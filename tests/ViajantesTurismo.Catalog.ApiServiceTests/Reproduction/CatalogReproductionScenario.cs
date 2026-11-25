using System.Diagnostics;
using SharedKernel.Testing.Snapshots;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Testing.Reproduction;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Reproduction;

/// <summary>Defines the reviewed Catalog fixture input and the manifest vocabulary.</summary>
internal static class CatalogReproductionScenario
{
    private const string InitialTourTitle = "Initial synthetic Catalog tour";
    private const string TourSlug = "synthetic-catalog-tour";

    /// <summary>The title of the successful management edit.</summary>
    internal const string CommittedTourTitle = "Committed synthetic Catalog edit";
    /// <summary>The request marker in the rejected stale edit.</summary>
    internal const string StaleTourTitle = "Stale synthetic Catalog edit marker";
    /// <summary>The title of the approved Brazilian Portuguese variant.</summary>
    internal const string LocalizedTitle = "Passeio de teste";
    /// <summary>The synthetic body returned in the localized public response.</summary>
    internal const string LocalizedBody = "Marcador sintetico da leitura localizada";
    /// <summary>A synthetic token-shaped header value excluded from artifacts.</summary>
    internal const string HeaderMarker = "Bearer synthetic-catalog-reproduction-header";
    /// <summary>The approved outcome for the stale update.</summary>
    internal const string StaleWriteOutcome = "stale-write-conflict";
    /// <summary>The approved outcome for the localized public read.</summary>
    internal const string LocalizedReadOutcome = "localized-content-returned";

    /// <summary>Registers the stale-write fixture and its permitted outcome.</summary>
    internal static ReproductionSpecification StaleWrite { get; } = Specify("catalog.presentation-stale-write", StaleWriteOutcome);

    /// <summary>Registers the localized public-read fixture and its permitted outcome.</summary>
    internal static ReproductionSpecification LocalizedRead { get; } = Specify("catalog.localized-public-content", LocalizedReadOutcome);

    /// <summary>Recreates the Catalog store and exercises the management write and read boundaries.</summary>
    internal static async Task<CatalogManagementReproductionRun> RunManagement(CancellationToken ct)
    {
        using var activity = new Activity(StaleWrite.Scenario);
        var attempt = StartAttempt(StaleWrite, activity);
        var tourStore = new TestCatalogTourReadModelStore();
        var tourId = await SeedTour(tourStore, ct);
        var tourUri = new Uri($"/api/v1/catalog/tours/{tourId}", UriKind.Relative);
        var presentationUri = new Uri($"{tourUri}/presentation", UriKind.Relative);
        var currentEdit = new UpsertCatalogTourPresentationRequest
        {
            Title = CommittedTourTitle,
            Slug = TourSlug,
            ExpectedVersion = 1
        };
        var staleEdit = currentEdit with
        {
            Title = StaleTourTitle
        };
        var invalidEdit = currentEdit with
        {
            Title = string.Empty
        };

        await using var baseFactory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore());
        await using var factory = attempt.Observe(baseFactory);
        using var admin = factory.CreateClient();
        using var guest = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(guest, "Guest");
        attempt.PropagateTrace(admin);
        attempt.PropagateTrace(guest);
        using var forbidden = await guest.PutAsJsonAsync(presentationUri, staleEdit, ct);
        using var invalid = await admin.PutAsJsonAsync(presentationUri, invalidEdit, ct);
        using var committed = await admin.PutAsJsonAsync(presentationUri, currentEdit, ct);
        using var stale = await admin.PutAsJsonAsync(presentationUri, staleEdit, ct);
        using var read = await admin.GetAsync(tourUri, ct);
        var presentation = read.IsSuccessStatusCode ? await read.Content.ReadFromJsonAsync<CatalogTourDto>(ct) : null;
        return new CatalogManagementReproductionRun(tourId, forbidden.StatusCode, invalid.StatusCode, committed.StatusCode,
            stale.StatusCode, read.StatusCode, presentation, attempt.ObservedRequests, attempt.Capture());
    }

    /// <summary>Recreates a published content record and reads it anonymously in the requested locale.</summary>
    internal static async Task<CatalogLocalizedReadReproductionRun> RunLocalizedRead(CancellationToken ct)
    {
        using var activity = new Activity(LocalizedRead.Scenario);
        var attempt = StartAttempt(LocalizedRead, activity);
        var contentStore = new TestPublicContentStore();
        var contentKey = await SeedPublishedContent(contentStore, ct);
        var localized = new Uri($"/api/v1/public/catalog/content/{contentKey}?culture=pt-BR", UriKind.Relative);
        await using var baseFactory = CatalogApiTestHost.Create(new TestCatalogTourReadModelStore(), contentStore);
        await using var factory = attempt.Observe(baseFactory);
        using var anonymous = factory.CreateClient();
        anonymous.DefaultRequestHeaders.Authorization = null;
        anonymous.DefaultRequestHeaders.Add("X-Reproduction-Marker", HeaderMarker);
        attempt.PropagateTrace(anonymous);
        using var response = await anonymous.GetAsync(localized, ct);
        var content = response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<PublicContentVariantDto>(ct) : null;
        return new CatalogLocalizedReadReproductionRun(contentKey, response.StatusCode, content, attempt.ObservedRequests, attempt.Capture());
    }

    private static CatalogReproductionAttempt StartAttempt(ReproductionSpecification specification, Activity activity)
    {
        activity.SetIdFormat(ActivityIdFormat.W3C)
            .SetParentId(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded)
            .Start();
        return new CatalogReproductionAttempt(specification, activity.TraceId, activity.Id);
    }

    private static async Task<Guid> SeedTour(TestCatalogTourReadModelStore store, CancellationToken ct)
    {
        var tourId = Guid.CreateVersion7();
        await store.UpsertDraft(new CatalogTourDraftReadModel(tourId, Guid.CreateVersion7(), "REPRO-CATALOG",
            InitialTourTitle, TourSlug, false, 1, new DateTimeOffset(2030, 1, 2, 10, 0, 0, TimeSpan.Zero)), ct);
        return tourId;
    }

    private static async Task<string> SeedPublishedContent(TestPublicContentStore store, CancellationToken ct)
    {
        var key = $"reproduction.{Guid.NewGuid():N}";
        var source = PublicContentVariant.Create(PublicContentCultures.EnglishUnitedStates,
            "Synthetic English source", "Synthetic original locale content", null, null, null,
            requiresHumanReview: false);
        var localized = PublicContentVariant.Create(PublicContentCultures.PortugueseBrazil,
            LocalizedTitle, LocalizedBody, null, null, null, requiresHumanReview: false);
        var entry = EditablePublicContent.Create(key, PublicContentCultures.EnglishUnitedStates, [source.Value, localized.Value]).Value;
        entry.Publish().IsSuccess.ShouldBeTrue();
        await store.SaveContent(entry, ct);
        return key;
    }

    private static ReproductionSpecification Specify(string scenario, string code) => new(
        scenario, ReproductionFixtureEnvironment.GetSourceRevision(typeof(CatalogReproductionScenario).Assembly),
        [new ReproductionOutcome("reproduced", code, new Dictionary<string, string>(StringComparer.Ordinal) { ["catalog-api"] = "ready" })]);
}
