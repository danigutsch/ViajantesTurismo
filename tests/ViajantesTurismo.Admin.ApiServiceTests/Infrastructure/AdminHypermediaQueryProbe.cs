using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Testing.Builders;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure;

/// <summary>Returns a known read projection and recognizes the resources written by the handlers.</summary>
internal sealed class AdminHypermediaQueryProbe(FakeCustomerStore customers, FakeTourStore tours) : IQueryService
{
    /// <summary>Gets the customer visible in list and detail responses.</summary>
    internal GetCustomerDto Customer { get; } = DtoBuilders.BuildCustomerDto();

    /// <summary>Gets the tour visible in list and detail responses.</summary>
    internal GetTourDto Tour { get; } = DtoBuilders.BuildTourDto();

    /// <summary>Gets the booking visible in list and detail responses.</summary>
    internal GetBookingDto Booking { get; } = DtoBuilders.BuildBookingDto();

    /// <inheritdoc />
    public async Task<IReadOnlyList<GetCustomerDto>> GetAllCustomers(CancellationToken ct) =>
        await new[] { Customer }.ToAsyncEnumerable().ToArrayAsync(ct);

    /// <inheritdoc />
    public Task<GetCustomerPageDto> GetCustomerPage(string? page, CustomerPageSort sort, bool descending, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var row = new GetCustomerListItemDto(Customer.Id, Customer.FirstName, Customer.LastName,
            Customer.Email, Customer.Mobile, Customer.Nationality);
        return Task.FromResult(new GetCustomerPageDto([row], 1));
    }

    /// <inheritdoc />
    public Task<GetCustomerOptionsDto> SearchCustomerOptions(CustomerOptionSearchDto request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(new GetCustomerOptionsDto(
            [new GetCustomerOptionDto(Customer.Id, Customer.FirstName, Customer.LastName, Customer.Email, Customer.BikeType)], 1));
    }

    /// <inheritdoc />
    public async Task<CustomerDetailsDto?> GetCustomerDetailsById(Guid id, CancellationToken ct) =>
        id == Customer.Id || await customers.GetById(id, ct) is not null ? DtoBuilders.BuildCustomerDetailsDto(id: id) : null;

    /// <inheritdoc />
    public async Task<IReadOnlyList<GetTourDto>> GetAllTours(CancellationToken ct) =>
        await new[] { Tour }.ToAsyncEnumerable().ToArrayAsync(ct);

    /// <inheritdoc />
    public Task<GetTourPageDto> GetTourPage(string? page, TourPageSort sort, bool descending, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(new GetTourPageDto([DtoBuilders.ToTourListItem(Tour)], 1));
    }

    /// <inheritdoc />
    public Task<GetTourOptionsDto> SearchTourOptions(TourOptionSearchDto request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(new GetTourOptionsDto(
            [new GetTourOptionDto(Tour.Id, Tour.Name, Tour.Identifier, Tour.StartDate)], 1));
    }

    /// <inheritdoc />
    public async Task<GetTourDto?> GetTourById(Guid id, CancellationToken ct)
    {
        if (id == Tour.Id)
        {
            return Tour;
        }

        var tour = await tours.GetById(id, ct);
        return tour is null ? null : DtoBuilders.BuildTourDto(id: id, identifier: tour.Identifier, name: tour.Name);
    }

    /// <inheritdoc />
    public async Task<GetTourDto?> GetActiveTourById(Guid id, CancellationToken ct) => await GetTourById(id, ct);

    /// <inheritdoc />
    public async Task<GetTourRetirementDto?> GetTourRetirement(Guid id, CancellationToken ct) =>
        await Enumerable.Empty<GetTourRetirementDto>().ToAsyncEnumerable().SingleOrDefaultAsync(ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<GetBookingDto>> GetAllBookings(CancellationToken ct) =>
        await new[] { Booking }.ToAsyncEnumerable().ToArrayAsync(ct);

    /// <inheritdoc />
    public Task<GetBookingPageDto> GetBookingPage(string? page, BookingPageSort sort, bool descending, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(new GetBookingPageDto([Booking], new BookingOverviewCountsDto(1, 1, 0, 0, 0)));
    }

    /// <inheritdoc />
    public Task<GetBookingHistoryPageDto> GetTourBookingPage(Guid tourId, string? page, BookingPageSort sort, bool descending,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Booking.TourId == tourId
            ? new GetBookingHistoryPageDto([Booking], 1)
            : new GetBookingHistoryPageDto([], 0));
    }

    /// <inheritdoc />
    public Task<GetBookingHistoryPageDto> GetCustomerBookingPage(Guid customerId, string? page, BookingPageSort sort, bool descending,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Booking.CustomerId == customerId || Booking.CompanionId == customerId
            ? new GetBookingHistoryPageDto([Booking], 1)
            : new GetBookingHistoryPageDto([], 0));
    }

    /// <inheritdoc />
    public async Task<GetBookingDto?> GetBookingById(Guid id, CancellationToken ct)
    {
        if (id == Booking.Id)
        {
            return Booking;
        }

        var tour = await tours.GetByBookingId(id, ct);
        return tour is null ? null : DtoBuilders.BuildBookingDto(id: id, tourId: tour.Id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GetBookingDto>> GetBookingsByTourId(Guid tourId, CancellationToken ct) =>
        await new[] { Booking }.ToAsyncEnumerable().Where(booking => booking.TourId == tourId).ToArrayAsync(ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<GetBookingDto>> GetBookingsByCustomerId(Guid customerId, CancellationToken ct) =>
        await new[] { Booking }.ToAsyncEnumerable().Where(booking => booking.CustomerId == customerId).ToArrayAsync(ct);
}
