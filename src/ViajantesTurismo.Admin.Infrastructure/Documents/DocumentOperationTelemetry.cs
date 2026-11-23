using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Records Admin document operations using an allowlist of operational metadata.</summary>
public sealed partial class DocumentOperationTelemetry : IDisposable
{
    private const string SourceName = "ViajantesTurismo.Admin.Documents";
    private const string ActivityName = "admin.document.operation";
    private const string OperationTag = "document.operation";
    private const string OutcomeTag = "document.outcome";

    private readonly Meter meter;
    private readonly Counter<long> operations;
    private readonly ILogger<DocumentOperationTelemetry> logger;

    /// <summary>Uses the host's meter factory and logger to publish document operation telemetry.</summary>
    public DocumentOperationTelemetry(IMeterFactory meterFactory, ILogger<DocumentOperationTelemetry> logger)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);
        ArgumentNullException.ThrowIfNull(logger);
        this.logger = logger;
        meter = meterFactory.Create(SourceName);
        operations = meter.CreateCounter<long>(
            "admin.document.operation", description: "Completed document operation attempts by operation and outcome.");
    }

    /// <summary>Gets the host's document operation trace source.</summary>
    internal ActivitySource Source { get; } = new(SourceName);

    /// <summary>
    /// Measures an operation and preserves its response, cancellation, or exception. Use a server-created
    /// correlation identifier and a classifier that inspects status only.
    /// </summary>
    public async ValueTask<T> Record<T>(
        DocumentOperation operation,
        Guid correlationId,
        Func<ValueTask<T>> execute,
        Func<T, DocumentOperationOutcome>? classify,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(execute);
        var operationName = GetOperationName(operation);
        var correlation = correlationId.ToString("N");
        using var activity = Source.StartActivity(ActivityName, ActivityKind.Internal);
        activity?.SetTag(OperationTag, operationName);
        activity?.SetTag("document.correlation", correlation);
        var outcome = DocumentOperationOutcome.Failed;
        try
        {
            var result = await execute().ConfigureAwait(false);
            outcome = classify?.Invoke(result) ?? DocumentOperationOutcome.Succeeded;
            return result;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            outcome = DocumentOperationOutcome.Canceled;
            throw;
        }
        finally
        {
            var outcomeName = GetOutcomeName(outcome);
            activity?.SetTag(OutcomeTag, outcomeName);
            if (outcome == DocumentOperationOutcome.Failed)
            {
                activity?.SetStatus(ActivityStatusCode.Error);
            }

            operations.Add(1, new(OperationTag, operationName), new(OutcomeTag, outcomeName));
            var level = outcome == DocumentOperationOutcome.Failed ? LogLevel.Error : LogLevel.Information;
            LogCompletion(logger, level, operationName, outcomeName, correlation);
        }
    }

    /// <summary>Disposes the document operation meter and activity source.</summary>
    public void Dispose()
    {
        Source.Dispose();
        meter.Dispose();
    }

    private static string GetOperationName(DocumentOperation operation) => operation switch
    {
        DocumentOperation.Generate => "generate",
        DocumentOperation.Preview => "preview",
        DocumentOperation.Review => "review",
        DocumentOperation.RequestChanges => "request_changes",
        DocumentOperation.AcknowledgeChanges => "acknowledge_changes",
        DocumentOperation.UpdateField => "update_field",
        DocumentOperation.Approve => "approve",
        DocumentOperation.Finalize => "finalize",
        DocumentOperation.Regenerate => "regenerate",
        DocumentOperation.WithdrawApproval => "withdraw_approval",
        DocumentOperation.Void => "void",
        DocumentOperation.Download => "download",
        DocumentOperation.PurgeDrafts => "purge_drafts",
        DocumentOperation.PurgeAudits => "purge_audits",
        DocumentOperation.PurgeIdempotency => "purge_idempotency",
        _ => throw new ArgumentOutOfRangeException(nameof(operation)),
    };

    private static string GetOutcomeName(DocumentOperationOutcome outcome) => outcome switch
    {
        DocumentOperationOutcome.Succeeded => "succeeded",
        DocumentOperationOutcome.Rejected => "rejected",
        DocumentOperationOutcome.Failed => "failed",
        DocumentOperationOutcome.Canceled => "canceled",
        _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
    };

    [LoggerMessage(EventId = 10, EventName = "DocumentOperationCompleted",
        Message = "Document operation {Operation} finished with {Outcome}. Correlation {Correlation}.")]
    private static partial void LogCompletion(ILogger logger, LogLevel level, string operation, string outcome, string correlation);
}
