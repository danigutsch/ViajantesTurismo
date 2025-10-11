using ViajantesTurismo.Admin.Domain;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class TourStore(ApplicationDbContext dbContext) : ITourStore
{
    public void Add(Tour tour) => dbContext.Tours.Add(tour);
}
