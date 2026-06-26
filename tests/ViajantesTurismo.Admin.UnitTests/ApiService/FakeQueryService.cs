using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

internal sealed class FakeQueryService(GetBookingDto? booking, IReadOnlyList<GetBookingDto>? bookings = null) : IQueryService
{
    public Task<IReadOnlyList<GetTourDto>> GetAllTours(CancellationToken ct) => throw new NotSupportedException();

    public Task<GetTourDto?> GetTourById(Guid id, CancellationToken ct) => throw new NotSupportedException();

    public Task<IReadOnlyList<GetCustomerDto>> GetAllCustomers(CancellationToken ct) => throw new NotSupportedException();

    public Task<CustomerDetailsDto?> GetCustomerDetailsById(Guid id, CancellationToken ct) => throw new NotSupportedException();

    public Task<IReadOnlyList<GetBookingDto>> GetAllBookings(CancellationToken ct) =>
        Task.FromResult(bookings ?? (IReadOnlyList<GetBookingDto>)[]);

    public Task<GetBookingDto?> GetBookingById(Guid id, CancellationToken ct) =>
        Task.FromResult(booking?.Id == id ? booking : null);

    public Task<IReadOnlyList<GetBookingDto>> GetBookingsByTourId(Guid tourId, CancellationToken ct) =>
        Task.FromResult(bookings ?? (IReadOnlyList<GetBookingDto>)[]);

    public Task<IReadOnlyList<GetBookingDto>> GetBookingsByCustomerId(Guid customerId, CancellationToken ct) =>
        Task.FromResult(bookings ?? (IReadOnlyList<GetBookingDto>)[]);
}
