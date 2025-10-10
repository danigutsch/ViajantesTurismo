namespace ViajantesTurismo.Web;

internal sealed class ToursApiClient(HttpClient httpClient)
{
    public async Task<Tour[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        List<Tour>? tours = null;

        await foreach (var tour in httpClient.GetFromJsonAsAsyncEnumerable<Tour>("/tours", cancellationToken))
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
}

internal sealed record Tour(string Name, DateTime StartDate, DateTime EndDate);
