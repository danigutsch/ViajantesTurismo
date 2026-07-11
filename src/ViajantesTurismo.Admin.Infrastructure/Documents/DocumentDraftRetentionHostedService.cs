using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedKernel.Scheduling;
using ViajantesTurismo.Admin.Application.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

internal sealed class DocumentDraftRetentionHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<DocumentDraftRetentionHostedService> logger)
    : PollingBackgroundService(logger, DocumentDraftRetentionServiceName, TimeSpan.FromDays(1))
{
    private const string DocumentDraftRetentionServiceName = "admin-document-draft-retention";

    internal ValueTask<int> RunBatch(CancellationToken stoppingToken) => ExecuteBatch(stoppingToken);

    protected override async ValueTask<int> ExecuteBatch(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<PurgeExpiredDraftsCommandHandler>();
        var result = await handler.Handle(new PurgeExpiredDraftsCommand(), stoppingToken).ConfigureAwait(false);

        return result.IsSuccess ? result.Value : 0;
    }
}
