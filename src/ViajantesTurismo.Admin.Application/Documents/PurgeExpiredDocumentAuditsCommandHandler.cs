using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Purges document audit records only after their approved retention period.</summary>
public sealed class PurgeExpiredDocumentAuditsCommandHandler(
    IDocumentAuditStore auditStore,
    TimeProvider timeProvider)
{
    /// <summary>Removes expired document audit records and returns the count.</summary>
    public async Task<int> Handle(PurgeExpiredDocumentAuditsCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await auditStore.PurgeExpiredRecords(timeProvider.GetUtcNow().UtcDateTime, ct);
    }
}
