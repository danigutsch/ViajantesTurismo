using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

/// <summary>Query service fake returning the supplied booking, tour and booking list; unsupported queries throw.</summary>
/// <param name="booking">The booking returned by <c>GetBookingById</c> when the id matches, or null.</param>
/// <param name="tour">The tour returned by <c>GetTourById</c> when the id matches, or null.</param>
/// <param name="bookings">The list returned by booking list queries, or empty when null.</param>
internal sealed class FakeQueryService(GetBookingDto? booking, GetTourDto? tour = null, IReadOnlyList<GetBookingDto>? bookings = null) : IQueryService
{
    /// <summary>Not supported by this fake; throws.</summary>
    public Task<IReadOnlyList<GetTourDto>> GetAllTours(CancellationToken ct) => throw new NotSupportedException();

    /// <summary>Returns the configured tour when its id matches, otherwise null.</summary>
    public Task<GetTourDto?> GetTourById(Guid id, CancellationToken ct) => Task.FromResult(tour?.Id == id ? tour : null);

    /// <summary>Not supported by this fake; throws.</summary>
    public Task<IReadOnlyList<GetCustomerDto>> GetAllCustomers(CancellationToken ct) => throw new NotSupportedException();

    /// <summary>Not supported by this fake; throws.</summary>
    public Task<CustomerDetailsDto?> GetCustomerDetailsById(Guid id, CancellationToken ct) => throw new NotSupportedException();

    /// <summary>Returns the configured bookings, or an empty list.</summary>
    public Task<IReadOnlyList<GetBookingDto>> GetAllBookings(CancellationToken ct) =>
        Task.FromResult(bookings ?? []);

    /// <summary>Returns the configured booking when its id matches, otherwise null.</summary>
    public Task<GetBookingDto?> GetBookingById(Guid id, CancellationToken ct) =>
        Task.FromResult(booking?.Id == id ? booking : null);

    /// <summary>Returns the configured bookings regardless of tour id.</summary>
    public Task<IReadOnlyList<GetBookingDto>> GetBookingsByTourId(Guid tourId, CancellationToken ct) =>
        Task.FromResult(bookings ?? []);

    /// <summary>Returns the configured bookings regardless of customer id.</summary>
    public Task<IReadOnlyList<GetBookingDto>> GetBookingsByCustomerId(Guid customerId, CancellationToken ct) =>
        Task.FromResult(bookings ?? []);
}
