using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure.Documents;

internal sealed class MissingDocumentSourceQueries : IQueryService
{
    public Task<IReadOnlyList<GetTourDto>> GetAllTours(CancellationToken ct) => Task.FromResult<IReadOnlyList<GetTourDto>>([]);
    public Task<GetTourDto?> GetTourById(Guid id, CancellationToken ct) => Task.FromResult<GetTourDto?>(null);
    public Task<IReadOnlyList<GetCustomerDto>> GetAllCustomers(CancellationToken ct) => Task.FromResult<IReadOnlyList<GetCustomerDto>>([]);
    public Task<CustomerDetailsDto?> GetCustomerDetailsById(Guid id, CancellationToken ct) => Task.FromResult<CustomerDetailsDto?>(null);
    public Task<IReadOnlyList<GetBookingDto>> GetAllBookings(CancellationToken ct) => Task.FromResult<IReadOnlyList<GetBookingDto>>([]);
    public Task<GetBookingDto?> GetBookingById(Guid id, CancellationToken ct) => Task.FromResult<GetBookingDto?>(null);
    public Task<IReadOnlyList<GetBookingDto>> GetBookingsByTourId(Guid tourId, CancellationToken ct) => Task.FromResult<IReadOnlyList<GetBookingDto>>([]);
    public Task<IReadOnlyList<GetBookingDto>> GetBookingsByCustomerId(Guid customerId, CancellationToken ct) => Task.FromResult<IReadOnlyList<GetBookingDto>>([]);
}
