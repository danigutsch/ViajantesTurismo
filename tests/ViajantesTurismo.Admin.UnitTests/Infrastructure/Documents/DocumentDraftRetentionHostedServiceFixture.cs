using System.Diagnostics.Metrics;
using PandaBear.DispositionHold;
using PandaBear.Mediator;
using PandaBear.Scheduling;
using RecordedLog = PandaBear.Testing.Logging.RecordedLog;
using RecordingLoggerProvider = PandaBear.Testing.Logging.RecordingLoggerProvider;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Documents.Holds;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Admin.Testing;
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
        Logs = provider.GetRequiredService<RecordingLoggerProvider>();
        Metrics = new DocumentOperationMetricCapture(provider.GetRequiredService<IMeterFactory>());
        hostedService = new DocumentDraftRetentionHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<DocumentOperationTelemetry>(),
            provider.GetRequiredService<ILogger<DocumentDraftRetentionHostedService>>());
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
    public static DocumentDraftRetentionHostedServiceFixture Create(DateTimeOffset now, DocumentDraft draft, Exception? purgeFailure = null)
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
        services.AddSharedKernelMediator();
        var provider = services.BuildServiceProvider();

        return new DocumentDraftRetentionHostedServiceFixture(provider, store);
    }

    /// <summary>
    /// Runs one purge batch on the hosted service and returns the purged count.
    /// </summary>
    public ValueTask<int> RunBatch(CancellationToken ct) => hostedService.RunBatch(ct);

    /// <summary>Starts and stops the polling worker around one observed scheduled failure.</summary>
    public async Task<RecordedLog> ObserveScheduledFailure(CancellationToken ct)
    {
        var observedFailure = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, recorder) =>
        {
            if (instrument.Name == SchedulingTelemetry.MetricPollingFailures)
            {
                recorder.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            foreach (var tag in tags)
            {
                if (tag.Key == SchedulingTelemetry.TagServiceName && Equals(tag.Value, "admin-document-draft-retention"))
                {
                    observedFailure.TrySetResult();
                }
            }
        });
        listener.Start();

        await hostedService.StartAsync(ct);
        try
        {
            await observedFailure.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
        }
        finally
        {
            await hostedService.StopAsync(ct);
        }

        return Logs.Records.Single(record => record.EventId.Name == "LogPollingFailure");
    }

    /// <summary>
    /// Disposes the hosted service and the service provider.
    /// </summary>
    public void Dispose()
    {
        hostedService.Dispose();
        Metrics.Dispose();
        provider.Dispose();
    }

    /// <summary>
    /// Asynchronously disposes the hosted service and the service provider.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        hostedService.Dispose();
        Metrics.Dispose();
        await provider.DisposeAsync();
    }
}
