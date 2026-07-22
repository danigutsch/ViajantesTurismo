using System.Collections.Concurrent;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace ViajantesTurismo.ServiceDefaults.Tests;

internal sealed class CollectingLogRecordExporter(ConcurrentQueue<CapturedLogRecord> exportedLogs) : BaseExporter<LogRecord>
{
    public override ExportResult Export(in Batch<LogRecord> batch)
    {
        foreach (var logRecord in batch)
        {
            var scopes = new List<object?>();
            logRecord.ForEachScope(static (scope, state) => state.Add(scope.Scope), scopes);
            exportedLogs.Enqueue(new CapturedLogRecord(
                logRecord.CategoryName,
                logRecord.Body,
                logRecord.FormattedMessage,
                logRecord.Exception,
                logRecord.Attributes is null ? [] : [.. logRecord.Attributes],
                scopes));
        }

        return ExportResult.Success;
    }
}
