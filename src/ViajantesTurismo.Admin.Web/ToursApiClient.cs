using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Web.Helpers;

namespace ViajantesTurismo.Admin.Web;

internal sealed class ToursApiClient(HttpClient httpClient) : IToursApiClient
{
    public async Task<GetTourDto[]> GetTours(CancellationToken cancellationToken, int maxItems = 10)
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

    public async Task<GetTourDto?> GetTourById(Guid id, CancellationToken cancellationToken)
    {
        return await httpClient.GetFromJsonAsync<GetTourDto>($"/tours/{id}", cancellationToken);
    }

    public async Task<Uri> CreateTour(CreateTourDto dto, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), dto, cancellationToken);
        await ValidationErrorHelper.EnsureSuccessOrThrowValidationException(response);

        return response.Headers.Location ??
               throw new InvalidOperationException("The Location header is missing in the response.");
    }

    public async Task UpdateTour(Guid id, UpdateTourDto dto, CancellationToken cancellationToken)
    {
        var response = await httpClient.PutAsJsonAsync($"/tours/{id}", dto, cancellationToken);
        await ValidationErrorHelper.EnsureSuccessOrThrowValidationException(response);
    }
}
