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
    public Task<GetCustomerPageDto> GetCustomerPage(string? page, CustomerPageSort sort, bool descending, CancellationToken ct) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task<GetCustomerOptionsDto> SearchCustomerOptions(CustomerOptionSearchDto request, CancellationToken ct) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task<GetTourPageDto> GetTourPage(string? page, TourPageSort sort, bool descending, CancellationToken ct) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task<GetTourOptionsDto> SearchTourOptions(TourOptionSearchDto request, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<GetTourDto?> GetTourById(Guid id, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<GetTourDto?> GetActiveTourById(Guid id, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<GetTourRetirementDto?> GetTourRetirement(Guid id, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<GetBookingPageDto> GetBookingPage(string? page, BookingPageSort sort, bool descending, CancellationToken ct) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task<GetBookingHistoryPageDto> GetTourBookingPage(Guid tourId, string? page, BookingPageSort sort, bool descending,
        CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<GetBookingHistoryPageDto> GetCustomerBookingPage(Guid customerId, string? page, BookingPageSort sort, bool descending,
        CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<GetBookingDto?> GetBookingById(Guid id, CancellationToken ct) => throw new NotSupportedException();

}
