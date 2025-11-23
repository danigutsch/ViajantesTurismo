using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Tests.Shared;

public sealed class FakeBookingsApiClient : IBookingsApiClient
{
    private readonly List<GetBookingDto> _bookings = [];
    private Exception? _getAllBookingsException;
    private Exception? _getBookingByIdException;
    private Exception? _getBookingsByCustomerIdException;
    private Exception? _getBookingsByTourIdException;

    public Task<GetBookingDto[]> GetAllBookings(CancellationToken cancellationToken)
    {
        if (_getAllBookingsException is not null)
        {
            throw _getAllBookingsException;
        }

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
        if (_getBookingsByTourIdException is not null)
        {
            throw _getBookingsByTourIdException;
        }

        return Task.FromResult(_bookings.Where(b => b.TourId == tourId).ToArray());
    }

    public Task<GetBookingDto[]> GetBookingsByCustomerId(Guid customerId, CancellationToken cancellationToken)
    {
        if (_getBookingsByCustomerIdException is not null)
        {
            throw _getBookingsByCustomerIdException;
        }

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
        return Task.CompletedTask;
    }

    public Task CancelBooking(Guid id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task ConfirmBooking(Guid id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task CompleteBooking(Guid id, CancellationToken cancellationToken)
    {
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

    public void SetGetAllBookingsException(Exception exception) => _getAllBookingsException = exception;

    public void SetGetBookingByIdException(Exception exception) => _getBookingByIdException = exception;

    public void SetGetBookingsByTourIdException(Exception exception) => _getBookingsByTourIdException = exception;

    public void SetGetBookingsByCustomerIdException(Exception exception) => _getBookingsByCustomerIdException = exception;
}
