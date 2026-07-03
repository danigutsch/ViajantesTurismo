using System.Net;
using System.Net.Http.Json;
using SharedKernel.HttpClients;

namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// HTTP client for the Admin tours API.
/// </summary>
public sealed class ToursApiClient(HttpClient httpClient) : IToursApiClient
{
    private static readonly ToursApiClientJsonContext Json = ToursApiClientJsonContext.Default;

    /// <inheritdoc />
    public async Task<GetTourDto[]> GetTours(CancellationToken cancellationToken, int maxItems = int.MaxValue)
    {
        if (maxItems <= 0)
        {
            return [];
        }

        List<GetTourDto>? tours = null;

        await foreach (var tour in httpClient.GetFromJsonAsAsyncEnumerable("/tours", Json.GetTourDto, cancellationToken).ConfigureAwait(false))
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

    /// <inheritdoc />
    public async Task<GetTourDto?> GetTourById(Guid id, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(new Uri($"/tours/{id}", UriKind.Relative), cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(Json.GetTourDto, cancellationToken).ConfigureAwait(false)
               ?? throw new InvalidOperationException("The tour response body was empty.");
    }

    /// <inheritdoc />
    public async Task<Uri> CreateTour(CreateTourDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var response = await httpClient.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), dto, Json.CreateTourDto, cancellationToken).ConfigureAwait(false);
        await ContractHttpValidation.EnsureSuccessOrThrowValidationException(response, Json.ContractValidationProblemDto, cancellationToken).ConfigureAwait(false);

        return response.Headers.Location ?? throw new InvalidOperationException("The Location header is missing in the response.");
    }

    /// <inheritdoc />
    public async Task UpdateTour(Guid id, UpdateTourDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var response = await httpClient.PutAsJsonAsync($"/tours/{id}", dto, Json.UpdateTourDto, cancellationToken).ConfigureAwait(false);
        await ContractHttpValidation.EnsureSuccessOrThrowValidationException(response, Json.ContractValidationProblemDto, cancellationToken).ConfigureAwait(false);
    }
}
