using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Common.Contracts;

namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// HTTP client for the Admin bookings API.
/// </summary>
public sealed class BookingsApiClient(HttpClient httpClient) : IBookingsApiClient
{
    private static readonly BookingsApiClientJsonContext Json = BookingsApiClientJsonContext.Default;

    /// <inheritdoc />
    public async Task<GetBookingDto[]> GetAllBookings(CancellationToken cancellationToken) =>
        await ReadBookings("/bookings", cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<GetBookingDto?> GetBookingById(Guid id, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(new Uri($"/bookings/{id}", UriKind.Relative), cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(Json.GetBookingDto, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<GetBookingDto[]> GetBookingsByTourId(Guid tourId, CancellationToken cancellationToken) =>
        await ReadBookings($"/bookings/tour/{tourId}", cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<GetBookingDto[]> GetBookingsByCustomerId(Guid customerId, CancellationToken cancellationToken) =>
        await ReadBookings($"/bookings/customer/{customerId}", cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Uri> CreateBooking(CreateBookingDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var response = await httpClient.PostAsJsonAsync(new Uri("/bookings", UriKind.Relative), dto, Json.CreateBookingDto, cancellationToken).ConfigureAwait(false);
        await EnsureSuccess(response, cancellationToken).ConfigureAwait(false);

        return response.Headers.Location ?? throw new InvalidOperationException("The Location header is missing in the response.");
    }

    /// <inheritdoc />
    public async Task UpdateBookingDiscount(Guid id, UpdateBookingDiscountDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var response = await httpClient.PutAsJsonAsync($"/bookings/{id}/discount", dto, Json.UpdateBookingDiscountDto, cancellationToken).ConfigureAwait(false);
        await EnsureSuccess(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateBookingDetails(Guid id, UpdateBookingDetailsDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var response = await httpClient.PutAsJsonAsync($"/bookings/{id}/details", dto, Json.UpdateBookingDetailsDto, cancellationToken).ConfigureAwait(false);
        await EnsureSuccess(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateBookingNotes(Guid id, UpdateBookingNotesDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var response = await httpClient.PatchAsJsonAsync($"/bookings/{id}/notes", dto, Json.UpdateBookingNotesDto, cancellationToken).ConfigureAwait(false);
        await EnsureSuccess(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CancelBooking(Guid id, CancellationToken cancellationToken) =>
        await PostCommand($"/bookings/{id}/cancel", cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task ConfirmBooking(Guid id, CancellationToken cancellationToken) =>
        await PostCommand($"/bookings/{id}/confirm", cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task CompleteBooking(Guid id, CancellationToken cancellationToken) =>
        await PostCommand($"/bookings/{id}/complete", cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task DeleteBooking(Guid id, CancellationToken cancellationToken)
    {
        var response = await httpClient.DeleteAsync(new Uri($"/bookings/{id}", UriKind.Relative), cancellationToken).ConfigureAwait(false);
        await EnsureSuccess(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Uri> RecordPayment(Guid bookingId, CreatePaymentDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var response = await httpClient.PostAsJsonAsync($"/bookings/{bookingId}/payments", dto, Json.CreatePaymentDto, cancellationToken).ConfigureAwait(false);
        await EnsureSuccess(response, cancellationToken).ConfigureAwait(false);

        return response.Headers.Location ?? throw new InvalidOperationException("The Location header is missing in the response.");
    }

    private async Task<GetBookingDto[]> ReadBookings(string requestUri, CancellationToken cancellationToken)
    {
        List<GetBookingDto>? bookings = null;

        await foreach (var booking in httpClient.GetFromJsonAsAsyncEnumerable(requestUri, Json.GetBookingDto, cancellationToken).ConfigureAwait(false))
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

    private async Task PostCommand(string requestUri, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsync(new Uri(requestUri, UriKind.Relative), null, cancellationToken).ConfigureAwait(false);
        await EnsureSuccess(response, cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureSuccess(HttpResponseMessage response, CancellationToken cancellationToken) =>
        await ContractHttpValidation.EnsureSuccessOrThrowValidationException(response, Json.ContractValidationProblemDto, cancellationToken).ConfigureAwait(false);
}
