using System.Net;
using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Testing.Builders;

namespace ViajantesTurismo.Admin.Testing.Fakes.ApiClients;

public sealed class FakeBookingsApiClient : IBookingsApiClient
{
    private readonly List<GetBookingDto> _bookings = [];
    private ContractCommandOutcomeDto? _createBookingOutcome;
    private Exception? _getBookingByIdException;
    private ContractCommandOutcomeDto? _recordPaymentOutcome;
    private Exception? _updateBookingNotesException;

    public Task<GetBookingDto[]> GetAllBookings(CancellationToken ct)
    {
        return Task.FromResult(_bookings.ToArray());
    }

    public Task<GetBookingDto?> GetBookingById(Guid id, CancellationToken ct)
    {
        if (_getBookingByIdException is not null)
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
        return Task.FromResult(_bookings.Where(b => b.CustomerId == customerId).ToArray());
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
            notes: dto.Notes
        );

        _bookings.Add(newBooking);
        return Task.FromResult(ContractCommandOutcome.Succeeded(HttpStatusCode.Created, new Uri($"/bookings/{newBooking.Id}", UriKind.Relative)));
    }

    public Task UpdateBookingDiscount(Guid id, UpdateBookingDiscountDto dto, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public Task UpdateBookingDetails(Guid id, UpdateBookingDetailsDto dto, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public Task UpdateBookingNotes(Guid id, UpdateBookingNotesDto dto, CancellationToken ct)
    {
        if (_updateBookingNotesException is not null)
        {
            throw _updateBookingNotesException;
        }

        return Task.CompletedTask;
    }

    public Task CancelBooking(Guid id, CancellationToken ct)
    {
        UpdateBookingStatus(id, BookingStatusDto.Cancelled);
        return Task.CompletedTask;
    }

    public Task ConfirmBooking(Guid id, CancellationToken ct)
    {
        UpdateBookingStatus(id, BookingStatusDto.Confirmed);
        return Task.CompletedTask;
    }

    public Task CompleteBooking(Guid id, CancellationToken ct)
    {
        UpdateBookingStatus(id, BookingStatusDto.Completed);
        return Task.CompletedTask;
    }

    public Task DeleteBooking(Guid id, CancellationToken ct)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == id);
        if (booking is not null)
        {
            _bookings.Remove(booking);
        }

        return Task.CompletedTask;
    }

    public Task<ContractCommandOutcomeDto> RecordPayment(Guid bookingId, CreatePaymentDto dto, CancellationToken ct)
    {
        if (_recordPaymentOutcome is not null)
        {
            return Task.FromResult(_recordPaymentOutcome);
        }

        var paymentId = Guid.NewGuid();
        return Task.FromResult(ContractCommandOutcome.Succeeded(HttpStatusCode.Created, new Uri($"/bookings/{bookingId}/payments/{paymentId}", UriKind.Relative)));
    }

    public void AddBooking(GetBookingDto booking) => _bookings.Add(booking);

    public void SetCreateBookingOutcome(ContractCommandOutcomeDto outcome) => _createBookingOutcome = outcome;

    public void SetGetBookingByIdException(Exception exception) => _getBookingByIdException = exception;

    public void SetRecordPaymentOutcome(ContractCommandOutcomeDto outcome) => _recordPaymentOutcome = outcome;

    public void SetUpdateBookingNotesException(Exception exception) => _updateBookingNotesException = exception;

    private void UpdateBookingStatus(Guid id, BookingStatusDto newStatus)
    {
        var index = _bookings.FindIndex(b => b.Id == id);
        if (index >= 0)
        {
            _bookings[index] = _bookings[index] with { Status = newStatus };
        }
    }
}
