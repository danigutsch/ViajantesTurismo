using System.Net.Http.Json;

namespace ViajantesTurismo.Admin.ContractTests.Tours;

/// <summary>
/// Reads the published tours OpenAPI document through HTTP and maps the consumer-owned slice.
/// </summary>
internal sealed class ToursOpenApiDocumentClient(HttpClient httpClient)
{
    /// <summary>
    /// Retrieves the subset of the tours OpenAPI contract that this consumer relies on.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the HTTP request.</param>
    /// <returns>The mapped tours OpenAPI contract slice.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the required tours contract shape is missing.</exception>
    public async Task<ToursOpenApiContractDto> GetContract(CancellationToken cancellationToken)
    {
        var document = await httpClient.GetFromJsonAsync<ToursOpenApiDocumentDto>(
            "/openapi/Tours.json",
            cancellationToken);

        if (document?.Info is null || document.Paths is null)
        {
            throw new InvalidOperationException("The tours OpenAPI document is incomplete.");
        }

        if (!document.Paths.TryGetValue("/tours", out var toursPath) ||
            toursPath.Get is null ||
            toursPath.Post is null)
        {
            throw new InvalidOperationException("The tours collection path is missing required operations.");
        }

        if (!document.Paths.TryGetValue("/tours/{id}", out var tourByIdPath) ||
            tourByIdPath.Get is null ||
            tourByIdPath.Put is null)
        {
            throw new InvalidOperationException("The tours item path is missing required operations.");
        }

        var createTourSchemaReference = toursPath.Post.RequestBody?.Content?.ApplicationJson?.Schema?.Reference;
        var updateTourSchemaReference = tourByIdPath.Put.RequestBody?.Content?.ApplicationJson?.Schema?.Reference;

        if (string.IsNullOrWhiteSpace(createTourSchemaReference) || string.IsNullOrWhiteSpace(updateTourSchemaReference))
        {
            throw new InvalidOperationException("The tours contract is missing request schema references.");
        }

        return new ToursOpenApiContractDto(
            document.OpenApi,
            document.Info.Title,
            toursPath.Get.OperationId,
            tourByIdPath.Get.OperationId,
            createTourSchemaReference,
            updateTourSchemaReference);
    }
}
