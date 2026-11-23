using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure.Documents;

/// <summary>Query service whose tours, customers and bookings are all missing or empty.</summary>
internal sealed class MissingDocumentSourceQueries : IQueryService
{
    /// <summary>Returns no tours.</summary>
    public Task<IReadOnlyList<GetTourDto>> GetAllTours(CancellationToken ct) => Task.FromResult<IReadOnlyList<GetTourDto>>([]);

    /// <inheritdoc />
    public Task<GetTourOptionsDto> SearchTourOptions(TourOptionSearchDto request, CancellationToken ct) => throw new NotSupportedException();
    /// <summary>Returns no tour.</summary>
    public Task<GetTourDto?> GetTourById(Guid id, CancellationToken ct) => Task.FromResult<GetTourDto?>(null);
    /// <inheritdoc />
    public Task<GetTourDto?> GetActiveTourById(Guid id, CancellationToken ct) => Task.FromResult<GetTourDto?>(null);
    /// <inheritdoc />
    public Task<GetTourRetirementDto?> GetTourRetirement(Guid id, CancellationToken ct) => Task.FromResult<GetTourRetirementDto?>(null);
    /// <summary>Returns no customers.</summary>
    public Task<IReadOnlyList<GetCustomerDto>> GetAllCustomers(CancellationToken ct) => Task.FromResult<IReadOnlyList<GetCustomerDto>>([]);

    /// <inheritdoc />
    public Task<GetCustomerOptionsDto> SearchCustomerOptions(CustomerOptionSearchDto request, CancellationToken ct) =>
        throw new NotSupportedException();
    /// <summary>Returns no customer details.</summary>
    public Task<CustomerDetailsDto?> GetCustomerDetailsById(Guid id, CancellationToken ct) => Task.FromResult<CustomerDetailsDto?>(null);
    /// <summary>Returns no bookings.</summary>
    public Task<IReadOnlyList<GetBookingDto>> GetAllBookings(CancellationToken ct) => Task.FromResult<IReadOnlyList<GetBookingDto>>([]);

    /// <inheritdoc />
    public Task<GetBookingPageDto> GetBookingPage(string? page, BookingPageSort sort, bool descending, CancellationToken ct) =>
        throw new NotSupportedException();
    /// <summary>Returns no booking.</summary>
    public Task<GetBookingDto?> GetBookingById(Guid id, CancellationToken ct) => Task.FromResult<GetBookingDto?>(null);
    /// <summary>Returns no bookings for the tour.</summary>
    public Task<IReadOnlyList<GetBookingDto>> GetBookingsByTourId(Guid tourId, CancellationToken ct) => Task.FromResult<IReadOnlyList<GetBookingDto>>([]);
    /// <summary>Returns no bookings for the customer.</summary>
    public Task<IReadOnlyList<GetBookingDto>> GetBookingsByCustomerId(Guid customerId, CancellationToken ct) => Task.FromResult<IReadOnlyList<GetBookingDto>>([]);
}
