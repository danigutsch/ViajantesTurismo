using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PandaBear.Scheduling;
using ViajantesTurismo.Admin.Application.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Purges expired unfinalized document drafts on a daily schedule.</summary>
/// <param name="scopeFactory">Creates the scope that resolves the purge handler.</param>
/// <param name="logger">The logger for the polling service.</param>
internal sealed class DocumentDraftRetentionHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<DocumentDraftRetentionHostedService> logger)
    : PollingBackgroundService(logger, DocumentDraftRetentionServiceName, TimeSpan.FromDays(1))
{
    private const string DocumentDraftRetentionServiceName = "admin-document-draft-retention";

    /// <summary>Runs one purge batch and returns the number of removed drafts.</summary>
    internal ValueTask<int> RunBatch(CancellationToken ct) => ExecuteBatch(ct);

    protected override async ValueTask<int> ExecuteBatch(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<PurgeExpiredDraftsCommandHandler>();
        var removedCount = await handler.Handle(new PurgeExpiredDraftsCommand(), ct).ConfigureAwait(false);

        return removedCount;
    }
}
