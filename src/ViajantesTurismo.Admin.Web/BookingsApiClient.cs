using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Web.Helpers;

namespace ViajantesTurismo.Admin.Web;

internal sealed class BookingsApiClient(HttpClient httpClient)
{
    public async Task<GetBookingDto[]> GetAllBookings(CancellationToken cancellationToken)
    {
        List<GetBookingDto>? bookings = null;

        await foreach (var booking in httpClient.GetFromJsonAsAsyncEnumerable<GetBookingDto>("/bookings", cancellationToken))
        {
            if (booking is null)
            {
                continue;
            }

            bookings ??= [];
            bookings.Add(booking);
        }

        return bookings?.ToArray() ?? [];
    }

    public async Task<GetBookingDto?> GetBookingById(long id, CancellationToken cancellationToken)
    {
        return await httpClient.GetFromJsonAsync<GetBookingDto>($"/bookings/{id}", cancellationToken);
    }

    public async Task<GetBookingDto[]> GetBookingsByTourId(int tourId, CancellationToken cancellationToken)
    {
        List<GetBookingDto>? bookings = null;

        await foreach (var booking in httpClient.GetFromJsonAsAsyncEnumerable<GetBookingDto>($"/bookings/tour/{tourId}", cancellationToken))
        {
            if (booking is null)
            {
                continue;
            }

            bookings ??= [];
            bookings.Add(booking);
        }

        return bookings?.ToArray() ?? [];
    }

    public async Task<GetBookingDto[]> GetBookingsByCustomerId(int customerId, CancellationToken cancellationToken)
    {
        List<GetBookingDto>? bookings = null;

        await foreach (var booking in httpClient.GetFromJsonAsAsyncEnumerable<GetBookingDto>($"/bookings/customer/{customerId}", cancellationToken))
        {
            if (booking is null)
            {
                continue;
            }

            bookings ??= [];
            bookings.Add(booking);
        }

        return bookings?.ToArray() ?? [];
    }

    public async Task<Uri> CreateBooking(CreateBookingDto dto, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(new Uri("/bookings", UriKind.Relative), dto, cancellationToken);
        await ValidationErrorHelper.EnsureSuccessOrThrowValidationException(response);

        return response.Headers.Location ?? throw new InvalidOperationException("The Location header is missing in the response.");
    }

    public async Task UpdateBookingDiscount(long id, UpdateBookingDiscountDto dto, CancellationToken cancellationToken)
    {
        var response = await httpClient.PutAsJsonAsync($"/bookings/{id}/discount", dto, cancellationToken);
        await ValidationErrorHelper.EnsureSuccessOrThrowValidationException(response);
    }

    public async Task UpdateBookingDetails(long id, UpdateBookingDetailsDto dto, CancellationToken cancellationToken)
    {
        var response = await httpClient.PutAsJsonAsync($"/bookings/{id}/details", dto, cancellationToken);
        await ValidationErrorHelper.EnsureSuccessOrThrowValidationException(response);
    }

    public async Task UpdateBookingNotes(long id, UpdateBookingNotesDto dto, CancellationToken cancellationToken)
    {
        var response = await httpClient.PatchAsJsonAsync($"/bookings/{id}/notes", dto, cancellationToken);
        await ValidationErrorHelper.EnsureSuccessOrThrowValidationException(response);
    }

    public async Task CancelBooking(long id, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsync(new Uri($"/bookings/{id}/cancel", UriKind.Relative), null, cancellationToken);
        await ValidationErrorHelper.EnsureSuccessOrThrowValidationException(response);
    }

    public async Task ConfirmBooking(long id, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsync(new Uri($"/bookings/{id}/confirm", UriKind.Relative), null, cancellationToken);
        await ValidationErrorHelper.EnsureSuccessOrThrowValidationException(response);
    }

    public async Task CompleteBooking(long id, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsync(new Uri($"/bookings/{id}/complete", UriKind.Relative), null, cancellationToken);
        await ValidationErrorHelper.EnsureSuccessOrThrowValidationException(response);
    }

    public async Task DeleteBooking(long id, CancellationToken cancellationToken)
    {
        var response = await httpClient.DeleteAsync(new Uri($"/bookings/{id}", UriKind.Relative), cancellationToken);
        await ValidationErrorHelper.EnsureSuccessOrThrowValidationException(response);
    }

    public async Task<Uri> RecordPayment(long bookingId, CreatePaymentDto dto, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync($"/bookings/{bookingId}/payments", dto, cancellationToken);
        await ValidationErrorHelper.EnsureSuccessOrThrowValidationException(response);

        return response.Headers.Location ?? throw new InvalidOperationException("The Location header is missing in the response.");
    }
}