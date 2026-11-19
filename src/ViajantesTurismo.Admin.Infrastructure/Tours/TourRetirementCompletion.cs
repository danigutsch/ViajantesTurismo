using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application.Tours;

namespace ViajantesTurismo.Admin.Infrastructure.Tours;

/// <summary>Only applies a Catalog completion to its matching pending retirement.</summary>
internal sealed class TourRetirementCompletion(AdminWriteDbContext dbContext) : ITourRetirementCompletion
{
    /// <inheritdoc />
    public async Task RecordApplied(Guid tourId, Guid retirementId, DateTimeOffset appliedAt, CancellationToken ct)
    {
        await dbContext.Tours
            .Where(tour => tour.Id == tourId
                && tour.RetirementId == retirementId
                && tour.CatalogRetirementAppliedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(
                tour => tour.CatalogRetirementAppliedAt,
                appliedAt.ToUniversalTime()), ct);
    }
}
