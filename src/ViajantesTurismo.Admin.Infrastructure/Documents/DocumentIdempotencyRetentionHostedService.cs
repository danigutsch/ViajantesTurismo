using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PandaBear.Scheduling;
using ViajantesTurismo.Admin.Application.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Purges document idempotency entries past their replay window on a daily schedule.</summary>
/// <param name="scopeFactory">Creates the scope that resolves the purge handler.</param>
/// <param name="logger">The logger for the polling service.</param>
internal sealed class DocumentIdempotencyRetentionHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<DocumentIdempotencyRetentionHostedService> logger)
    : PollingBackgroundService(logger, DocumentIdempotencyRetentionServiceName, TimeSpan.FromDays(1))
{
    private const string DocumentIdempotencyRetentionServiceName = "admin-document-idempotency-retention";

    /// <summary>Runs one purge batch and returns the number of removed entries.</summary>
    internal ValueTask<int> RunBatch(CancellationToken ct) => ExecuteBatch(ct);

    private void LogHeld(int held)
    {
        if (held > 0)
        {
            DocumentRetentionLog.SkippedHeld(logger, DocumentIdempotencyRetentionServiceName, held);
        }
    }

    protected override async ValueTask<int> ExecuteBatch(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<PurgeExpiredDocumentIdempotencyEntriesCommandHandler>();
        var counts = await handler.Handle(ct).ConfigureAwait(false);
        LogHeld(counts.Held);
        return counts.Purged;
    }
}
