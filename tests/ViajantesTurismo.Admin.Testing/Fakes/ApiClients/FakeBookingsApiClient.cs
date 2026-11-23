using System.Net;
using PandaBear.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Testing.Builders;

namespace ViajantesTurismo.Admin.Testing.Fakes.ApiClients;

public sealed class FakeBookingsApiClient : IBookingsApiClient
{
    private readonly List<GetBookingDto> _bookings = [];
    private Task _bookingActionTask = Task.CompletedTask;
    private HttpOutcome? _bookingActionOutcome;
    private HttpOutcome? _createBookingOutcome;
    private Exception? _getAllBookingsException;
    private HttpOutcome<GetBookingDto[]>? _getAllBookingsOutcome;
    private Exception? _getBookingPageException;
    private HttpOutcome<GetBookingPageDto>? _getBookingPageOutcome;
    private readonly List<(int Page, BookingPageSort Sort, bool Descending)> _pageRequests = [];
    private readonly List<(Guid Owner, bool ByTour, int Page, BookingPageSort Sort, bool Descending)> _historyRequests = [];
    private HttpOutcome<GetBookingHistoryPageDto>? _getBookingHistoryOutcome;
    private Exception? _getBookingByIdException;
    private int? _getBookingByIdExceptionCall;
    private HttpOutcome<GetBookingDto>? _getBookingByIdOutcome;
    private int? _getBookingByIdOutcomeCall;
    private Exception? _updateBookingNotesException;
    private HttpOutcome? _updateBookingNotesOutcome;
    private HttpOutcome? _updateBookingDetailsOutcome;
    private HttpOutcome? _updateBookingDiscountOutcome;
    private Task? _updateBookingNotesGate;
    private decimal? _updatedDetailsTotalPrice;

    public int BookingActionCallCount
    {
        get; private set;
    }


    public int GetBookingByIdCallCount
    {
        get; private set;
    }

    public int GetAllBookingsCallCount
    {
        get; private set;
    }

    public IReadOnlyList<(int Page, BookingPageSort Sort, bool Descending)> PageRequests => _pageRequests;

    /// <summary>Gets the recorded requests for owner booking pages.</summary>
    public IReadOnlyList<(Guid Owner, bool ByTour, int Page, BookingPageSort Sort, bool Descending)> HistoryRequests => _historyRequests;

    /// <summary>Gets the number of legacy collection requests for booking histories.</summary>
    public int GetBookingHistoryCollectionCallCount
    {
        get; private set;
    }

    public UpdateBookingDetailsDto? LastUpdatedDetails
    {
        get; private set;
    }


    public int UpdateBookingDetailsCallCount
    {
        get; private set;
    }

    public int UpdateBookingDiscountCallCount
    {
        get; private set;
    }

    public int UpdateBookingNotesCallCount
    {
        get; private set;
    }

    public Task<HttpOutcome<GetBookingDto[]>> GetAllBookings(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        GetAllBookingsCallCount++;

        if (_getAllBookingsException is not null)
        {
            throw _getAllBookingsException;
        }

        if (_getAllBookingsOutcome is not null)
        {
            return Task.FromResult(_getAllBookingsOutcome);
        }

        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, _bookings.ToArray()));
    }

    public Task<HttpOutcome<GetBookingPageDto>> GetBookingPage(int page, BookingPageSort sort, bool descending, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _pageRequests.Add((page, sort, descending));
        if (_getBookingPageException is not null)
        {
            throw _getBookingPageException;
        }

        if (_getBookingPageOutcome is not null)
        {
            return Task.FromResult(_getBookingPageOutcome);
        }

        var counts = new BookingOverviewCountsDto(
            _bookings.Count,
            _bookings.Count(booking => booking.Status == BookingStatusDto.Pending),
            _bookings.Count(booking => booking.Status == BookingStatusDto.Confirmed),
            _bookings.Count(booking => booking.Status == BookingStatusDto.Completed),
            _bookings.Count(booking => booking.Status == BookingStatusDto.Cancelled));
        var items = ReadWindow(_bookings, page, sort, descending);
        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, new GetBookingPageDto(items, counts)));
    }

    /// <inheritdoc />
    public Task<HttpOutcome<GetBookingHistoryPageDto>> GetTourBookingPage(Guid tourId, int page, BookingPageSort sort, bool descending,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _historyRequests.Add((tourId, true, page, sort, descending));
        return Task.FromResult(_getBookingHistoryOutcome ??
            ReadHistory(_bookings.Where(booking => booking.TourId == tourId), page, sort, descending));
    }

    /// <inheritdoc />
    public Task<HttpOutcome<GetBookingHistoryPageDto>> GetCustomerBookingPage(Guid customerId, int page, BookingPageSort sort, bool descending,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _historyRequests.Add((customerId, false, page, sort, descending));
        return Task.FromResult(_getBookingHistoryOutcome ?? ReadHistory(
            _bookings.Where(booking => booking.CustomerId == customerId || booking.CompanionId == customerId),
            page, sort == BookingPageSort.Default ? BookingPageSort.BookingDate : sort, sort == BookingPageSort.Default || descending));
    }

    /// <summary>Sets a fixed outcome for owner history page requests.</summary>
    public void SetGetBookingHistoryOutcome(HttpOutcome<GetBookingHistoryPageDto> outcome) => _getBookingHistoryOutcome = outcome;

    private static HttpOutcome<GetBookingHistoryPageDto> ReadHistory(
        IEnumerable<GetBookingDto> matching, int page, BookingPageSort sort, bool descending)
    {
        var bookings = matching.ToArray();
        var items = ReadWindow(bookings, page, sort, descending);
        return HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, new GetBookingHistoryPageDto(items, bookings.Length));
    }

    private static GetBookingDto[] ReadWindow(IEnumerable<GetBookingDto> bookings, int page, BookingPageSort sort, bool descending)
    {
        var sorted = sort switch
        {
            BookingPageSort.BookingDate => OrderBy(bookings, booking => booking.BookingDate, descending),
            BookingPageSort.TourName => OrderBy(bookings, booking => booking.TourName, descending),
            BookingPageSort.CustomerName => OrderBy(bookings, booking => booking.CustomerName, descending),
            BookingPageSort.TotalPrice => OrderBy(bookings, booking => booking.TotalPrice, descending),
            _ => bookings.OrderByDescending(booking => booking.Status == BookingStatusDto.Cancelled)
                .ThenByDescending(booking => booking.BookingDate),
        };
        return
        [
            .. sorted.ThenBy(booking => booking.Id)
                .Skip((page - 1) * ContractConstants.BookingListPageSize)
                .Take(ContractConstants.BookingListPageSize)
        ];
    }

    private static IOrderedEnumerable<GetBookingDto> OrderBy<TKey>(IEnumerable<GetBookingDto> bookings, Func<GetBookingDto, TKey> key,
        bool descending) => descending ? bookings.OrderByDescending(key) : bookings.OrderBy(key);

    public Task<HttpOutcome<GetBookingDto>> GetBookingById(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        GetBookingByIdCallCount++;

        if (_getBookingByIdException is not null &&
            (!_getBookingByIdExceptionCall.HasValue || _getBookingByIdExceptionCall == GetBookingByIdCallCount))
        {
            throw _getBookingByIdException;
        }

        if (_getBookingByIdOutcome is not null &&
            (!_getBookingByIdOutcomeCall.HasValue || _getBookingByIdOutcomeCall == GetBookingByIdCallCount))
        {
            return Task.FromResult(_getBookingByIdOutcome);
        }

        var booking = _bookings.FirstOrDefault(b => b.Id == id);
        return Task.FromResult(booking is null
            ? HttpOutcomeMapper.Status<GetBookingDto>(HttpOutcomeKind.NotFound, HttpStatusCode.NotFound)
            : HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, booking));
    }

    public Task<HttpOutcome<GetBookingDto[]>> GetBookingsByTourId(Guid tourId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        GetBookingHistoryCollectionCallCount++;
        return Task.FromResult(HttpOutcomeMapper.Succeeded(
            HttpStatusCode.OK,
            _bookings.Where(b => b.TourId == tourId).ToArray()));
    }

    public Task<HttpOutcome<GetBookingDto[]>> GetBookingsByCustomerId(Guid customerId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        GetBookingHistoryCollectionCallCount++;
        return Task.FromResult(HttpOutcomeMapper.Succeeded(
            HttpStatusCode.OK,
            _bookings.Where(b => b.CustomerId == customerId || b.CompanionId == customerId).ToArray()));
    }

    public Task<HttpOutcome> CreateBooking(CreateBookingDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ct.ThrowIfCancellationRequested();

        if (_createBookingOutcome is not null)
        {
            return Task.FromResult(_createBookingOutcome);
        }

        var newBooking = DtoBuilders.BuildBookingDto(
            tourId: dto.TourId,
            customerId: dto.PrincipalCustomerId,
            companionId: dto.CompanionCustomerId,
            discountType: dto.DiscountType,
            discountAmount: dto.DiscountAmount,
            notes: dto.Notes,
            roomType: dto.RoomType,
            principalBikeType: dto.PrincipalBikeType,
            companionBikeType: dto.CompanionBikeType
        );

        _bookings.Add(newBooking);
        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.Created, new Uri($"/api/v1/bookings/{newBooking.Id}", UriKind.Relative)));
    }

    public Task<HttpOutcome> UpdateBookingDiscount(Guid id, UpdateBookingDiscountDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ct.ThrowIfCancellationRequested();
        UpdateBookingDiscountCallCount++;

        if (_updateBookingDiscountOutcome is not null)
        {
            return Task.FromResult(_updateBookingDiscountOutcome);
        }

        var index = _bookings.FindIndex(booking => booking.Id == id);
        if (index >= 0)
        {
            _bookings[index] = _bookings[index] with
            {
                DiscountType = dto.DiscountType,
                DiscountAmount = dto.DiscountAmount,
                DiscountReason = dto.DiscountReason
            };
        }

        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, location: null));
    }

    public Task<HttpOutcome> UpdateBookingDetails(Guid id, UpdateBookingDetailsDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ct.ThrowIfCancellationRequested();

        UpdateBookingDetailsCallCount++;
        LastUpdatedDetails = dto;

        if (_updateBookingDetailsOutcome is not null)
        {
            return Task.FromResult(_updateBookingDetailsOutcome);
        }

        var index = _bookings.FindIndex(booking => booking.Id == id);
        if (index >= 0)
        {
            var booking = _bookings[index];
            var totalPrice = _updatedDetailsTotalPrice ?? booking.TotalPrice;
            _bookings[index] = booking with
            {
                RoomType = dto.RoomType,
                PrincipalBikeType = dto.PrincipalBikeType,
                CompanionId = dto.CompanionCustomerId,
                CompanionName = dto.CompanionCustomerId == booking.CompanionId ? booking.CompanionName : null,
                CompanionBikeType = dto.CompanionBikeType,
                TotalPrice = totalPrice,
                RemainingBalance = totalPrice - booking.AmountPaid
            };
        }

        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, location: null));
    }

    public async Task<HttpOutcome> UpdateBookingNotes(Guid id, UpdateBookingNotesDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ct.ThrowIfCancellationRequested();
        UpdateBookingNotesCallCount++;

        if (_updateBookingNotesGate is not null)
        {
            await _updateBookingNotesGate;
        }

        if (_updateBookingNotesException is not null)
        {
            throw _updateBookingNotesException;
        }

        if (_updateBookingNotesOutcome is not null)
        {
            return _updateBookingNotesOutcome;
        }

        var index = _bookings.FindIndex(booking => booking.Id == id);
        if (index >= 0)
        {
            _bookings[index] = _bookings[index] with
            {
                Notes = dto.Notes
            };
        }

        return HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, location: null);
    }

    public Task<HttpOutcome> CancelBooking(Guid id, CancellationToken ct) =>
        ExecuteBookingAction(() => UpdateBookingStatus(id, BookingStatusDto.Cancelled), HttpStatusCode.OK, ct);

    public Task<HttpOutcome> ConfirmBooking(Guid id, CancellationToken ct) =>
        ExecuteBookingAction(() => UpdateBookingStatus(id, BookingStatusDto.Confirmed), HttpStatusCode.OK, ct);

    public Task<HttpOutcome> CompleteBooking(Guid id, CancellationToken ct) =>
        ExecuteBookingAction(() => UpdateBookingStatus(id, BookingStatusDto.Completed), HttpStatusCode.OK, ct);

    public Task<HttpOutcome> DeleteBooking(Guid id, CancellationToken ct) =>
        ExecuteBookingAction(() =>
        {
            var booking = _bookings.FirstOrDefault(candidate => candidate.Id == id);
            if (booking is not null)
            {
                _bookings.Remove(booking);
            }
        }, HttpStatusCode.NoContent, ct);

    public void AddBooking(GetBookingDto booking) => _bookings.Add(booking);

    public void SetCreateBookingOutcome(HttpOutcome outcome) => _createBookingOutcome = outcome;

    public void SetBookingActionTask(Task actionTask)
    {
        ArgumentNullException.ThrowIfNull(actionTask);
        _bookingActionTask = actionTask;
    }

    public void SetBookingActionOutcome(HttpOutcome outcome) => _bookingActionOutcome = outcome;

    public void SetGetAllBookingsException(Exception exception) => _getAllBookingsException = exception;

    public void SetGetAllBookingsOutcome(HttpOutcome<GetBookingDto[]> outcome) => _getAllBookingsOutcome = outcome;

    public void SetGetBookingPageException(Exception exception) => _getBookingPageException = exception;

    public void SetGetBookingPageOutcome(HttpOutcome<GetBookingPageDto> outcome) => _getBookingPageOutcome = outcome;

    public void SetGetBookingByIdException(Exception exception) => _getBookingByIdException = exception;

    public void SetGetBookingByIdOutcome(HttpOutcome<GetBookingDto> outcome)
    {
        _getBookingByIdOutcomeCall = null;
        _getBookingByIdOutcome = outcome;
    }

    public void SetGetBookingByIdOutcomeOnCall(int call, HttpOutcome<GetBookingDto> outcome)
    {
        _getBookingByIdOutcomeCall = call;
        _getBookingByIdOutcome = outcome;
    }

    public void SetGetBookingByIdExceptionOnCall(int call, Exception exception)
    {
        _getBookingByIdExceptionCall = call;
        _getBookingByIdException = exception;
    }

    public void SetUpdateBookingNotesException(Exception exception) => _updateBookingNotesException = exception;

    public void SetUpdateBookingNotesOutcome(HttpOutcome outcome) => _updateBookingNotesOutcome = outcome;

    public void SetUpdateBookingDetailsOutcome(HttpOutcome outcome) => _updateBookingDetailsOutcome = outcome;

    public void SetUpdateBookingDiscountOutcome(HttpOutcome outcome) => _updateBookingDiscountOutcome = outcome;

    public void SetUpdateBookingNotesGate(Task gate) => _updateBookingNotesGate = gate;

    public void SetUpdatedDetailsTotalPrice(decimal totalPrice) => _updatedDetailsTotalPrice = totalPrice;

    private async Task<HttpOutcome> ExecuteBookingAction(
        Action onCompleted,
        HttpStatusCode statusCode,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        BookingActionCallCount++;
        await _bookingActionTask.WaitAsync(ct);
        if (_bookingActionOutcome is not null)
        {
            return _bookingActionOutcome;
        }

        onCompleted();
        return HttpOutcomeMapper.Succeeded(statusCode, location: null);
    }

    private void UpdateBookingStatus(Guid id, BookingStatusDto newStatus)
    {
        var index = _bookings.FindIndex(b => b.Id == id);
        if (index >= 0)
        {
            _bookings[index] = DtoBuilders.WithBookingFacts(_bookings[index] with
            {
                Status = newStatus
            });
        }
    }
}
