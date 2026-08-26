using System.Net;
using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Testing.Builders;

namespace ViajantesTurismo.Admin.Testing.Fakes.ApiClients;

public sealed class FakeBookingsApiClient : IBookingsApiClient
{
    private readonly List<GetBookingDto> _bookings = [];
    private Task _bookingActionTask = Task.CompletedTask;
    private ContractCommandOutcomeDto? _createBookingOutcome;
    private Exception? _getAllBookingsException;
    private Exception? _getBookingByIdException;
    private int? _getBookingByIdExceptionCall;
    private Exception? _recordPaymentExceptionAfterCommit;
    private ContractCommandOutcomeDto? _recordPaymentOutcome;
    private Task _recordPaymentTask = Task.CompletedTask;
    private Exception? _updateBookingNotesException;
    private decimal? _updatedDetailsTotalPrice;

    public int BookingActionCallCount { get; private set; }

    public int CommittedPaymentCount { get; private set; }

    public int GetBookingByIdCallCount { get; private set; }

    public UpdateBookingDetailsDto? LastUpdatedDetails { get; private set; }

    public int RecordPaymentCallCount { get; private set; }

    public int UpdateBookingDetailsCallCount { get; private set; }

    public int UpdateBookingDiscountCallCount { get; private set; }

    public int UpdateBookingNotesCallCount { get; private set; }

    public Task<GetBookingDto[]> GetAllBookings(CancellationToken ct)
    {
        if (_getAllBookingsException is not null)
        {
            throw _getAllBookingsException;
        }

        return Task.FromResult(_bookings.ToArray());
    }

    public Task<GetBookingDto?> GetBookingById(Guid id, CancellationToken ct)
    {
        GetBookingByIdCallCount++;

        if (_getBookingByIdException is not null &&
            (!_getBookingByIdExceptionCall.HasValue || _getBookingByIdExceptionCall == GetBookingByIdCallCount))
        {
            throw _getBookingByIdException;
        }

        return Task.FromResult(_bookings.FirstOrDefault(b => b.Id == id));
    }

    public Task<GetBookingDto[]> GetBookingsByTourId(Guid tourId, CancellationToken ct)
    {
        return Task.FromResult(_bookings.Where(b => b.TourId == tourId).ToArray());
    }

    public Task<GetBookingDto[]> GetBookingsByCustomerId(Guid customerId, CancellationToken ct)
    {
        return Task.FromResult(_bookings.Where(b => b.CustomerId == customerId || b.CompanionId == customerId).ToArray());
    }

    public Task<ContractCommandOutcomeDto> CreateBooking(CreateBookingDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

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

    public Task UpdateBookingDiscount(Guid id, UpdateBookingDiscountDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);
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

        return Task.CompletedTask;
    }

    public Task UpdateBookingDetails(Guid id, UpdateBookingDetailsDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

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

        return Task.CompletedTask;
    }

    public Task UpdateBookingNotes(Guid id, UpdateBookingNotesDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);
        UpdateBookingNotesCallCount++;

        if (_updateBookingNotesException is not null)
        {
            throw _updateBookingNotesException;
        }

        var index = _bookings.FindIndex(booking => booking.Id == id);
        if (index >= 0)
        {
            _bookings[index] = _bookings[index] with { Notes = dto.Notes };
        }

        return Task.CompletedTask;
    }

    public Task CancelBooking(Guid id, CancellationToken ct) =>
        ExecuteBookingAction(() => UpdateBookingStatus(id, BookingStatusDto.Cancelled), ct);

    public Task ConfirmBooking(Guid id, CancellationToken ct) =>
        ExecuteBookingAction(() => UpdateBookingStatus(id, BookingStatusDto.Confirmed), ct);

    public Task CompleteBooking(Guid id, CancellationToken ct) =>
        ExecuteBookingAction(() => UpdateBookingStatus(id, BookingStatusDto.Completed), ct);

    public Task DeleteBooking(Guid id, CancellationToken ct) =>
        ExecuteBookingAction(() =>
        {
            var booking = _bookings.FirstOrDefault(candidate => candidate.Id == id);
            if (booking is not null)
            {
                _bookings.Remove(booking);
            }
        }, ct);

    public async Task<ContractCommandOutcomeDto> RecordPayment(Guid bookingId, CreatePaymentDto dto, CancellationToken ct)
    {
        RecordPaymentCallCount++;
        await _recordPaymentTask.WaitAsync(ct);

        if (_recordPaymentExceptionAfterCommit is not null)
        {
            CommittedPaymentCount++;
            throw _recordPaymentExceptionAfterCommit;
        }

        if (_recordPaymentOutcome is not null)
        {
            return _recordPaymentOutcome;
        }

        var paymentId = Guid.NewGuid();
        return ContractCommandOutcome.Succeeded(HttpStatusCode.Created, new Uri($"/api/v1/bookings/{bookingId}/payments/{paymentId}", UriKind.Relative));
    }

    public void AddBooking(GetBookingDto booking) => _bookings.Add(booking);

    public void SetCreateBookingOutcome(ContractCommandOutcomeDto outcome) => _createBookingOutcome = outcome;

    public void SetBookingActionTask(Task actionTask)
    {
        ArgumentNullException.ThrowIfNull(actionTask);
        _bookingActionTask = actionTask;
    }

    public void SetGetAllBookingsException(Exception exception) => _getAllBookingsException = exception;

    public void SetGetBookingByIdException(Exception exception) => _getBookingByIdException = exception;

    public void SetGetBookingByIdExceptionOnCall(int call, Exception exception)
    {
        _getBookingByIdExceptionCall = call;
        _getBookingByIdException = exception;
    }

    public void SetRecordPaymentOutcome(ContractCommandOutcomeDto outcome) => _recordPaymentOutcome = outcome;

    public void SetRecordPaymentExceptionAfterCommit(Exception exception) =>
        _recordPaymentExceptionAfterCommit = exception;

    public void SetRecordPaymentTask(Task paymentTask)
    {
        ArgumentNullException.ThrowIfNull(paymentTask);
        _recordPaymentTask = paymentTask;
    }

    public void SetUpdateBookingNotesException(Exception exception) => _updateBookingNotesException = exception;

    public void SetUpdatedDetailsTotalPrice(decimal totalPrice) => _updatedDetailsTotalPrice = totalPrice;

    private async Task ExecuteBookingAction(Action onCompleted, CancellationToken ct)
    {
        BookingActionCallCount++;
        await _bookingActionTask.WaitAsync(ct);
        onCompleted();
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
