using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Branding;

/// <summary>Observes the trace ID on the real transport retry log without collecting the log body or attributes.</summary>
internal sealed class BrandingWorkerRetryTraceExporter(TaskCompletionSource<ActivityTraceId> trace) : BaseExporter<LogRecord>
{
    /// <inheritdoc />
    public override ExportResult Export(in Batch<LogRecord> batch)
    {
        foreach (var record in batch)
        {
            if (record.EventId.Name == "LogConsumeFailed"
                && record.CategoryName?.StartsWith(
                    "PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore.PostgreSqlIntegrationEventTransportConsumer",
                    StringComparison.Ordinal) == true
                && record.TraceId != default)
            {
                trace.TrySetResult(record.TraceId);
            }
        }

        return ExportResult.Success;
    }
}
