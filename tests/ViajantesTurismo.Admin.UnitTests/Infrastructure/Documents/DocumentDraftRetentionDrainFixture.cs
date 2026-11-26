using System.Diagnostics.Metrics;
using SharedKernel.DispositionHold;
using SharedKernel.Mediator;
using SharedKernel.Scheduling;
using RecordingLoggerProvider = SharedKernel.Testing.Logging.RecordingLoggerProvider;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Documents.Holds;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Admin.Testing;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure.Documents;

/// <summary>
/// Wires the draft retention drain to a fake document store and fixed clock for batch tests.
/// </summary>
internal sealed class DocumentDraftRetentionDrainFixture : IAsyncDisposable, IDisposable
{
    private readonly ServiceProvider provider;
    private readonly DocumentMaintenanceDrain drain;

    private DocumentDraftRetentionDrainFixture(ServiceProvider provider, FakeDocumentStore documentStore)
    {
        this.provider = provider;
        DocumentStore = documentStore;
        Logs = provider.GetRequiredService<RecordingLoggerProvider>();
        Metrics = new DocumentOperationMetricCapture(provider.GetRequiredService<IMeterFactory>());
        drain = new DocumentMaintenanceDrain(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<DocumentOperationTelemetry>(),
            provider.GetRequiredService<ILogger<DocumentMaintenanceDrain>>());
    }

    /// <summary>
    /// Gets the fake document store holding the seeded draft.
    /// </summary>
    public FakeDocumentStore DocumentStore
    {
        get;
    }

    /// <summary>Gets metrics from the worker's meter factory.</summary>
    public DocumentOperationMetricCapture Metrics
    {
        get;
    }

    /// <summary>Gets the log output captured by the fixture.</summary>
    public RecordingLoggerProvider Logs
    {
        get;
    }

    /// <summary>
    /// Creates a fixture whose fake store contains <paramref name="draft"/> and whose clock is fixed at <paramref name="now"/>.
    /// </summary>
    public static DocumentDraftRetentionDrainFixture Create(DateTimeOffset now, DocumentDraft draft, Exception? purgeFailure = null)
    {
        var store = new FakeDocumentStore(purgeFailure);
        store.Documents.Add(draft.Id, draft);
        var services = new ServiceCollection();
        services.AddMetrics();
        services.AddSingleton<DocumentOperationTelemetry>();
        services.AddSingleton<RecordingLoggerProvider>();
        services.AddSingleton<ILoggerProvider>(sp => sp.GetRequiredService<RecordingLoggerProvider>());
        services.AddLogging();
        services.AddSingleton<IDocumentStore>(store);
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(now));
        services.AddSingleton<IReadCommittedTransaction>(new FakeReadCommittedTransaction());
        services.AddSingleton<IDispositionHoldGuard>(new FakeDispositionHoldGuard());
        services.AddSingleton<IDispositionHoldStore>(new InMemoryDispositionHoldStore());
        services.AddScoped<DocumentHoldGate>();
        services.AddSingleton<IDocumentPurgeEvidenceStore>(new FakeDocumentPurgeEvidenceStore());
        services.AddSingleton<IUnitOfWork>(new FakeUnitOfWork());
        services.AddScoped<DocumentPurgeRunner>();
        services.AddMediator();
        var provider = services.BuildServiceProvider();

        return new DocumentDraftRetentionDrainFixture(provider, store);
    }

    /// <summary>
    /// Runs one purge batch on the drain and returns the purged count.
    /// </summary>
    public ValueTask<int> RunBatch(CancellationToken ct) => drain.RunBatch(DocumentMaintenanceJob.Drafts, ct);

    /// <summary>Runs one complete drain cycle.</summary>
    public async ValueTask RunDrain(CancellationToken ct)
    {
        using var observation = new BatchDrain(DocumentMaintenanceSchedule.GetJobName(DocumentMaintenanceJob.Drafts));
        await observation.Run(RunBatch, ct);
    }


    /// <summary>
    /// Disposes the drain and the service provider.
    /// </summary>
    public void Dispose()
    {
        Metrics.Dispose();
        provider.Dispose();
    }

    /// <summary>
    /// Asynchronously disposes the drain and the service provider.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        Metrics.Dispose();
        await provider.DisposeAsync();
    }
}
