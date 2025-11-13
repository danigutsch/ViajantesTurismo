using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class TourStore(ApplicationDbContext dbContext) : ITourStore
{
    public void Add(Tour tour) => dbContext.Tours.Add(tour);

    public async Task<Tour?> GetById(Guid id, CancellationToken ct) =>
        await dbContext.Tours.FindAsync([id], ct);

    public async Task<Tour?> GetByBookingId(Guid bookingId, CancellationToken ct) =>
        await dbContext.Tours
            .Include(t => t.Bookings)
            .FirstOrDefaultAsync(t => t.Bookings.Any(b => b.Id == bookingId), ct);

    public async Task<bool> IdentifierExists(string identifier, CancellationToken ct) =>
        await dbContext.Tours.AnyAsync(t => t.Identifier == identifier, ct);
}
