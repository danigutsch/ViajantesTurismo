using SharedKernel.DispositionHold;
using SharedKernel.Mediator;
using ViajantesTurismo.Admin.Application.Documents.Holds;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure.Documents;

/// <summary>
/// Wires the audit retention drain to a fake audit store and fixed clock for batch tests.
/// </summary>
internal sealed class DocumentAuditRetentionDrainFixture : IAsyncDisposable, IDisposable
{
    private readonly ServiceProvider provider;
    private readonly DocumentMaintenanceDrain drain;

    private DocumentAuditRetentionDrainFixture(ServiceProvider provider, FakeDocumentAuditStore auditStore)
    {
        this.provider = provider;
        AuditStore = auditStore;
        drain = new DocumentMaintenanceDrain(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<DocumentOperationTelemetry>(),
            NullLogger<DocumentMaintenanceDrain>.Instance);
    }

    /// <summary>
    /// Gets the fake audit store the drain purges from.
    /// </summary>
    public FakeDocumentAuditStore AuditStore
    {
        get;
    }

    /// <summary>
    /// Creates a fixture whose fake store reports <paramref name="purgedCount"/> purged audits at <paramref name="now"/>.
    /// </summary>
    public static DocumentAuditRetentionDrainFixture Create(DateTimeOffset now, int purgedCount)
    {
        var auditStore = new FakeDocumentAuditStore { PurgedCount = purgedCount };
        var services = new ServiceCollection();
        services.AddMetrics();
        services.AddLogging();
        services.AddSingleton<DocumentOperationTelemetry>();
        services.AddSingleton<IDocumentAuditStore>(auditStore);
        services.AddSingleton<IUnitOfWork>(new FakeUnitOfWork());
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(now));
        services.AddSingleton<IReadCommittedTransaction>(new FakeReadCommittedTransaction());
        services.AddSingleton<IDispositionHoldGuard>(new FakeDispositionHoldGuard());
        services.AddSingleton<IDispositionHoldStore>(new InMemoryDispositionHoldStore());
        services.AddScoped<DocumentHoldGate>();
        services.AddMediator();
        var provider = services.BuildServiceProvider();

        return new DocumentAuditRetentionDrainFixture(provider, auditStore);
    }

    /// <summary>
    /// Runs one purge batch on the drain and returns the purged count.
    /// </summary>
    public ValueTask<int> RunBatch(CancellationToken ct) => drain.RunBatch(DocumentMaintenanceJob.Audit, ct);

    /// <summary>
    /// Disposes the drain and the service provider.
    /// </summary>
    public void Dispose()
    {
        provider.Dispose();
    }

    /// <summary>
    /// Asynchronously disposes the drain and the service provider.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await provider.DisposeAsync();
    }
}
