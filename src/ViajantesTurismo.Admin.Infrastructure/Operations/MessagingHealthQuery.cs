using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application.Operations;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.Infrastructure.Operations;

/// <summary>Reads Admin-to-Catalog delivery health from Admin's local transport.</summary>
internal sealed class MessagingHealthQuery(AdminReadDbContext dbContext, TimeProvider clock) : IMessagingHealthQuery
{
    /// <inheritdoc />
    public async Task<GetMessagingHealthDto> ReadMessagingHealth(CancellationToken ct)
    {
        var observedAt = clock.GetUtcNow();
        var outbox = await IntegrationEventMessagingHealth.ReadOutbox(dbContext, observedAt, ct);
        var health = await IntegrationEventMessagingHealth.ReadTransport(
            dbContext, IntegrationEventConsumerNames.Catalog, observedAt, ct);
        return new GetMessagingHealthDto
        {
            ObservedAtUtc = observedAt,
            AdminPendingOutboxCount = outbox.PendingCount,
            AdminOldestPendingOutboxEnqueuedAt = outbox.OldestEnqueuedAt,
            CatalogTerminalDeliveryFailureCount = health.TerminalCount,
            CatalogOldestTerminalFailureAt = health.OldestPoisonedAt,
            CatalogRetryCount = health.RetryCount,
            CatalogOldestRetryReceivedAt = health.OldestRetryReceivedAt,
            CatalogDueRetryCount = health.DueRetryCount,
        };
    }
}
