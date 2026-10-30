using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure.Documents;

/// <summary>
/// Wires the audit retention hosted service to a fake audit store and fixed clock for batch tests.
/// </summary>
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

    /// <summary>
    /// Gets the fake audit store the hosted service purges from.
    /// </summary>
    public FakeDocumentAuditStore AuditStore
    {
        get;
    }

    /// <summary>
    /// Creates a fixture whose fake store reports <paramref name="purgedCount"/> purged audits at <paramref name="now"/>.
    /// </summary>
    public static DocumentAuditRetentionHostedServiceFixture Create(DateTimeOffset now, int purgedCount)
    {
        var auditStore = new FakeDocumentAuditStore { PurgedCount = purgedCount };
        var services = new ServiceCollection();
        services.AddSingleton<IDocumentAuditStore>(auditStore);
        services.AddSingleton<IUnitOfWork>(new FakeUnitOfWork());
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(now));
        services.AddScoped<PurgeExpiredDocumentAuditsCommandHandler>();
        var provider = services.BuildServiceProvider();

        return new DocumentAuditRetentionHostedServiceFixture(provider, auditStore);
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
