using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Testing.Integration.Helpers;

/// <summary>
/// Helper methods for Bookings API operations in integration tests.
/// </summary>
public static class BookingsApiHelper
{
    private const string RoutePrefix = "/api/v1/bookings";

    public static async Task<HttpResponseMessage> CreateBooking(
        this HttpClient client,
        CreateBookingDto request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(request);

        return await client.PostAsJsonAsync(
            new Uri(RoutePrefix, UriKind.Relative),
            request,
            ct);
    }

    public static async Task<GetBookingDto> CreateBookingAndRead(
        this HttpClient client,
        CreateBookingDto request,
        CancellationToken ct)
    {
        var response = await CreateBooking(client, request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetBookingDto>(ct))!;
    }

    public static async Task<HttpResponseMessage> GetBooking(
        this HttpClient client,
        Guid bookingId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);

        return await client.GetAsync(
            new Uri($"{RoutePrefix}/{bookingId}", UriKind.Relative),
            ct);
    }

    public static async Task<GetBookingDto> GetBookingAndRead(
        this HttpClient client,
        Guid bookingId,
        CancellationToken ct)
    {
        var response = await GetBooking(client, bookingId, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetBookingDto>(ct))!;
    }

    public static async Task<HttpResponseMessage> GetAllBookings(
        this HttpClient client,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);

        return await client.GetAsync(
            new Uri(RoutePrefix, UriKind.Relative),
            ct);
    }

    public static async Task<GetBookingDto[]> GetAllBookingsAndRead(
        this HttpClient client,
        CancellationToken ct)
    {
        var response = await GetAllBookings(client, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetBookingDto[]>(ct))!;
    }

    public static async Task<HttpResponseMessage> GetBookingsByTour(
        this HttpClient client,
        Guid tourId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);

        return await client.GetAsync(
            new Uri($"{RoutePrefix}/tour/{tourId}", UriKind.Relative),
            ct);
    }

    public static async Task<GetBookingDto[]> GetBookingsByTourAndRead(
        this HttpClient client,
        Guid tourId,
        CancellationToken ct)
    {
        var response = await GetBookingsByTour(client, tourId, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetBookingDto[]>(ct))!;
    }

    public static async Task<HttpResponseMessage> GetBookingsByCustomer(
        this HttpClient client,
        Guid customerId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);

        return await client.GetAsync(
            new Uri($"{RoutePrefix}/customer/{customerId}", UriKind.Relative),
            ct);
    }

    public static async Task<GetBookingDto[]> GetBookingsByCustomerAndRead(
        this HttpClient client,
        Guid customerId,
        CancellationToken ct)
    {
        var response = await GetBookingsByCustomer(client, customerId, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetBookingDto[]>(ct))!;
    }

    public static async Task<HttpResponseMessage> ConfirmBooking(
        this HttpClient client,
        Guid bookingId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);

        return await client.PostAsync(
            new Uri($"{RoutePrefix}/{bookingId}/confirm", UriKind.Relative),
            null,
            ct);
    }

    public static async Task<HttpResponseMessage> CancelBooking(
        this HttpClient client,
        Guid bookingId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);

        return await client.PostAsync(
            new Uri($"{RoutePrefix}/{bookingId}/cancel", UriKind.Relative),
            null,
            ct);
    }

    public static async Task<HttpResponseMessage> CompleteBooking(
        this HttpClient client,
        Guid bookingId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);

        return await client.PostAsync(
            new Uri($"{RoutePrefix}/{bookingId}/complete", UriKind.Relative),
            null,
            ct);
    }

    public static async Task<HttpResponseMessage> UpdateBookingNotes(
        this HttpClient client,
        Guid bookingId,
        UpdateBookingNotesDto request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(request);

        return await client.PatchAsJsonAsync(
            new Uri($"{RoutePrefix}/{bookingId}/notes", UriKind.Relative),
            request,
            ct);
    }

    public static async Task<HttpResponseMessage> UpdateBookingDiscount(
        this HttpClient client,
        Guid bookingId,
        UpdateBookingDiscountDto request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(request);

        return await client.PutAsJsonAsync(
            new Uri($"{RoutePrefix}/{bookingId}/discount", UriKind.Relative),
            request,
            ct);
    }

    public static async Task<HttpResponseMessage> UpdateBookingDetails(
        this HttpClient client,
        Guid bookingId,
        UpdateBookingDetailsDto request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(request);

        return await client.PutAsJsonAsync(
            new Uri($"{RoutePrefix}/{bookingId}/details", UriKind.Relative),
            request,
            ct);
    }

    public static async Task<HttpResponseMessage> DeleteBooking(
        this HttpClient client,
        Guid bookingId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);

        return await client.DeleteAsync(
            new Uri($"{RoutePrefix}/{bookingId}", UriKind.Relative),
            ct);
    }
}
