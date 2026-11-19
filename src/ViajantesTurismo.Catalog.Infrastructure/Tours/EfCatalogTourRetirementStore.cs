using Microsoft.EntityFrameworkCore;
using Npgsql;
using PandaBear.Messaging.IntegrationEvents;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.Infrastructure.Tours;

/// <summary>Coordinates durable tour withdrawal and completion outbox entries.</summary>
internal sealed class EfCatalogTourRetirementStore(
    CatalogDbContext dbContext,
    IIntegrationEventOutbox outbox,
    TimeProvider clock) : ICatalogTourRetirementStore
{
    /// <inheritdoc />
    public async ValueTask Withdraw(CatalogTourRetired retirement, CancellationToken ct)
    {
        var existing = await dbContext.CatalogTourRetirements.AsNoTracking()
            .SingleOrDefaultAsync(row => row.AdminTourId == retirement.AdminTourId, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            EnsureMatchingRetirement(retirement, existing);
            return;
        }

        var marker = new CatalogTourRetirementEntity
        {
            AdminTourId = retirement.AdminTourId,
            RetirementId = retirement.RetirementId,
            WithdrawnAt = clock.GetUtcNow()
        };
        dbContext.CatalogTourRetirements.Add(marker);
        var completion = new CatalogTourRetirementAppliedIntegrationEvent(
            retirement.RetirementId, marker.WithdrawnAt, retirement.AdminTourId, retirement.RetirementId);
        await outbox.Enqueue(completion, ct).ConfigureAwait(false);
        try
        {
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            dbContext.ChangeTracker.Clear();
            existing = await dbContext.CatalogTourRetirements.AsNoTracking()
                .SingleAsync(row => row.AdminTourId == retirement.AdminTourId, ct).ConfigureAwait(false);
            EnsureMatchingRetirement(retirement, existing);
        }
    }

    private static void EnsureMatchingRetirement(CatalogTourRetired retirement, CatalogTourRetirementEntity existing)
    {
        if (existing.RetirementId != retirement.RetirementId)
        {
            throw new InvalidOperationException("The Catalog tour already has a different retirement marker.");
        }
    }
}
