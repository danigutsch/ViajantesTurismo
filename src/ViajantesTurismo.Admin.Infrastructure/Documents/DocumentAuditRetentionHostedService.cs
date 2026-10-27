using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PandaBear.Scheduling;
using ViajantesTurismo.Admin.Application.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Purges expired document audit metadata on the currently configured daily schedule.</summary>
internal sealed class DocumentAuditRetentionHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<DocumentAuditRetentionHostedService> logger)
    : PollingBackgroundService(logger, DocumentAuditRetentionServiceName, TimeSpan.FromDays(1))
{
    private const string DocumentAuditRetentionServiceName = "admin-document-audit-retention";

    /// <summary>Runs one purge batch and returns the number of removed audit records.</summary>
    internal ValueTask<int> RunBatch(CancellationToken ct) => ExecuteBatch(ct);

    protected override async ValueTask<int> ExecuteBatch(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<PurgeExpiredDocumentAuditsCommandHandler>();
        return await handler.Handle(new PurgeExpiredDocumentAuditsCommand(), ct).ConfigureAwait(false);
    }
}
