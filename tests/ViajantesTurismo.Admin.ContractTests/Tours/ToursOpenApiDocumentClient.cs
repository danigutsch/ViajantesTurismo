using System.Text.Json;

using ViajantesTurismo.Admin.ContractTests.Infrastructure;

namespace ViajantesTurismo.Admin.ContractTests.Tours;

/// <summary>
/// Reads the canonical tours OpenAPI artifact and maps the consumer-owned slice.
/// </summary>
internal static class ToursOpenApiDocumentClient
{
    private const string RoutePrefix = "/api/v1/tours";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Retrieves the subset of the canonical tours OpenAPI contract that this consumer relies on.
    /// </summary>
    /// <param name="ct">The cancellation token for the file read.</param>
    /// <returns>The mapped tours OpenAPI contract slice.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the required tours contract shape is missing.</exception>
    public static async Task<ToursOpenApiContractDto> GetContract(CancellationToken ct)
        => await GetContract(GetCanonicalDocumentPath(), ct);

    /// <summary>
    /// Retrieves the subset of the build-time generated tours OpenAPI contract used for
    /// generated-vs-canonical compatibility validation.
    /// </summary>
    /// <param name="ct">The cancellation token for the file read.</param>
    /// <returns>The mapped tours OpenAPI contract slice from the generated artifact.</returns>
    public static async Task<ToursOpenApiContractDto> GetGeneratedContract(CancellationToken ct)
        => await GetContract(GetGeneratedDocumentPath(), ct);

    private static async Task<ToursOpenApiContractDto> GetContract(string documentPath, CancellationToken ct)
    {
        var document = await ReadDocument(documentPath, ct);

        if (document?.Info is null || document.Paths is null)
        {
            throw new InvalidOperationException("The tours OpenAPI document is incomplete.");
        }

        if (!document.Paths.TryGetValue(RoutePrefix, out var toursPath) ||
            toursPath.Get is null ||
            toursPath.Post is null)
        {
            throw new InvalidOperationException("The tours collection path is missing required operations.");
        }

        if (!document.Paths.TryGetValue($"{RoutePrefix}/{{id}}", out var tourByIdPath) ||
            tourByIdPath.Get is null ||
            tourByIdPath.Put is null)
        {
            throw new InvalidOperationException("The tours item path is missing required operations.");
        }

        if (!document.Paths.TryGetValue($"{RoutePrefix}/options/search", out var optionsSearchPath) ||
            optionsSearchPath.Post is null)
        {
            throw new InvalidOperationException("The tours option search path is missing a required POST operation.");
        }

        var createTourSchemaReference = toursPath.Post.RequestBody?.Content?.ApplicationJson?.Schema?.Reference;
        var updateTourSchemaReference = tourByIdPath.Put.RequestBody?.Content?.ApplicationJson?.Schema?.Reference;
        var searchTourOptionsSchemaReference = optionsSearchPath.Post.RequestBody?.Content?.ApplicationJson?.Schema?.Reference;

        if (string.IsNullOrWhiteSpace(createTourSchemaReference) || string.IsNullOrWhiteSpace(updateTourSchemaReference) ||
            string.IsNullOrWhiteSpace(searchTourOptionsSchemaReference))
        {
            throw new InvalidOperationException("The tours contract is missing request schema references.");
        }

        return new ToursOpenApiContractDto(
            document.OpenApi,
            document.Info.Title,
            toursPath.Get.OperationId,
            tourByIdPath.Get.OperationId,
            createTourSchemaReference,
            updateTourSchemaReference,
            optionsSearchPath.Post.OperationId,
            searchTourOptionsSchemaReference);
    }

    private static async Task<ToursOpenApiDocumentDto?> ReadDocument(string documentPath, CancellationToken ct)
    {
        await using var stream = File.OpenRead(documentPath);
        return await JsonSerializer.DeserializeAsync<ToursOpenApiDocumentDto>(stream, SerializerOptions, ct);
    }

    private static string GetCanonicalDocumentPath()
        => Path.Combine(ContractTestRepository.RootPath, "src", "ViajantesTurismo.Admin.Contracts.Http", "OpenApi", "tours.openapi.json");

    private static string GetGeneratedDocumentPath()
        => Path.Combine(ContractTestRepository.RootPath, "src", "ViajantesTurismo.Admin.Contracts.Http", "OpenApi", ".generated", "ViajantesTurismo.Admin.Api_tours.json");
}
