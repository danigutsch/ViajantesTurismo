using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PandaBear.EventSourcing;
using PandaBear.EventSourcing.Npgsql;
using PandaBear.MalwareScanning.ClamAv;
using PandaBear.Messaging.IntegrationEvents;
using PandaBear.ObjectStorage;
using PandaBear.Results;
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

    /// <summary>Gets the runner that applies pending Catalog projections.</summary>
    public CatalogProjectionRunner Runner => scope.ServiceProvider.GetRequiredService<CatalogProjectionRunner>();

    /// <summary>Gets the tour projection for replaying a recorded event after outbox retention.</summary>
    public CatalogTourReadModelProjection TourProjection => scope.ServiceProvider.GetRequiredService<CatalogTourReadModelProjection>();

    /// <summary>Gets the local wake handler with its inbox decorator.</summary>
    public IIntegrationEventHandler<CatalogGalleryPlacementQueuedIntegrationEvent> Handler =>
        scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<CatalogGalleryPlacementQueuedIntegrationEvent>>();

    /// <summary>Seeds a draft Catalog tour, optionally with an explicitly authored empty album.</summary>
    public async Task<CatalogTour> SeedDraft(bool authoredGallery, CancellationToken ct)
    {
        var tour = CatalogTour.CreateDraft(Guid.CreateVersion7(), "andes", "Andes", Guid.CreateVersion7());
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
    public void AdvanceBeyondInboxLease()
    {
        var clock = (FakeTimeProvider)scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var timeout = scope.ServiceProvider.GetRequiredService<IOptions<IntegrationEventOptions>>().Value.IdempotencyLockDuration;
        clock.CurrentInstant += timeout + TimeSpan.FromSeconds(1);
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
