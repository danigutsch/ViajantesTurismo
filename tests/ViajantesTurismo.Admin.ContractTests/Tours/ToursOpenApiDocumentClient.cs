using System.Text.Json;

namespace ViajantesTurismo.Admin.ContractTests.Tours;

/// <summary>
/// Reads the canonical tours OpenAPI artifact and maps the consumer-owned slice.
/// </summary>
internal static class ToursOpenApiDocumentClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Retrieves the subset of the canonical tours OpenAPI contract that this consumer relies on.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the file read.</param>
    /// <returns>The mapped tours OpenAPI contract slice.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the required tours contract shape is missing.</exception>
    public static async Task<ToursOpenApiContractDto> GetContract(CancellationToken cancellationToken)
        => await GetContract(GetCanonicalDocumentPath(), cancellationToken);

    /// <summary>
    /// Retrieves the subset of the build-time generated tours OpenAPI contract used for
    /// generated-vs-canonical compatibility validation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the file read.</param>
    /// <returns>The mapped tours OpenAPI contract slice from the generated artifact.</returns>
    public static async Task<ToursOpenApiContractDto> GetGeneratedContract(CancellationToken cancellationToken)
        => await GetContract(GetGeneratedDocumentPath(), cancellationToken);

    private static async Task<ToursOpenApiContractDto> GetContract(string documentPath, CancellationToken cancellationToken)
    {
        var document = await ReadDocument(documentPath, cancellationToken);

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

    private static async Task<ToursOpenApiDocumentDto?> ReadDocument(string documentPath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(documentPath);
        return await JsonSerializer.DeserializeAsync<ToursOpenApiDocumentDto>(stream, SerializerOptions, cancellationToken);
    }

    private static string GetCanonicalDocumentPath()
        => Path.Combine(GetRepositoryRoot(), "src", "ViajantesTurismo.Admin.Contracts", "OpenApi", "tours.openapi.json");

    private static string GetGeneratedDocumentPath()
        => Path.Combine(GetRepositoryRoot(), "src", "ViajantesTurismo.Admin.Contracts", "OpenApi", ".generated", "ViajantesTurismo.Admin.ApiService_tours.json");

    private static string GetRepositoryRoot()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            var candidatePath = Path.Combine(currentDirectory.FullName, "ViajantesTurismo.slnx");
            if (File.Exists(candidatePath))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root for contract test artifacts.");
    }
}
