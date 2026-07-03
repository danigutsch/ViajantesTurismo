using System.Net;
using System.Net.Http.Json;
using SharedKernel.HttpClients;

namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// HTTP client for the Admin bookings API.
/// </summary>
public sealed class BookingsApiClient(HttpClient httpClient) : IBookingsApiClient
{
    private static readonly BookingsApiClientJsonContext Json = BookingsApiClientJsonContext.Default;

    /// <inheritdoc />
    public async Task<GetBookingDto[]> GetAllBookings(CancellationToken ct) =>
        await ReadBookings("/bookings", ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<GetBookingDto?> GetBookingById(Guid id, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(new Uri($"/bookings/{id}", UriKind.Relative), ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(Json.GetBookingDto, ct).ConfigureAwait(false)
               ?? throw new InvalidOperationException("The booking response body was empty.");
    }

    /// <inheritdoc />
    public async Task<GetBookingDto[]> GetBookingsByTourId(Guid tourId, CancellationToken ct) =>
        await ReadBookings($"/bookings/tour/{tourId}", ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<GetBookingDto[]> GetBookingsByCustomerId(Guid customerId, CancellationToken ct) =>
        await ReadBookings($"/bookings/customer/{customerId}", ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Uri> CreateBooking(CreateBookingDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var response = await httpClient.PostAsJsonAsync(new Uri("/bookings", UriKind.Relative), dto, Json.CreateBookingDto, ct).ConfigureAwait(false);
        await EnsureSuccess(response, ct).ConfigureAwait(false);

        return response.Headers.Location ?? throw new InvalidOperationException("The Location header is missing in the response.");
    }

    /// <inheritdoc />
    public async Task UpdateBookingDiscount(Guid id, UpdateBookingDiscountDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var response = await httpClient.PutAsJsonAsync($"/bookings/{id}/discount", dto, Json.UpdateBookingDiscountDto, ct).ConfigureAwait(false);
        await EnsureSuccess(response, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateBookingDetails(Guid id, UpdateBookingDetailsDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var response = await httpClient.PutAsJsonAsync($"/bookings/{id}/details", dto, Json.UpdateBookingDetailsDto, ct).ConfigureAwait(false);
        await EnsureSuccess(response, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateBookingNotes(Guid id, UpdateBookingNotesDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var response = await httpClient.PatchAsJsonAsync($"/bookings/{id}/notes", dto, Json.UpdateBookingNotesDto, ct).ConfigureAwait(false);
        await EnsureSuccess(response, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CancelBooking(Guid id, CancellationToken ct) =>
        await PostCommand($"/bookings/{id}/cancel", ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task ConfirmBooking(Guid id, CancellationToken ct) =>
        await PostCommand($"/bookings/{id}/confirm", ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task CompleteBooking(Guid id, CancellationToken ct) =>
        await PostCommand($"/bookings/{id}/complete", ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task DeleteBooking(Guid id, CancellationToken ct)
    {
        using var response = await httpClient.DeleteAsync(new Uri($"/bookings/{id}", UriKind.Relative), ct).ConfigureAwait(false);
        await EnsureSuccess(response, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Uri> RecordPayment(Guid bookingId, CreatePaymentDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var response = await httpClient.PostAsJsonAsync($"/bookings/{bookingId}/payments", dto, Json.CreatePaymentDto, ct).ConfigureAwait(false);
        await EnsureSuccess(response, ct).ConfigureAwait(false);

        return response.Headers.Location ?? throw new InvalidOperationException("The Location header is missing in the response.");
    }

    private async Task<GetBookingDto[]> ReadBookings(string requestUri, CancellationToken ct)
    {
        List<GetBookingDto>? bookings = null;

        await foreach (var booking in httpClient.GetFromJsonAsAsyncEnumerable(requestUri, Json.GetBookingDto, ct).ConfigureAwait(false))
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

    private async Task PostCommand(string requestUri, CancellationToken ct)
    {
        using var response = await httpClient.PostAsync(new Uri(requestUri, UriKind.Relative), null, ct).ConfigureAwait(false);
        await EnsureSuccess(response, ct).ConfigureAwait(false);
    }

    private static async Task EnsureSuccess(HttpResponseMessage response, CancellationToken ct) =>
        await ContractHttpValidation.EnsureSuccessOrThrowValidationException(response, Json.ContractValidationProblemDto, ct).ConfigureAwait(false);
}
