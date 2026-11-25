using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedKernel.Mediator;
using SharedKernel.Scheduling;
using ViajantesTurismo.Admin.Application.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Purges expired unfinalized document drafts on a daily schedule.</summary>
/// <param name="scopeFactory">Creates the scope that dispatches the purge command.</param>
/// <param name="telemetry">Records each draft purge outcome.</param>
/// <param name="logger">The logger for the polling service.</param>
internal sealed class DocumentDraftRetentionHostedService(
    IServiceScopeFactory scopeFactory,
    DocumentOperationTelemetry telemetry,
    ILogger<DocumentDraftRetentionHostedService> logger)
    : PollingBackgroundService(logger, DocumentDraftRetentionServiceName, TimeSpan.FromDays(1))
{
    private const string DocumentDraftRetentionServiceName = "admin-document-draft-retention";

    /// <summary>Runs one purge batch and returns the number of removed drafts.</summary>
    internal ValueTask<int> RunBatch(CancellationToken ct) => ExecuteBatch(ct);

    protected override ValueTask<int> ExecuteBatch(CancellationToken ct)
    {
        return telemetry.Record(DocumentOperation.PurgeDrafts, Guid.CreateVersion7(), () => PurgeBatch(ct), null, ct);
    }

    private async ValueTask<int> PurgeBatch(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var counts = await sender.Send(new PurgeExpiredDraftsCommand(), ct).ConfigureAwait(false);
        if (counts.Held > 0)
        {
            DocumentRetentionLog.SkippedHeld(logger, DocumentDraftRetentionServiceName, counts.Held);
        }

        return counts.Purged;
    }
}
