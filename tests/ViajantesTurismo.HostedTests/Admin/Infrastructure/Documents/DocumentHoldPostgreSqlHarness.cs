using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PandaBear.Domain.EntityFrameworkCore;
using PandaBear.DispositionHold;
using PandaBear.Idempotency.EntityFrameworkCore;
using PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Application.Documents.Holds;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.Infrastructure.DispositionHolds;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Documents;

/// <summary>Composes the real hold store, guard, transaction and purge handlers over a migrated PostgreSQL database.</summary>
internal sealed class DocumentHoldPostgreSqlHarness : IAsyncDisposable
{
    private readonly ServiceProvider provider;

    private DocumentHoldPostgreSqlHarness(ServiceProvider provider)
    {
        this.provider = provider;
    }

    /// <summary>Creates the harness over the given database; the schema must already be migrated. Retry must be off to hold a transaction open. A given failure replaces the real hold guard with one that always throws it.</summary>
    public static DocumentHoldPostgreSqlHarness Create(string connectionString, bool enableRetryOnFailure = true, Exception? holdGuardFailure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddDomainEventDispatch<AdminWriteDbContext>();
        services.AddIdempotencyStore<AdminWriteDbContext>();
        services.AddAdminDispositionHolds();
        if (holdGuardFailure is not null)
        {
            services.AddSingleton<IDispositionHoldGuard>(new FakeDispositionHoldGuard { Failure = holdGuardFailure });
        }

        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddPostgreSqlIntegrationEventTransportProducer<AdminWriteDbContext>(IntegrationEventConsumerNames.Catalog);
        services.AddDbContext<AdminWriteDbContext>(options => options.UseNpgsql(connectionString, npgsql =>
        {
            if (enableRetryOnFailure)
            {
                npgsql.EnableRetryOnFailure();
            }
        }));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AdminWriteDbContext>());
        services.AddScoped<IDocumentStore, DocumentStore>();
        services.AddScoped<IDocumentAuditStore, DocumentAuditStore>();
        services.AddScoped<IDocumentIdempotencyRetentionStore, DocumentIdempotencyRetentionStore>();
        services.AddScoped<IDocumentHoldQuery, DocumentHoldQuery>();
        services.AddScoped<IDocumentDispositionTransaction, DocumentDispositionTransaction>();
        services.AddScoped<DocumentHoldGate>();
        services.AddScoped<DocumentAuditWriter>();
        services.AddScoped<PlaceDocumentHoldCommandHandler>();
        services.AddScoped<ReleaseDocumentHoldCommandHandler>();
        services.AddScoped<ListDocumentHoldsQueryHandler>();
        services.AddScoped<IDocumentPurgeEvidenceStore, DocumentPurgeEvidenceStore>();
        services.AddScoped<DocumentPurgeRunner>();
        services.AddScoped<PurgeExpiredDraftsCommandHandler>();
        services.AddScoped<PurgeExpiredDocumentIdempotencyEntriesCommandHandler>();
        services.AddScoped<PurgeExpiredDocumentAuditsCommandHandler>();
        return new DocumentHoldPostgreSqlHarness(services.BuildServiceProvider());
    }

    private static DocumentAuditContext AuditContext(string actorId) => DocumentAuditContext.Create(actorId, Guid.CreateVersion7().ToString("N")).Value;

    /// <summary>Places a hold on the lineage of the document as a synthetic actor and returns its identifier.</summary>
    public async Task<Guid> Place(Guid documentId, CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        var now = TimeProvider.System.GetUtcNow();
        var command = new PlaceDocumentHoldCommand(documentId, HoldBasis.LegalClaim, "purpose.claim-001", "authority:case-0001", now.AddDays(90), null, AuditContext("actor-0001"));
        var result = await scope.ServiceProvider.GetRequiredService<PlaceDocumentHoldCommandHandler>().Handle(command, ct);
        result.IsSuccess.ShouldBeTrue();
        return result.Value.Id;
    }

    /// <summary>Releases a hold as a synthetic actor.</summary>
    public async Task Release(Guid documentId, Guid holdId, CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        var command = new ReleaseDocumentHoldCommand(documentId, holdId, "reason.case-closed", AuditContext("actor-0002"));
        var result = await scope.ServiceProvider.GetRequiredService<ReleaseDocumentHoldCommandHandler>().Handle(command, ct);
        result.IsSuccess.ShouldBeTrue();
    }

    /// <summary>Releases a hold as a synthetic actor and returns the outcome without asserting success.</summary>
    public async Task<PandaBear.Results.Result<Hold>> TryRelease(Guid documentId, Guid holdId, CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        var command = new ReleaseDocumentHoldCommand(documentId, holdId, "reason.case-closed", AuditContext("actor-0003"));
        return await scope.ServiceProvider.GetRequiredService<ReleaseDocumentHoldCommandHandler>().Handle(command, ct);
    }

    /// <summary>Saves audit records through the unit of work, so each is stamped with its lineage.</summary>
    public async Task SaveAudit(DocumentAuditRecord record, CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IDocumentAuditStore>().Add(record);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveEntities(ct);
    }

    /// <summary>Runs the draft purge handler at the given time.</summary>
    public async Task<DocumentPurgeCounts> PurgeDrafts(CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<PurgeExpiredDraftsCommandHandler>().Handle(new PurgeExpiredDraftsCommand(), ct);
    }

    /// <summary>Runs the idempotency purge handler.</summary>
    public async Task<DocumentPurgeCounts> PurgeIdempotencyEntries(CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<PurgeExpiredDocumentIdempotencyEntriesCommandHandler>().Handle(ct);
    }

    /// <summary>Runs the audit purge handler.</summary>
    public async Task<DocumentPurgeCounts> PurgeAudits(CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        var context = DocumentAuditContext.Create("system:test-purge", Guid.CreateVersion7().ToString("N")).Value;
        return await scope.ServiceProvider.GetRequiredService<PurgeExpiredDocumentAuditsCommandHandler>().Handle(new PurgeExpiredDocumentAuditsCommand(context), ct);
    }

    /// <summary>Runs the idempotency purge in the disposition transaction with one hold check.</summary>
    public async Task<DocumentPurgeCounts> PurgeIdempotency(DateTimeOffset cutoff, CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        var gate = scope.ServiceProvider.GetRequiredService<DocumentHoldGate>();
        var store = scope.ServiceProvider.GetRequiredService<IDocumentIdempotencyRetentionStore>();
        return await scope.ServiceProvider.GetRequiredService<IDocumentDispositionTransaction>()
            .Execute([], () => store.PurgeExpiredEntries(cutoff, gate.FindHeldLineages, ct), ct);
    }

    /// <summary>Asks the gate about a lineage outside any transaction.</summary>
    public async Task<IReadOnlySet<Guid>> FindHeldLineagesWithoutTransaction(Guid lineageId, CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<DocumentHoldGate>().FindHeldLineages([lineageId], ct);
    }

    /// <summary>Places a hold in an open transaction and keeps it uncommitted until the returned action commits it.</summary>
    public async Task<Func<Task>> PlaceWithoutCommitting(Guid lineageId, CancellationToken ct)
    {
        var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminWriteDbContext>();
        var transaction = await context.Database.BeginTransactionAsync(ct);
        var now = TimeProvider.System.GetUtcNow();
        var hold = Hold.Place(DocumentHoldScopes.ForLineage(lineageId), HoldBasis.LegalClaim, "purpose.claim-001", "authority:case-0001", "actor-0001", now, now, now.AddDays(90));
        await scope.ServiceProvider.GetRequiredService<IDispositionHoldStore>().Add(hold, ct);
        await context.SaveChangesAsync(ct);
        return async () =>
        {
            await transaction.CommitAsync(ct);
            await transaction.DisposeAsync();
            await scope.DisposeAsync();
        };
    }

    /// <summary>Runs one guard check over the lineage inside the disposition transaction and returns the held lineages.</summary>
    public async Task<IReadOnlySet<Guid>> CheckInTransaction(Guid lineageId, CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        var gate = scope.ServiceProvider.GetRequiredService<DocumentHoldGate>();
        return await scope.ServiceProvider.GetRequiredService<IDocumentDispositionTransaction>()
            .Execute([], () => gate.FindHeldLineages([lineageId], ct), ct);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await provider.DisposeAsync();
}
