using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedKernel.Mediator;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Drains application-owned document retention batches within one scheduled execution.</summary>
internal sealed class DocumentMaintenanceDrain(
    IServiceScopeFactory scopeFactory,
    DocumentOperationTelemetry telemetry,
    ILogger<DocumentMaintenanceDrain> logger)
{
    private const string AuditActor = "system:admin-document-audit-retention";

    /// <summary>Binds one allowlisted job to the scheduler's batch delegate.</summary>
    internal Func<CancellationToken, ValueTask<int>> Bind(DocumentMaintenanceJob job) => ct => RunBatch(job, ct);

    /// <summary>Runs one retention batch and returns the number of removed entries.</summary>
    internal ValueTask<int> RunBatch(DocumentMaintenanceJob job, CancellationToken ct)
    {
        var correlationId = Guid.CreateVersion7();
        return job switch
        {
            DocumentMaintenanceJob.Drafts =>
                telemetry.Record(DocumentOperation.PurgeDrafts, correlationId, () => PurgeDrafts(ct), null, ct),
            DocumentMaintenanceJob.Audit =>
                telemetry.Record(DocumentOperation.PurgeAudits, correlationId, () => PurgeAudit(correlationId, ct), null, ct),
            DocumentMaintenanceJob.Idempotency =>
                telemetry.Record(DocumentOperation.PurgeIdempotency, correlationId, () => PurgeIdempotency(ct), null, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(job), job, null),
        };
    }

    private async ValueTask<int> PurgeDrafts(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var counts = await sender.Send(new PurgeExpiredDraftsCommand(), ct).ConfigureAwait(false);
        LogHeld(DocumentMaintenanceJob.Drafts, counts.Held);
        return counts.Purged;
    }

    private async ValueTask<int> PurgeAudit(Guid correlationId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var audit = DocumentAuditContext.Create(AuditActor, correlationId.ToString("N")).Value;
        var counts = await sender.Send(new PurgeExpiredDocumentAuditsCommand(audit), ct).ConfigureAwait(false);
        LogHeld(DocumentMaintenanceJob.Audit, counts.Held);
        return counts.Purged;
    }

    private async ValueTask<int> PurgeIdempotency(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var counts = await sender.Send(new PurgeExpiredDocumentIdempotencyEntriesCommand(), ct).ConfigureAwait(false);
        LogHeld(DocumentMaintenanceJob.Idempotency, counts.Held);
        if (counts.Unattributed > 0)
        {
            var jobName = DocumentMaintenanceSchedule.GetJobName(DocumentMaintenanceJob.Idempotency);
            DocumentRetentionLog.KeptUnattributed(logger, jobName, counts.Unattributed);
        }

        return counts.Purged;
    }

    private void LogHeld(DocumentMaintenanceJob job, int held)
    {
        if (held > 0)
        {
            var jobName = DocumentMaintenanceSchedule.GetJobName(job);
            DocumentRetentionLog.SkippedHeld(logger, jobName, held);
        }
    }
}
