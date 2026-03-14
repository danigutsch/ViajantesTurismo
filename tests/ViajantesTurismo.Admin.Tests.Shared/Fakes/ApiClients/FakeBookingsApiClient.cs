using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Tests.Shared.Builders;

namespace ViajantesTurismo.Admin.Tests.Shared.Fakes.ApiClients;

public sealed class FakeBookingsApiClient : IBookingsApiClient
{
    private readonly List<GetBookingDto> _bookings = [];
    private Exception? _getBookingByIdException;
    private Exception? _updateBookingNotesException;

    public Task<GetBookingDto[]> GetAllBookings(CancellationToken cancellationToken)
    {
        return Task.FromResult(_bookings.ToArray());
    }

    public Task<GetBookingDto?> GetBookingById(Guid id, CancellationToken cancellationToken)
    {
        if (_getBookingByIdException is not null)
        {
            throw _getBookingByIdException;
        }

        return Task.FromResult(_bookings.FirstOrDefault(b => b.Id == id));
    }

    public Task<GetBookingDto[]> GetBookingsByTourId(Guid tourId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_bookings.Where(b => b.TourId == tourId).ToArray());
    }

    public Task<GetBookingDto[]> GetBookingsByCustomerId(Guid customerId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_bookings.Where(b => b.CustomerId == customerId).ToArray());
    }

    public Task<Uri> CreateBooking(CreateBookingDto dto, CancellationToken cancellationToken)
    {
        var newBooking = DtoBuilders.BuildBookingDto(
            tourId: dto.TourId,
            customerId: dto.PrincipalCustomerId,
            companionId: dto.CompanionCustomerId,
            discountType: dto.DiscountType,
            discountAmount: dto.DiscountAmount,
            notes: dto.Notes
        );

        _bookings.Add(newBooking);
        return Task.FromResult(new Uri($"/bookings/{newBooking.Id}", UriKind.Relative));
    }

    public Task UpdateBookingDiscount(Guid id, UpdateBookingDiscountDto dto, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task UpdateBookingDetails(Guid id, UpdateBookingDetailsDto dto, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task UpdateBookingNotes(Guid id, UpdateBookingNotesDto dto, CancellationToken cancellationToken)
    {
        if (_updateBookingNotesException is not null)
        {
            throw _updateBookingNotesException;
        }

        return Task.CompletedTask;
    }

    public Task CancelBooking(Guid id, CancellationToken cancellationToken)
    {
        UpdateBookingStatus(id, BookingStatusDto.Cancelled);
        return Task.CompletedTask;
    }

    public Task ConfirmBooking(Guid id, CancellationToken cancellationToken)
    {
        UpdateBookingStatus(id, BookingStatusDto.Confirmed);
        return Task.CompletedTask;
    }

    public Task CompleteBooking(Guid id, CancellationToken cancellationToken)
    {
        UpdateBookingStatus(id, BookingStatusDto.Completed);
        return Task.CompletedTask;
    }

    public Task DeleteBooking(Guid id, CancellationToken cancellationToken)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == id);
        if (booking is not null)
        {
            _bookings.Remove(booking);
        }

        return Task.CompletedTask;
    }

    public Task<Uri> RecordPayment(Guid bookingId, CreatePaymentDto dto, CancellationToken cancellationToken)
    {
        var paymentId = Guid.NewGuid();
        return Task.FromResult(new Uri($"/bookings/{bookingId}/payments/{paymentId}", UriKind.Relative));
    }

    public void AddBooking(GetBookingDto booking) => _bookings.Add(booking);

    public void SetGetBookingByIdException(Exception exception) => _getBookingByIdException = exception;

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
