using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Web;

internal sealed class ToursApiClient(HttpClient httpClient)
{
    public async Task<GetTourDto[]> GetTours(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        List<GetTourDto>? tours = null;

        await foreach (var tour in httpClient.GetFromJsonAsAsyncEnumerable<GetTourDto>("/tours", cancellationToken))
        {
            if (tours?.Count >= maxItems)
            {
                break;
            }

            if (tour is null)
            {
                continue;
            }

            tours ??= [];
            tours.Add(tour);
        }

        return tours?.ToArray() ?? [];
    }

    public async Task<Uri> CreateTour(CreateTourDto dto, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), dto, cancellationToken);
        response.EnsureSuccessStatusCode();

        return response.Headers.Location ?? throw new InvalidOperationException("The Location header is missing in the response.");
    }
}
