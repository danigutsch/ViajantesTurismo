using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure.Documents;

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

    public FakeDocumentStore DocumentStore { get; }

    public static DocumentDraftRetentionHostedServiceFixture Create(DateTimeOffset now, DocumentDraft draft)
    {
        var store = new FakeDocumentStore();
        store.Documents.Add(draft.Id, draft);
        var services = new ServiceCollection();
        services.AddSingleton<IDocumentStore>(store);
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(now));
        services.AddScoped<PurgeExpiredDraftsCommandHandler>();
        var provider = services.BuildServiceProvider();

        return new DocumentDraftRetentionHostedServiceFixture(provider, store);
    }

    public ValueTask<int> RunBatch(CancellationToken ct) => hostedService.RunBatch(ct);

    public void Dispose()
    {
        hostedService.Dispose();
        provider.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        hostedService.Dispose();
        await provider.DisposeAsync();
    }
}
