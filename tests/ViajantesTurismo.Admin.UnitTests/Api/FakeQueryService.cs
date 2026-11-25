using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.UnitTests.Api;

/// <summary>Returns the supplied booking and tour. Unsupported queries throw.</summary>
/// <param name="booking">The booking returned by <c>GetBookingById</c> when the id matches, or null.</param>
/// <param name="tour">The tour returned by <c>GetTourById</c> when the id matches, or null.</param>
internal sealed class FakeQueryService(GetBookingDto? booking, GetTourDto? tour = null) : IQueryService
{
    /// <inheritdoc />
    public Task<GetTourPageDto> GetTourPage(string? page, TourPageSort sort, bool descending, CancellationToken ct) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task<GetTourOptionsDto> SearchTourOptions(TourOptionSearchDto request, CancellationToken ct) => throw new NotSupportedException();

    /// <summary>Returns the configured tour when its id matches, otherwise null.</summary>
    public Task<GetTourDto?> GetTourById(Guid id, CancellationToken ct) => Task.FromResult(tour?.Id == id ? tour : null);

    /// <inheritdoc />
    public async Task<GetTourDto?> GetActiveTourById(Guid id, CancellationToken ct) => await GetTourById(id, ct);

    /// <inheritdoc />
    public Task<GetTourRetirementDto?> GetTourRetirement(Guid id, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<GetCustomerPageDto> GetCustomerPage(string? page, CustomerPageSort sort, bool descending, CancellationToken ct) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task<GetCustomerOptionsDto> SearchCustomerOptions(CustomerOptionSearchDto request, CancellationToken ct) =>
        throw new NotSupportedException();

    /// <summary>Not supported by this fake; throws.</summary>
    public Task<CustomerDetailsDto?> GetCustomerDetailsById(Guid id, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<GetBookingPageDto> GetBookingPage(string? page, BookingPageSort sort, bool descending, CancellationToken ct) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task<GetBookingHistoryPageDto> GetTourBookingPage(Guid tourId, string? page, BookingPageSort sort, bool descending,
        CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<GetBookingHistoryPageDto> GetCustomerBookingPage(Guid customerId, string? page, BookingPageSort sort, bool descending,
        CancellationToken ct) => throw new NotSupportedException();

    /// <summary>Returns the configured booking when its id matches, otherwise null.</summary>
    public Task<GetBookingDto?> GetBookingById(Guid id, CancellationToken ct) =>
        Task.FromResult(booking?.Id == id ? booking : null);
}
