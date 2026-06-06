using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Testing.Integration.Helpers;

/// <summary>
/// Helper methods for Tours API operations in integration tests.
/// </summary>
public static class ToursApiHelper
{
    public static async Task<HttpResponseMessage> CreateTour(
        this HttpClient client,
        CreateTourDto request,
        CancellationToken cancellationToken)
    {
        return await client.PostAsJsonAsync(
            new Uri("/tours", UriKind.Relative),
            request,
            cancellationToken);
    }

    public static async Task<HttpResponseMessage> GetAllTours(
        this HttpClient client,
        CancellationToken cancellationToken)
    {
        return await client.GetAsync(
            new Uri("/tours", UriKind.Relative),
            cancellationToken);
    }
}
