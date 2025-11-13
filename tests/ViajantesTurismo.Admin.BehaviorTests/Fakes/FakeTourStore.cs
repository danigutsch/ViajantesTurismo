using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.BehaviorTests.Fakes;

public sealed class FakeTourStore : ITourStore
{
    private readonly List<Tour> _tours = [];

    public void Add(Tour tour) => _tours.Add(tour);

    public Task<Tour?> GetById(Guid id, CancellationToken ct) =>
        Task.FromResult(_tours.SingleOrDefault(t => t.Id == id));

    public Task<Tour?> GetByBookingId(Guid bookingId, CancellationToken ct) =>
        Task.FromResult(_tours.SingleOrDefault(t => t.Bookings.Any(b => b.Id == bookingId)));

    public Task<bool> IdentifierExists(string identifier, CancellationToken ct) =>
        Task.FromResult(_tours.Any(t =>
            string.Equals(t.Identifier, identifier, StringComparison.OrdinalIgnoreCase)));

    public void Delete(Tour tour) => _tours.Remove(tour);

    public void AddExistingTour(Tour tour) => _tours.Add(tour);
}
