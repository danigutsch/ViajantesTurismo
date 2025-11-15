using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.IntegrationTests.Helpers;

/// <summary>
/// Helper methods for Tours API operations in integration tests.
/// </summary>
internal static class ToursApiHelper
{
    public static async Task<HttpResponseMessage> CreateTourAsync(
        HttpClient client,
        CreateTourDto request,
        CancellationToken cancellationToken = default)
    {
        return await client.PostAsJsonAsync(
            new Uri("/tours", UriKind.Relative),
            request,
            cancellationToken);
    }

    public static async Task<GetTourDto> CreateTourAndReadAsync(
        HttpClient client,
        CreateTourDto request,
        CancellationToken cancellationToken = default)
    {
        var response = await CreateTourAsync(client, request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var location = response.Headers.Location;
        var getResponse = await client.GetAsync(location, cancellationToken);
        return (await getResponse.Content.ReadFromJsonAsync<GetTourDto>(cancellationToken))!;
    }

    public static async Task<HttpResponseMessage> GetTourAsync(
        HttpClient client,
        Guid tourId,
        CancellationToken cancellationToken = default)
    {
        return await client.GetAsync(
            new Uri($"/tours/{tourId}", UriKind.Relative),
            cancellationToken);
    }

    public static async Task<GetTourDto> GetTourAndReadAsync(
        HttpClient client,
        Guid tourId,
        CancellationToken cancellationToken = default)
    {
        var response = await GetTourAsync(client, tourId, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetTourDto>(cancellationToken))!;
    }

    public static async Task<HttpResponseMessage> GetAllToursAsync(
        HttpClient client,
        CancellationToken cancellationToken = default)
    {
        return await client.GetAsync(
            new Uri("/tours", UriKind.Relative),
            cancellationToken);
    }

    public static async Task<GetTourDto[]> GetAllToursAndReadAsync(
        HttpClient client,
        CancellationToken cancellationToken = default)
    {
        var response = await GetAllToursAsync(client, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetTourDto[]>(cancellationToken))!;
    }
}
