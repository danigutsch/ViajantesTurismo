using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure.Customers;

/// <summary>Serves the configured read projection for a customer written by the endpoint.</summary>
internal sealed class CustomerMutationQueryService(
    FakeCustomerStore customers,
    Func<Guid, CustomerDetailsDto>? projectCustomer) : IQueryService
{
    /// <inheritdoc />
    public Task<CustomerDetailsDto?> GetCustomerDetailsById(Guid id, CancellationToken ct) =>
        Task.FromResult(customers.AllCustomers.Any(customer => customer.Id == id) ? projectCustomer?.Invoke(id) : null);

    /// <inheritdoc />
    public Task<IReadOnlyList<GetCustomerDto>> GetAllCustomers(CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<IReadOnlyList<GetTourDto>> GetAllTours(CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<GetTourDto?> GetTourById(Guid id, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<IReadOnlyList<GetBookingDto>> GetAllBookings(CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<GetBookingDto?> GetBookingById(Guid id, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<IReadOnlyList<GetBookingDto>> GetBookingsByTourId(Guid tourId, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<IReadOnlyList<GetBookingDto>> GetBookingsByCustomerId(Guid customerId, CancellationToken ct) => throw new NotSupportedException();
}
