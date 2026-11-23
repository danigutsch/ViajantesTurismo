using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.ApiServiceTests.Tours;

/// <summary>Provides the retirement state observed by the HTTP endpoints.</summary>
internal sealed class TourRetirementQueries(Tour tour, DateTimeOffset? completedAt) : IQueryService
{
    /// <inheritdoc />
    public Task<GetTourRetirementDto?> GetTourRetirement(Guid id, CancellationToken ct)
    {
        var status = id == tour.Id && tour.RetiredAt is { } retiredAt
            ? new GetTourRetirementDto
            {
                TourId = id,
                RetiredAt = retiredAt,
                CompletedAt = completedAt
            }
            : null;
        return Task.FromResult(status);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<GetTourDto>> GetAllTours(CancellationToken ct) => throw new NotSupportedException();

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
    public Task<IReadOnlyList<GetCustomerDto>> GetAllCustomers(CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<GetCustomerPageDto> GetCustomerPage(string? page, CustomerPageSort sort, bool descending, CancellationToken ct) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task<GetCustomerOptionsDto> SearchCustomerOptions(CustomerOptionSearchDto request, CancellationToken ct) =>
        throw new NotSupportedException();
    /// <inheritdoc />
    public Task<CustomerDetailsDto?> GetCustomerDetailsById(Guid id, CancellationToken ct) => throw new NotSupportedException();
    /// <inheritdoc />
    public Task<IReadOnlyList<GetBookingDto>> GetAllBookings(CancellationToken ct) => throw new NotSupportedException();

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
    /// <inheritdoc />
    public Task<IReadOnlyList<GetBookingDto>> GetBookingsByTourId(Guid tourId, CancellationToken ct) => throw new NotSupportedException();
    /// <inheritdoc />
    public Task<IReadOnlyList<GetBookingDto>> GetBookingsByCustomerId(Guid customerId, CancellationToken ct) => throw new NotSupportedException();
}
