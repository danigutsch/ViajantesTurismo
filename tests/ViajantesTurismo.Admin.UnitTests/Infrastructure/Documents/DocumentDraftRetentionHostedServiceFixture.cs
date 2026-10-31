using PandaBear.DispositionHold;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Documents.Holds;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure.Documents;

/// <summary>
/// Wires the draft retention hosted service to a fake document store and fixed clock for batch tests.
/// </summary>
internal sealed class DocumentDraftRetentionHostedServiceFixture : IAsyncDisposable, IDisposable
{
    private readonly ServiceProvider provider;
    private readonly DocumentDraftRetentionHostedService hostedService;

    private DocumentDraftRetentionHostedServiceFixture(ServiceProvider provider, FakeDocumentStore documentStore)
    {
        this.provider = provider;
        DocumentStore = documentStore;
        hostedService = new DocumentDraftRetentionHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<DocumentDraftRetentionHostedService>.Instance);
    }

    /// <summary>
    /// Gets the fake document store holding the seeded draft.
    /// </summary>
    public FakeDocumentStore DocumentStore
    {
        get;
    }

    /// <summary>
    /// Creates a fixture whose fake store contains <paramref name="draft"/> and whose clock is fixed at <paramref name="now"/>.
    /// </summary>
    public static DocumentDraftRetentionHostedServiceFixture Create(DateTimeOffset now, DocumentDraft draft)
    {
        var store = new FakeDocumentStore();
        store.Documents.Add(draft.Id, draft);
        var services = new ServiceCollection();
        services.AddSingleton<IDocumentStore>(store);
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(now));
        services.AddSingleton<IDocumentDispositionTransaction>(new FakeDocumentDispositionTransaction());
        services.AddSingleton<IDispositionHoldGuard>(new FakeDispositionHoldGuard());
        services.AddSingleton<IDispositionHoldStore>(new InMemoryDispositionHoldStore());
        services.AddScoped<DocumentHoldGate>();
        services.AddSingleton<IDocumentPurgeEvidenceStore>(new FakeDocumentPurgeEvidenceStore());
        services.AddSingleton<IUnitOfWork>(new FakeUnitOfWork());
        services.AddScoped<DocumentPurgeRunner>();
        services.AddScoped<PurgeExpiredDraftsCommandHandler>();
        var provider = services.BuildServiceProvider();

        return new DocumentDraftRetentionHostedServiceFixture(provider, store);
    }

    /// <summary>
    /// Runs one purge batch on the hosted service and returns the purged count.
    /// </summary>
    public ValueTask<int> RunBatch(CancellationToken ct) => hostedService.RunBatch(ct);

    /// <summary>
    /// Disposes the hosted service and the service provider.
    /// </summary>
    public void Dispose()
    {
        hostedService.Dispose();
        provider.Dispose();
    }

    /// <summary>
    /// Asynchronously disposes the hosted service and the service provider.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        hostedService.Dispose();
        await provider.DisposeAsync();
    }
}
