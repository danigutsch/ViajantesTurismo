using ViajantesTurismo.Admin.Domain;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class TourStore(ApplicationDbContext dbContext) : ITourStore
{
    public void Add(Tour tour) => dbContext.Tours.Add(tour);

    public async Task<Tour?> GetById(int id, CancellationToken ct) =>
        await dbContext.Tours.FindAsync([id], ct);
}