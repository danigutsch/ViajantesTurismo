using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.Mediator;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Application.Documents.Holds;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure.Documents;

/// <summary>Runs one idempotency retention batch through the Admin API's generated mediator.</summary>
internal static class DocumentIdempotencyRetentionBatch
{
    /// <summary>Dispatches the batch with a fixed clock and a test-owned store.</summary>
    public static async ValueTask<int> Run(DateTimeOffset now, FakeDocumentIdempotencyRetentionStore store, CancellationToken ct)
    {
        var clock = new FakeTimeProvider(now);
        var services = new ServiceCollection();
        services.AddMetrics();
        services.AddLogging();
        services.AddSingleton<DocumentOperationTelemetry>();
        services.AddSingleton<IDocumentIdempotencyRetentionStore>(store);
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton(new DocumentHoldGate(new FakeDispositionHoldGuard(), new InMemoryDispositionHoldStore(), clock));
        services.AddSingleton(DocumentPurgeTestData.CreateRunner(new FakeDocumentPurgeEvidenceStore(), clock));
        services.AddMediator();

        await using var provider = services.BuildServiceProvider();
        using var job = new DocumentIdempotencyRetentionHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<DocumentOperationTelemetry>(),
            NullLogger<DocumentIdempotencyRetentionHostedService>.Instance);
        return await job.RunBatch(ct);
    }
}
