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
    public Task<GetTourDto?> GetTourById(Guid id, CancellationToken ct) => throw new NotSupportedException();
    /// <inheritdoc />
    public Task<GetTourDto?> GetActiveTourById(Guid id, CancellationToken ct) => throw new NotSupportedException();
    /// <inheritdoc />
    public Task<IReadOnlyList<GetCustomerDto>> GetAllCustomers(CancellationToken ct) => throw new NotSupportedException();
    /// <inheritdoc />
    public Task<CustomerDetailsDto?> GetCustomerDetailsById(Guid id, CancellationToken ct) => throw new NotSupportedException();
    /// <inheritdoc />
    public Task<IReadOnlyList<GetBookingDto>> GetAllBookings(CancellationToken ct) => throw new NotSupportedException();
    /// <inheritdoc />
    public Task<GetBookingDto?> GetBookingById(Guid id, CancellationToken ct) => throw new NotSupportedException();
    /// <inheritdoc />
    public Task<IReadOnlyList<GetBookingDto>> GetBookingsByTourId(Guid tourId, CancellationToken ct) => throw new NotSupportedException();
    /// <inheritdoc />
    public Task<IReadOnlyList<GetBookingDto>> GetBookingsByCustomerId(Guid customerId, CancellationToken ct) => throw new NotSupportedException();
}
