using System.Net;
using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Testing.Builders;

namespace ViajantesTurismo.Admin.Testing.Fakes.ApiClients;

public sealed class FakeBookingsApiClient : IBookingsApiClient
{
    private readonly List<GetBookingDto> _bookings = [];
    private Task _bookingActionTask = Task.CompletedTask;
    private HttpOutcome? _createBookingOutcome;
    private Exception? _getAllBookingsException;
    private HttpOutcome<GetBookingDto[]>? _getAllBookingsOutcome;
    private Exception? _getBookingByIdException;
    private int? _getBookingByIdExceptionCall;
    private HttpOutcome<GetBookingDto>? _getBookingByIdOutcome;
    private int? _getBookingByIdOutcomeCall;
    private Exception? _recordPaymentExceptionAfterCommit;
    private HttpOutcome? _recordPaymentOutcome;
    private HttpOutcome? _recordPaymentOutcomeAfterCommit;
    private Task _recordPaymentTask = Task.CompletedTask;
    private Exception? _updateBookingNotesException;
    private HttpOutcome? _updateBookingNotesOutcome;
    private decimal? _updatedDetailsTotalPrice;

    public int BookingActionCallCount { get; private set; }

    public int CommittedPaymentCount { get; private set; }

    public int GetBookingByIdCallCount { get; private set; }

    public UpdateBookingDetailsDto? LastUpdatedDetails { get; private set; }

    public int RecordPaymentCallCount { get; private set; }

    public int UpdateBookingDetailsCallCount { get; private set; }

    public int UpdateBookingDiscountCallCount { get; private set; }

    public int UpdateBookingNotesCallCount { get; private set; }

    public Task<HttpOutcome<GetBookingDto[]>> GetAllBookings(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (_getAllBookingsException is not null)
        {
            throw _getAllBookingsException;
        }

        if (_getAllBookingsOutcome is not null)
        {
            return Task.FromResult(_getAllBookingsOutcome);
        }

        return Task.FromResult(ContractCommandOutcome.Succeeded(HttpStatusCode.OK, _bookings.ToArray()));
    }

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
            ? ContractCommandOutcome.Status<GetBookingDto>(ContractCommandOutcomeKind.NotFound, HttpStatusCode.NotFound)
            : ContractCommandOutcome.Succeeded(HttpStatusCode.OK, booking));
    }

    public Task<HttpOutcome<GetBookingDto[]>> GetBookingsByTourId(Guid tourId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ContractCommandOutcome.Succeeded(
            HttpStatusCode.OK,
            _bookings.Where(b => b.TourId == tourId).ToArray()));
    }

    public Task<HttpOutcome<GetBookingDto[]>> GetBookingsByCustomerId(Guid customerId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ContractCommandOutcome.Succeeded(
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
        return Task.FromResult(ContractCommandOutcome.Succeeded(HttpStatusCode.Created, new Uri($"/api/v1/bookings/{newBooking.Id}", UriKind.Relative)));
    }

    public Task<HttpOutcome> UpdateBookingDiscount(Guid id, UpdateBookingDiscountDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ct.ThrowIfCancellationRequested();
        UpdateBookingDiscountCallCount++;

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

        return Task.FromResult(ContractCommandOutcome.Succeeded(HttpStatusCode.OK, location: null));
    }

    public Task<HttpOutcome> UpdateBookingDetails(Guid id, UpdateBookingDetailsDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ct.ThrowIfCancellationRequested();

        UpdateBookingDetailsCallCount++;
        LastUpdatedDetails = dto;

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

        return Task.FromResult(ContractCommandOutcome.Succeeded(HttpStatusCode.OK, location: null));
    }

    public Task<HttpOutcome> UpdateBookingNotes(Guid id, UpdateBookingNotesDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ct.ThrowIfCancellationRequested();
        UpdateBookingNotesCallCount++;

        if (_updateBookingNotesException is not null)
        {
            throw _updateBookingNotesException;
        }

        if (_updateBookingNotesOutcome is not null)
        {
            return Task.FromResult(_updateBookingNotesOutcome);
        }

        var index = _bookings.FindIndex(booking => booking.Id == id);
        if (index >= 0)
        {
            _bookings[index] = _bookings[index] with { Notes = dto.Notes };
        }

        return Task.FromResult(ContractCommandOutcome.Succeeded(HttpStatusCode.OK, location: null));
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

    public async Task<HttpOutcome> RecordPayment(Guid bookingId, CreatePaymentDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ct.ThrowIfCancellationRequested();
        RecordPaymentCallCount++;
        await _recordPaymentTask.WaitAsync(ct);

        if (_recordPaymentExceptionAfterCommit is not null)
        {
            CommittedPaymentCount++;
            throw _recordPaymentExceptionAfterCommit;
        }

        if (_recordPaymentOutcomeAfterCommit is not null)
        {
            CommittedPaymentCount++;
            return _recordPaymentOutcomeAfterCommit;
        }

        if (_recordPaymentOutcome is not null)
        {
            return _recordPaymentOutcome;
        }

        var paymentId = Guid.NewGuid();
        return ContractCommandOutcome.Succeeded(HttpStatusCode.Created, new Uri($"/api/v1/bookings/{bookingId}/payments/{paymentId}", UriKind.Relative));
    }

    public void AddBooking(GetBookingDto booking) => _bookings.Add(booking);

    public void SetCreateBookingOutcome(HttpOutcome outcome) => _createBookingOutcome = outcome;

    public void SetBookingActionTask(Task actionTask)
    {
        ArgumentNullException.ThrowIfNull(actionTask);
        _bookingActionTask = actionTask;
    }

    public void SetGetAllBookingsException(Exception exception) => _getAllBookingsException = exception;

    public void SetGetAllBookingsOutcome(HttpOutcome<GetBookingDto[]> outcome) => _getAllBookingsOutcome = outcome;

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

    public void SetRecordPaymentOutcome(HttpOutcome outcome) => _recordPaymentOutcome = outcome;

    public void SetRecordPaymentOutcomeAfterCommit(HttpOutcome outcome) =>
        _recordPaymentOutcomeAfterCommit = outcome;

    public void SetRecordPaymentExceptionAfterCommit(Exception exception) =>
        _recordPaymentExceptionAfterCommit = exception;

    public void SetRecordPaymentTask(Task paymentTask)
    {
        ArgumentNullException.ThrowIfNull(paymentTask);
        _recordPaymentTask = paymentTask;
    }

    public void SetUpdateBookingNotesException(Exception exception) => _updateBookingNotesException = exception;

    public void SetUpdateBookingNotesOutcome(HttpOutcome outcome) => _updateBookingNotesOutcome = outcome;

    public void SetUpdatedDetailsTotalPrice(decimal totalPrice) => _updatedDetailsTotalPrice = totalPrice;

    private async Task<HttpOutcome> ExecuteBookingAction(
        Action onCompleted,
        HttpStatusCode statusCode,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        BookingActionCallCount++;
        await _bookingActionTask.WaitAsync(ct);
        onCompleted();
        return ContractCommandOutcome.Succeeded(statusCode, location: null);
    }

    private void UpdateBookingStatus(Guid id, BookingStatusDto newStatus)
    {
        var index = _bookings.FindIndex(b => b.Id == id);
        if (index >= 0)
        {
            _bookings[index] = _bookings[index] with { Status = newStatus };
        }
    }
}
