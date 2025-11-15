using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.IntegrationTests.Helpers;

/// <summary>
/// Helper methods for Bookings API operations in integration tests.
/// </summary>
internal static class BookingsApiHelper
{
    public static async Task<HttpResponseMessage> CreateBookingAsync(
        HttpClient client,
        CreateBookingDto request,
        CancellationToken cancellationToken = default)
    {
        return await client.PostAsJsonAsync(
            new Uri("/bookings", UriKind.Relative),
            request,
            cancellationToken);
    }

    public static async Task<GetBookingDto> CreateBookingAndReadAsync(
        HttpClient client,
        CreateBookingDto request,
        CancellationToken cancellationToken = default)
    {
        var response = await CreateBookingAsync(client, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetBookingDto>(cancellationToken))!;
    }

    public static async Task<HttpResponseMessage> GetBookingAsync(
        HttpClient client,
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        return await client.GetAsync(
            new Uri($"/bookings/{bookingId}", UriKind.Relative),
            cancellationToken);
    }

    public static async Task<GetBookingDto> GetBookingAndReadAsync(
        HttpClient client,
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var response = await GetBookingAsync(client, bookingId, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetBookingDto>(cancellationToken))!;
    }

    public static async Task<HttpResponseMessage> GetAllBookingsAsync(
        HttpClient client,
        CancellationToken cancellationToken = default)
    {
        return await client.GetAsync(
            new Uri("/bookings", UriKind.Relative),
            cancellationToken);
    }

    public static async Task<GetBookingDto[]> GetAllBookingsAndReadAsync(
        HttpClient client,
        CancellationToken cancellationToken = default)
    {
        var response = await GetAllBookingsAsync(client, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetBookingDto[]>(cancellationToken))!;
    }

    public static async Task<HttpResponseMessage> GetBookingsByTourAsync(
        HttpClient client,
        Guid tourId,
        CancellationToken cancellationToken = default)
    {
        return await client.GetAsync(
            new Uri($"/bookings/tour/{tourId}", UriKind.Relative),
            cancellationToken);
    }

    public static async Task<GetBookingDto[]> GetBookingsByTourAndReadAsync(
        HttpClient client,
        Guid tourId,
        CancellationToken cancellationToken = default)
    {
        var response = await GetBookingsByTourAsync(client, tourId, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetBookingDto[]>(cancellationToken))!;
    }

    public static async Task<HttpResponseMessage> GetBookingsByCustomerAsync(
        HttpClient client,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await client.GetAsync(
            new Uri($"/bookings/customer/{customerId}", UriKind.Relative),
            cancellationToken);
    }

    public static async Task<GetBookingDto[]> GetBookingsByCustomerAndReadAsync(
        HttpClient client,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var response = await GetBookingsByCustomerAsync(client, customerId, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetBookingDto[]>(cancellationToken))!;
    }

    public static async Task<HttpResponseMessage> RecordPaymentAsync(
        HttpClient client,
        Guid bookingId,
        CreatePaymentDto payment,
        CancellationToken cancellationToken = default)
    {
        return await client.PostAsJsonAsync(
            new Uri($"/bookings/{bookingId}/payments", UriKind.Relative),
            payment,
            cancellationToken);
    }

    public static async Task<HttpResponseMessage> ConfirmBookingAsync(
        HttpClient client,
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        return await client.PostAsync(
            new Uri($"/bookings/{bookingId}/confirm", UriKind.Relative),
            null,
            cancellationToken);
    }

    public static async Task<HttpResponseMessage> CancelBookingAsync(
        HttpClient client,
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        return await client.PostAsync(
            new Uri($"/bookings/{bookingId}/cancel", UriKind.Relative),
            null,
            cancellationToken);
    }

    public static async Task<HttpResponseMessage> CompleteBookingAsync(
        HttpClient client,
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        return await client.PostAsync(
            new Uri($"/bookings/{bookingId}/complete", UriKind.Relative),
            null,
            cancellationToken);
    }

    public static async Task<HttpResponseMessage> UpdateBookingNotesAsync(
        HttpClient client,
        Guid bookingId,
        UpdateBookingNotesDto request,
        CancellationToken cancellationToken = default)
    {
        return await client.PatchAsJsonAsync(
            new Uri($"/bookings/{bookingId}/notes", UriKind.Relative),
            request,
            cancellationToken);
    }

    public static async Task<HttpResponseMessage> UpdateBookingDiscountAsync(
        HttpClient client,
        Guid bookingId,
        UpdateBookingDiscountDto request,
        CancellationToken cancellationToken = default)
    {
        return await client.PutAsJsonAsync(
            new Uri($"/bookings/{bookingId}/discount", UriKind.Relative),
            request,
            cancellationToken);
    }

    public static async Task<HttpResponseMessage> UpdateBookingDetailsAsync(
        HttpClient client,
        Guid bookingId,
        UpdateBookingDetailsDto request,
        CancellationToken cancellationToken = default)
    {
        return await client.PutAsJsonAsync(
            new Uri($"/bookings/{bookingId}", UriKind.Relative),
            request,
            cancellationToken);
    }
}
