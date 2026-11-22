using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Testing.Fakes;

public sealed class FakeTourStore : ITourStore, ITourCommandStore
{
    private readonly List<Tour> _tours = [];

    public void Add(Tour tour) => _tours.Add(tour);

    public Task<Tour?> GetById(Guid id, CancellationToken ct) =>
        Task.FromResult(_tours.SingleOrDefault(t => t.Id == id));

    public Task<Tour?> GetByBookingId(Guid bookingId, CancellationToken ct) =>
        Task.FromResult(_tours.SingleOrDefault(t => t.Bookings.Any(b => b.Id == bookingId)));

    async Task<Option<Tour>> ITourCommandStore.GetById(Guid id, CancellationToken ct) =>
        Option.FromNullable(await GetById(id, ct));

    async Task<Option<Tour>> ITourCommandStore.GetByBookingId(Guid bookingId, CancellationToken ct) =>
        Option.FromNullable(await GetByBookingId(bookingId, ct));

    Task<Tour?> ITourCommandStore.GetByBookingIdForPaymentUpdate(Guid bookingId, CancellationToken ct) => GetByBookingId(bookingId, ct);

    public Task<bool> IdentifierExists(string identifier, CancellationToken ct) =>
        Task.FromResult(_tours.Any(t =>
            string.Equals(t.Identifier, identifier, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> IdentifierExistsExcluding(string identifier, Guid excludeTourId, CancellationToken ct) =>
        Task.FromResult(_tours.Any(t =>
            string.Equals(t.Identifier, identifier, StringComparison.OrdinalIgnoreCase) &&
            t.Id != excludeTourId));

    public void AddExistingTour(Tour tour) => _tours.Add(tour);
}
