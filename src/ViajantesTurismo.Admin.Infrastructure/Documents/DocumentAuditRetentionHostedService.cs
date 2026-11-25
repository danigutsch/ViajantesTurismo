using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedKernel.Mediator;
using SharedKernel.Scheduling;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Purges expired document audit metadata on the currently configured daily schedule.</summary>
internal sealed class DocumentAuditRetentionHostedService(
    IServiceScopeFactory scopeFactory,
    DocumentOperationTelemetry telemetry,
    ILogger<DocumentAuditRetentionHostedService> logger)
    : PollingBackgroundService(logger, DocumentAuditRetentionServiceName, TimeSpan.FromDays(1))
{
    private const string DocumentAuditRetentionServiceName = "admin-document-audit-retention";
    private const string SystemActorId = "system:admin-document-audit-retention";

    /// <summary>Runs one purge batch and returns the number of removed audit records.</summary>
    internal ValueTask<int> RunBatch(CancellationToken ct) => ExecuteBatch(ct);

    protected override ValueTask<int> ExecuteBatch(CancellationToken ct)
    {
        var correlationId = Guid.CreateVersion7();
        return telemetry.Record(DocumentOperation.PurgeAudits, correlationId, () => PurgeBatch(correlationId, ct), null, ct);
    }

    private async ValueTask<int> PurgeBatch(Guid correlationId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var auditContext = DocumentAuditContext.Create(SystemActorId, correlationId.ToString("N")).Value;
        var counts = await sender.Send(new PurgeExpiredDocumentAuditsCommand(auditContext), ct).ConfigureAwait(false);
        if (counts.Held > 0)
        {
            DocumentRetentionLog.SkippedHeld(logger, DocumentAuditRetentionServiceName, counts.Held);
        }

        return counts.Purged;
    }
}
