using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedKernel.Scheduling;
using ViajantesTurismo.Admin.Application.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Purges expired document audit metadata on the approved daily schedule.</summary>
internal sealed class DocumentAuditRetentionHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<DocumentAuditRetentionHostedService> logger)
    : PollingBackgroundService(logger, DocumentAuditRetentionServiceName, TimeSpan.FromDays(1))
{
    private const string DocumentAuditRetentionServiceName = "admin-document-audit-retention";

    internal ValueTask<int> RunBatch(CancellationToken stoppingToken) => ExecuteBatch(stoppingToken);

    protected override async ValueTask<int> ExecuteBatch(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<PurgeExpiredDocumentAuditsCommandHandler>();
        return await handler.Handle(new PurgeExpiredDocumentAuditsCommand(), stoppingToken).ConfigureAwait(false);
    }
}
