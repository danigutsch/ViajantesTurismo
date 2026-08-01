using Microsoft.EntityFrameworkCore;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class TourStore(AdminWriteDbContext dbContext) : ITourStore
{
    public void Add(Tour tour) => dbContext.Tours.Add(tour);

    public async Task<Tour?> GetById(Guid id, CancellationToken ct) =>
        await dbContext.Tours
            .Include(t => t.Bookings)
            .ThenInclude(b => b.Payments)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Tour?> GetByBookingId(Guid bookingId, CancellationToken ct) =>
        await dbContext.Tours
            .Include(t => t.Bookings)
            .ThenInclude(b => b.Payments)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Bookings.Any(b => b.Id == bookingId), ct);

    public async Task<Option<Guid>> GetTourIdByBookingId(Guid bookingId, CancellationToken ct)
    {
        var booking = await dbContext.Set<Booking>()
            .Where(currentBooking => currentBooking.Id == bookingId)
            .Select(currentBooking => new { currentBooking.TourId })
            .SingleOrDefaultAsync(ct);

        return Option.FromNullable(booking)
            .Map(currentBooking => currentBooking.TourId);
    }

    public async Task<bool> IdentifierExists(string identifier, CancellationToken ct) =>
        await dbContext.Tours.AnyAsync(t => t.Identifier == identifier, ct);

    public async Task<bool> IdentifierExistsExcluding(string identifier, Guid excludeTourId, CancellationToken ct) =>
        await dbContext.Tours.AnyAsync(t => t.Identifier == identifier && t.Id != excludeTourId, ct);

    public void Delete(Tour tour) => dbContext.Tours.Remove(tour);
}
