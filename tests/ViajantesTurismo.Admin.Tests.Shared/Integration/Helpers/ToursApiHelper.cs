using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Tests.Shared.Integration.Helpers;

/// <summary>
/// Helper methods for Tours API operations in integration tests.
/// </summary>
public static class ToursApiHelper
{
    public static async Task<HttpResponseMessage> CreateTourAsync(
        this HttpClient client,
        CreateTourDto request,
        CancellationToken cancellationToken)
    {
        return await client.PostAsJsonAsync(
            new Uri("/tours", UriKind.Relative),
            request,
            cancellationToken);
    }

    public static async Task<GetTourDto> CreateTourAndReadAsync(
        this HttpClient client,
        CreateTourDto request,
        CancellationToken cancellationToken)
    {
        var response = await CreateTourAsync(client, request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var location = response.Headers.Location;
        var getResponse = await client.GetAsync(location, cancellationToken);
        return (await getResponse.Content.ReadFromJsonAsync<GetTourDto>(cancellationToken))!;
    }

    public static async Task<HttpResponseMessage> GetTourAsync(
        this HttpClient client,
        Guid tourId,
        CancellationToken cancellationToken)
    {
        return await client.GetAsync(
            new Uri($"/tours/{tourId}", UriKind.Relative),
            cancellationToken);
    }

    public static async Task<GetTourDto> GetTourAndReadAsync(
        this HttpClient client,
        Guid tourId,
        CancellationToken cancellationToken)
    {
        var response = await GetTourAsync(client, tourId, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetTourDto>(cancellationToken))!;
    }

    public static async Task<HttpResponseMessage> GetAllToursAsync(
        this HttpClient client,
        CancellationToken cancellationToken)
    {
        return await client.GetAsync(
            new Uri("/tours", UriKind.Relative),
            cancellationToken);
    }

    public static async Task<GetTourDto[]> GetAllToursAndReadAsync(
        this HttpClient client,
        CancellationToken cancellationToken)
    {
        var response = await GetAllToursAsync(client, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetTourDto[]>(cancellationToken))!;
    }
}
