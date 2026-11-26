using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SharedKernel.EventSourcing;
using SharedKernel.EventSourcing.Npgsql;
using SharedKernel.Idempotency;
using SharedKernel.MalwareScanning.ClamAv;
using SharedKernel.Messaging.IntegrationEvents;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using SharedKernel.ObjectStorage;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Catalog.Application.Gallery;
using ViajantesTurismo.Catalog.Application.IntegrationEvents;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Domain.Tours;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Catalog.Testing.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure.Tours;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Uses the real Catalog journal, EF outbox and event store on a disposable database.</summary>
internal sealed class CatalogGalleryIntentPostgreSqlScenario(
    ServiceProvider provider, AsyncServiceScope scope) : IAsyncDisposable
{
    /// <summary>Gets the scoped Catalog database.</summary>
    public CatalogDbContext Database => scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

    /// <summary>Gets the initialized Catalog event store.</summary>
    public PostgreSqlEventStore Events => (PostgreSqlEventStore)scope.ServiceProvider.GetRequiredService<IEventStore>();

    /// <summary>Gets the persistent placement journal.</summary>
    public ICatalogGalleryPlacementIntentStore Intents => scope.ServiceProvider.GetRequiredService<ICatalogGalleryPlacementIntentStore>();

    /// <summary>Gets the tour read model store.</summary>
    public ICatalogTourReadModelStore Tours => scope.ServiceProvider.GetRequiredService<ICatalogTourReadModelStore>();

    /// <summary>Gets the Catalog media image store.</summary>
    public IPublicMediaImageStore Images => scope.ServiceProvider.GetRequiredService<IPublicMediaImageStore>();

    /// <summary>Gets the bounded Catalog photo options query.</summary>
    public ICatalogGalleryPhotoOptionsQuery PhotoOptions => scope.ServiceProvider.GetRequiredService<ICatalogGalleryPhotoOptionsQuery>();

    /// <summary>Gets the public Catalog album query.</summary>
    public ICatalogGalleryReadStore PublicGallery => scope.ServiceProvider.GetRequiredService<ICatalogGalleryReadStore>();

    /// <summary>Gets the service that authorizes and queues placements.</summary>
    public CatalogTourPresentationService Writer => scope.ServiceProvider.GetRequiredService<CatalogTourPresentationService>();

    /// <summary>Gets the internal Gallery placement reader registered by Catalog.</summary>
    internal CatalogGalleryProcessStatusReader ProcessStatus => scope.ServiceProvider.GetRequiredService<CatalogGalleryProcessStatusReader>();

    /// <summary>Gets the operations query used by the authenticated Gallery lookup.</summary>
    internal ICatalogGalleryOperationsQuery GalleryOperations => scope.ServiceProvider.GetRequiredService<ICatalogGalleryOperationsQuery>();

    /// <summary>Gets the runner that applies pending Catalog projections.</summary>
    public CatalogProjectionRunner Runner => scope.ServiceProvider.GetRequiredService<CatalogProjectionRunner>();

    /// <summary>Gets the tour projection for replaying a recorded event after outbox retention.</summary>
    public CatalogTourReadModelProjection TourProjection => scope.ServiceProvider.GetRequiredService<CatalogTourReadModelProjection>();

    /// <summary>Gets the local wake handler with its inbox decorator.</summary>
    public IIntegrationEventHandler<CatalogGalleryPlacementQueuedIntegrationEvent> Handler =>
        scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<CatalogGalleryPlacementQueuedIntegrationEvent>>();

    /// <summary>Delivers an Admin tour creation through the Catalog inbox decorator.</summary>
    public ValueTask Deliver(AdminTourCreatedIntegrationEvent integrationEvent, CancellationToken ct) =>
        scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<AdminTourCreatedIntegrationEvent>>().Handle(integrationEvent, ct);

    /// <summary>Acquires a Catalog inbox lease without delivering the wake.</summary>
    public ValueTask<IdempotencyStartResult> StartInboxLease(IdempotencyOperation operation, CancellationToken ct)
    {
        var services = scope.ServiceProvider;
        var inbox = services.GetRequiredKeyedService<IIdempotencyStore>(typeof(CatalogDbContext));
        var clock = services.GetRequiredService<TimeProvider>();
        var timeout = services.GetRequiredService<IOptions<IntegrationEventOptions>>().Value.IdempotencyLockDuration;
        return inbox.TryStart(operation, clock.GetUtcNow(), timeout, ct);
    }

    /// <summary>Captures the inbox state, row version and hash of all stored values for one operation.</summary>
    public Task<string> GetInboxRowVersion(IdempotencyOperation operation, CancellationToken ct) =>
        Database.Database.SqlQuery<string>($"""
            SELECT concat(inbox."State", '/', inbox.xmin::text, '/', encode(sha256(convert_to(to_jsonb(inbox)::text, 'UTF8')), 'hex')) AS "Value"
            FROM messaging.idempotency_keys AS inbox
            WHERE inbox."Scope" = {operation.Scope.Value} AND inbox."Key" = {operation.Key.Value}
            """).SingleAsync(ct);

    /// <summary>Reads durable duplicate observations for the exact Catalog delivery.</summary>
    public Task<IntegrationEventInboxDuplicatePage> ReadInboxDuplicates(Guid eventId, CancellationToken ct) =>
        IntegrationEventInboxDuplicates.ReadPage(Database, IntegrationEventConsumerNames.Catalog, eventId, limit: 1, cursor: null, ct);

    /// <summary>Seeds a draft Catalog tour, optionally with an explicitly authored empty album.</summary>
    public Task<CatalogTour> SeedDraft(bool authoredGallery, CancellationToken ct) =>
        SeedDraft("andes", "Andes", authoredGallery, ct);

    /// <summary>Seeds a distinct draft for a second tour in the same database.</summary>
    public async Task<CatalogTour> SeedDraft(string identifier, string title, bool authoredGallery, CancellationToken ct)
    {
        var tour = CatalogTour.CreateDraft(Guid.CreateVersion7(), identifier, title, Guid.CreateVersion7());
        var stream = CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId);
        await Events.Append(stream, ExpectedStreamRevision.NoStream, tour.GetUncommittedEvents(), ct);
        await Runner.Project(ct);
        if (authoredGallery)
        {
            var gallery = await Writer.ChangeGallery(tour.Id, [], null, 1, canPublish: false, ct);
            gallery.IsSuccess.ShouldBeTrue();
        }

        return tour;
    }

    /// <summary>Reads the actual keyed Catalog inbox in this test scope.</summary>
    public ValueTask<IdempotencyEntry?> ReadInbox(IdempotencyOperation operation, CancellationToken ct) =>
        scope.ServiceProvider.GetRequiredKeyedService<IIdempotencyStore>(typeof(CatalogDbContext)).Get(operation, ct);

    /// <summary>Queues one real photo placement and returns its durable journal identity.</summary>
    public async Task<(Guid IntentId, long Sequence)> QueuePhoto(Guid tourId, CancellationToken ct)
    {
        var filename = $"{tourId:N}.jpg";
        var photo = PublicMediaImageTestFactory.CreateReadyImage(tourId, filename, filename, $"sha256:{tourId:N}", "Fixture", 0, true);
        await Images.Upsert(photo, ct);
        var sequence = (await Writer.QueueGallery(tourId, [photo.Id], photo.Id, 1, 0, canPublish: false, ct)).Value;
        var intent = await Intents.GetIntent(tourId, sequence, ct);
        return (intent.ShouldNotBeNull().Id, sequence);
    }

    /// <summary>Seeds the stream and projection of a tour ready to publish.</summary>
    public async Task<CatalogTour> SeedPublishableDraft(CancellationToken ct)
    {
        var tour = await SeedDraft(authoredGallery: false, ct);
        var update = new CatalogTourPresentationUpdate(tour.Title, tour.Slug, "Ride the Andes.", "", "", "", "");
        var prepared = await Writer.UpdatePresentation(tour.Id, update, 1, ct);
        prepared.IsSuccess.ShouldBeTrue();
        return tour;
    }

    /// <summary>Delivers the real Admin retirement event to Catalog.</summary>
    public ValueTask Retire(CatalogTour tour, CancellationToken ct) =>
        scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<AdminTourRetiredIntegrationEvent>>().Handle(
            new AdminTourRetiredIntegrationEvent(Guid.CreateVersion7(), scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow(),
                tour.AdminTourId, Guid.CreateVersion7()), ct);

    /// <summary>Runs the guarded API upload path with the real journal and EF upload transaction.</summary>
    public async ValueTask<Result<MediaImageUploadIntakeResult>> AcceptUpload(
        Guid tourId, MediaImageUploadIntakeRequest request, IMediaUploadScanner scanner, IObjectStore objects, CancellationToken ct)
    {
        await using var uploadScope = provider.CreateAsyncScope();
        var services = uploadScope.ServiceProvider;
        var guard = services.GetRequiredService<CatalogGalleryMutationGuard>();
        var authorization = await guard.Acquire([tourId], canPublish: false, ct);
        await using var lease = authorization.Value;
        var images = await services.GetRequiredService<IPublicMediaImageStore>().ListByTour(tourId, ct);
        var options = Options.Create(new MediaUploadValidationOptions());
        var intake = new MediaImageUploadIntake(
            new MediaUploadValidator(options.Value),
            scanner,
            objects,
            services.GetRequiredService<IMediaImageUploadStore>(),
            services.GetRequiredService<IIntegrationEventOutbox>(),
            options,
            NullLogger<MediaImageUploadIntake>.Instance,
            services.GetRequiredService<TimeProvider>());
        return await intake.Accept(request with
        {
            TourLinks = [new MediaImageTourLink(tourId, images.Count, images.Count == 0)],
            CanPublishGalleryPlacement = false
        }, ct);
    }

    /// <summary>Advances the clock past an interrupted wake's inbox lease.</summary>
    public DateTimeOffset AdvanceBeyondInboxLease()
    {
        var clock = (FakeTimeProvider)scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var timeout = scope.ServiceProvider.GetRequiredService<IOptions<IntegrationEventOptions>>().Value.IdempotencyLockDuration;
        var instant = clock.CurrentInstant + timeout + TimeSpan.FromSeconds(1);
        clock.CurrentInstant = instant.AddTicks(-(instant.UtcTicks % TimeSpan.TicksPerMicrosecond));
        return clock.CurrentInstant;
    }

    /// <summary>Creates an isolated scenario with the Catalog migrations and event-store schema applied.</summary>
    public static async ValueTask<CatalogGalleryIntentPostgreSqlScenario> Create(string connectionString, CancellationToken ct)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Development });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = connectionString,
            [ClamAvMalwareScannerConfigurationKeys.DisabledConfigurationKey] = bool.TrueString
        });
        builder.AddCatalogInfrastructure(addOutboxRelay: false);
        builder.Services.AddSingleton<TimeProvider>(new FakeTimeProvider(TimeProvider.System.GetUtcNow()));
        var provider = builder.Services.BuildServiceProvider();
        var scenario = new CatalogGalleryIntentPostgreSqlScenario(provider, provider.CreateAsyncScope());
        try
        {
            await scenario.Database.Database.MigrateAsync(ct);
            await scenario.Events.Initialize(ct);
            return scenario;
        }
        catch
        {
            await scenario.DisposeAsync();
            throw;
        }
    }

    /// <summary>Counts durable outbox rows in the scenario database.</summary>
    public Task<long> CountOutbox(CancellationToken ct) =>
        Database.Database.SqlQueryRaw<long>("""SELECT COUNT(*) AS "Value" FROM messaging.outbox_messages""").SingleAsync(ct);

    /// <summary>Reads the actual queued wake by its persistent outbox identity and contract type.</summary>
    public async Task<CatalogGalleryPlacementQueuedIntegrationEvent> GetWake(Guid intentId, CancellationToken ct)
    {
        var eventId = intentId.ToString("D");
        var eventType = CatalogGalleryPlacementQueuedIntegrationEvent.EventType;
        var eventVersion = CatalogGalleryPlacementQueuedIntegrationEvent.EventVersion;
        var payload = await Database.Database.SqlQuery<string>($"""
            SELECT "Payload" AS "Value" FROM messaging.outbox_messages
            WHERE "EventId" = {eventId} AND "EventType" = {eventType} AND "EventVersion" = {eventVersion}
            """).SingleAsync(ct);

        return JsonSerializer.Deserialize(payload, CatalogIntegrationEventJsonContext.Default.CatalogGalleryPlacementQueuedIntegrationEvent)
            ?? throw new InvalidOperationException("The placement wake payload is missing.");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await scope.DisposeAsync();
        await provider.DisposeAsync();
    }
}
