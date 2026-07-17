using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure.Documents;

internal sealed class DocumentAuditRetentionHostedServiceFixture : IAsyncDisposable, IDisposable
{
    private readonly ServiceProvider provider;
    private readonly DocumentAuditRetentionHostedService hostedService;

    private DocumentAuditRetentionHostedServiceFixture(ServiceProvider provider, FakeDocumentAuditStore auditStore)
    {
        this.provider = provider;
        AuditStore = auditStore;
        hostedService = new DocumentAuditRetentionHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<DocumentAuditRetentionHostedService>.Instance);
    }

    public FakeDocumentAuditStore AuditStore { get; }

    public static DocumentAuditRetentionHostedServiceFixture Create(DateTimeOffset now, int purgedCount)
    {
        var auditStore = new FakeDocumentAuditStore { PurgedCount = purgedCount };
        var services = new ServiceCollection();
        services.AddSingleton<IDocumentAuditStore>(auditStore);
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(now));
        services.AddScoped<PurgeExpiredDocumentAuditsCommandHandler>();
        var provider = services.BuildServiceProvider();

        return new DocumentAuditRetentionHostedServiceFixture(provider, auditStore);
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
