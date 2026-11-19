using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Infrastructure.Payments;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure.Tours;

/// <summary>Tour repository backed by the Admin write database context.</summary>
/// <param name="dbContext">The Admin write database context.</param>
/// <param name="histories">Payment history store attached to loaded bookings.</param>
internal sealed class TourStore(AdminWriteDbContext dbContext, PaymentHistoryStore histories) : ITourStore
{
    /// <inheritdoc />
    public void Add(Tour tour) => dbContext.Tours.Add(tour);

    /// <inheritdoc />
    public async Task<Tour?> GetById(Guid id, CancellationToken ct)
    {
        var tour = await dbContext.Tours
            .Include(t => t.Bookings)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tour is not null)
        {
            await histories.Attach(tour.Bookings, ct);
        }
        return tour;
    }

    /// <inheritdoc />
    public async Task<Tour?> GetByBookingId(Guid bookingId, CancellationToken ct)
    {
        var tour = await dbContext.Tours
            .Include(t => t.Bookings)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Bookings.Any(b => b.Id == bookingId), ct);
        if (tour is not null)
        {
            await histories.Attach(tour.Bookings, ct);
        }
        return tour;
    }

    /// <inheritdoc />
    public async Task<bool> IdentifierExists(string identifier, CancellationToken ct) =>
        await dbContext.Tours.AnyAsync(t => t.Identifier == identifier, ct);

    /// <inheritdoc />
    public async Task<bool> IdentifierExistsExcluding(string identifier, Guid excludeTourId, CancellationToken ct) =>
        await dbContext.Tours.AnyAsync(t => t.Identifier == identifier && t.Id != excludeTourId, ct);

    /// <inheritdoc />
    public void Delete(Tour tour) => dbContext.Tours.Remove(tour);
}
