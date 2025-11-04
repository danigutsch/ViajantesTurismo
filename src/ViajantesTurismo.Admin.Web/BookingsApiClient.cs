using ViajantesTurismo.AdminApi.Contracts;

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
        response.EnsureSuccessStatusCode();

        return response.Headers.Location ?? throw new InvalidOperationException("The Location header is missing in the response.");
    }

    public async Task UpdateBooking(long id, UpdateBookingDto dto, CancellationToken cancellationToken)
    {
        var response = await httpClient.PutAsJsonAsync($"/bookings/{id}", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateBookingNotes(long id, UpdateBookingNotesDto dto, CancellationToken cancellationToken)
    {
        var response = await httpClient.PatchAsJsonAsync($"/bookings/{id}/notes", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task CancelBooking(long id, CancellationToken cancellationToken)
    {
        var response = await httpClient.PatchAsync(new Uri($"/bookings/{id}/cancel", UriKind.Relative), null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task ConfirmBooking(long id, CancellationToken cancellationToken)
    {
        var response = await httpClient.PatchAsync(new Uri($"/bookings/{id}/confirm", UriKind.Relative), null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task CompleteBooking(long id, CancellationToken cancellationToken)
    {
        var response = await httpClient.PatchAsync(new Uri($"/bookings/{id}/complete", UriKind.Relative), null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteBooking(long id, CancellationToken cancellationToken)
    {
        var response = await httpClient.DeleteAsync(new Uri($"/bookings/{id}", UriKind.Relative), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<Uri> RecordPayment(long bookingId, CreatePaymentDto dto, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync($"/bookings/{bookingId}/payments", dto, cancellationToken);
        response.EnsureSuccessStatusCode();

        return response.Headers.Location ?? throw new InvalidOperationException("The Location header is missing in the response.");
    }
}