using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.IntegrationTests.Helpers;

/// <summary>
/// Helper methods for Bookings API operations in integration tests.
/// </summary>
internal static class BookingsApiHelper
{
    public static async Task<HttpResponseMessage> CreateBooking(
        this HttpClient client,
        CreateBookingDto request,
        CancellationToken cancellationToken)
    {
        return await client.PostAsJsonAsync(
            new Uri("/bookings", UriKind.Relative),
            request,
            cancellationToken);
    }

    public static async Task<GetBookingDto> CreateBookingAndRead(
        this HttpClient client,
        CreateBookingDto request,
        CancellationToken cancellationToken)
    {
        var response = await CreateBooking(client, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetBookingDto>(cancellationToken))!;
    }

    public static async Task<HttpResponseMessage> GetBooking(
        this HttpClient client,
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        return await client.GetAsync(
            new Uri($"/bookings/{bookingId}", UriKind.Relative),
            cancellationToken);
    }

    public static async Task<GetBookingDto> GetBookingAndRead(
        this HttpClient client,
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        var response = await GetBooking(client, bookingId, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetBookingDto>(cancellationToken))!;
    }

    public static async Task<HttpResponseMessage> GetAllBookingsAsync(
        this HttpClient client,
        CancellationToken cancellationToken)
    {
        return await client.GetAsync(
            new Uri("/bookings", UriKind.Relative),
            cancellationToken);
    }

    public static async Task<GetBookingDto[]> GetAllBookingsAndReadAsync(
        this HttpClient client,
        CancellationToken cancellationToken)
    {
        var response = await GetAllBookingsAsync(client, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetBookingDto[]>(cancellationToken))!;
    }

    public static async Task<HttpResponseMessage> GetBookingsByTour(
        this HttpClient client,
        Guid tourId,
        CancellationToken cancellationToken)
    {
        return await client.GetAsync(
            new Uri($"/bookings/tour/{tourId}", UriKind.Relative),
            cancellationToken);
    }

    public static async Task<GetBookingDto[]> GetBookingsByTourAndRead(
        this HttpClient client,
        Guid tourId,
        CancellationToken cancellationToken)
    {
        var response = await GetBookingsByTour(client, tourId, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetBookingDto[]>(cancellationToken))!;
    }

    public static async Task<HttpResponseMessage> GetBookingsByCustomer(
        this HttpClient client,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return await client.GetAsync(
            new Uri($"/bookings/customer/{customerId}", UriKind.Relative),
            cancellationToken);
    }

    public static async Task<GetBookingDto[]> GetBookingsByCustomerAndRead(
        this HttpClient client,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var response = await GetBookingsByCustomer(client, customerId, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetBookingDto[]>(cancellationToken))!;
    }

    public static async Task<HttpResponseMessage> RecordPayment(
        this HttpClient client,
        Guid bookingId,
        CreatePaymentDto payment,
        CancellationToken cancellationToken)
    {
        return await client.PostAsJsonAsync(
            new Uri($"/bookings/{bookingId}/payments", UriKind.Relative),
            payment,
            cancellationToken);
    }

    public static async Task<HttpResponseMessage> ConfirmBooking(
        this HttpClient client,
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        return await client.PostAsync(
            new Uri($"/bookings/{bookingId}/confirm", UriKind.Relative),
            null,
            cancellationToken);
    }

    public static async Task<HttpResponseMessage> CancelBooking(
        this HttpClient client,
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        return await client.PostAsync(
            new Uri($"/bookings/{bookingId}/cancel", UriKind.Relative),
            null,
            cancellationToken);
    }

    public static async Task<HttpResponseMessage> CompleteBooking(
        this HttpClient client,
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        return await client.PostAsync(
            new Uri($"/bookings/{bookingId}/complete", UriKind.Relative),
            null,
            cancellationToken);
    }

    public static async Task<HttpResponseMessage> UpdateBookingNotes(
        this HttpClient client,
        Guid bookingId,
        UpdateBookingNotesDto request,
        CancellationToken cancellationToken)
    {
        return await client.PatchAsJsonAsync(
            new Uri($"/bookings/{bookingId}/notes", UriKind.Relative),
            request,
            cancellationToken);
    }

    public static async Task<HttpResponseMessage> UpdateBookingDiscount(
        this HttpClient client,
        Guid bookingId,
        UpdateBookingDiscountDto request,
        CancellationToken cancellationToken)
    {
        return await client.PutAsJsonAsync(
            new Uri($"/bookings/{bookingId}/discount", UriKind.Relative),
            request,
            cancellationToken);
    }

    public static async Task<HttpResponseMessage> UpdateBookingDetails(
        this HttpClient client,
        Guid bookingId,
        UpdateBookingDetailsDto request,
        CancellationToken cancellationToken)
    {
        return await client.PutAsJsonAsync(
            new Uri($"/bookings/{bookingId}/details", UriKind.Relative),
            request,
            cancellationToken);
    }

    public static async Task<HttpResponseMessage> DeleteBooking(
        this HttpClient client,
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        return await client.DeleteAsync(
            new Uri($"/bookings/{bookingId}", UriKind.Relative),
            cancellationToken);
    }
}
