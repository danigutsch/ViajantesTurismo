using System.Text.Json;

namespace ViajantesTurismo.Admin.ContractTests.Customers;

/// <summary>
/// Reads the canonical customers OpenAPI artifact and maps the consumer-owned slice.
/// </summary>
internal static class CustomersOpenApiDocumentClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Retrieves the subset of the canonical customers OpenAPI contract that this consumer relies on.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the file read.</param>
    /// <returns>The mapped customers OpenAPI contract slice.</returns>
    public static async Task<CustomersOpenApiContractDto> GetContract(CancellationToken cancellationToken)
        => await GetContract(GetCanonicalDocumentPath(), cancellationToken);

    /// <summary>
    /// Retrieves the subset of the build-time generated customers OpenAPI contract used for
    /// generated-vs-canonical compatibility validation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the file read.</param>
    /// <returns>The mapped customers OpenAPI contract slice from the generated artifact.</returns>
    public static async Task<CustomersOpenApiContractDto> GetGeneratedContract(CancellationToken cancellationToken)
        => await GetContract(GetGeneratedDocumentPath(), cancellationToken);

    private static async Task<CustomersOpenApiContractDto> GetContract(string documentPath, CancellationToken cancellationToken)
    {
        var document = await ReadDocument(documentPath, cancellationToken);

        if (document?.Info is null || document.Paths is null)
        {
            throw new InvalidOperationException("The customers OpenAPI document is incomplete.");
        }

        if (!document.Paths.TryGetValue("/customers", out var customersPath) ||
            customersPath.Get is null ||
            customersPath.Post is null)
        {
            throw new InvalidOperationException("The customers collection path is missing required operations.");
        }

        if (!document.Paths.TryGetValue("/customers/{id}", out var customerByIdPath) ||
            customerByIdPath.Get is null ||
            customerByIdPath.Put is null)
        {
            throw new InvalidOperationException("The customers item path is missing required operations.");
        }

        if (!document.Paths.TryGetValue("/customers/import", out var importPath) ||
            importPath.Post is null)
        {
            throw new InvalidOperationException("The customers import path is missing the required operation.");
        }

        if (!document.Paths.TryGetValue("/customers/import/commit", out var commitImportPath) ||
            commitImportPath.Post is null)
        {
            throw new InvalidOperationException("The customers import commit path is missing the required operation.");
        }

        var createCustomerSchemaReference = customersPath.Post.RequestBody?.Content?.ApplicationJson?.Schema?.Reference;
        var updateCustomerSchemaReference = customerByIdPath.Put.RequestBody?.Content?.ApplicationJson?.Schema?.Reference;
        var importCustomersSchemaReference = ExtractImportCustomersSchemaReference(importPath.Post.RequestBody?.Content?.MultipartFormData?.Schema);
        var commitImportSchemaToken = ExtractCommitImportSchemaToken(commitImportPath.Post.RequestBody?.Content?.MultipartFormData?.Schema);

        if (string.IsNullOrWhiteSpace(createCustomerSchemaReference) ||
            string.IsNullOrWhiteSpace(updateCustomerSchemaReference) ||
            string.IsNullOrWhiteSpace(importCustomersSchemaReference) ||
            string.IsNullOrWhiteSpace(commitImportSchemaToken))
        {
            throw new InvalidOperationException("The customers contract is missing request schema references.");
        }

        return new CustomersOpenApiContractDto(
            document.OpenApi,
            document.Info.Title,
            customersPath.Get.OperationId,
            customerByIdPath.Get.OperationId,
            createCustomerSchemaReference,
            updateCustomerSchemaReference,
            importPath.Post.OperationId,
            importCustomersSchemaReference,
            commitImportPath.Post.OperationId,
            commitImportSchemaToken);
    }

    private static string? ExtractImportCustomersSchemaReference(CustomersOpenApiSchemaDto? schema)
    {
        if (schema?.Reference is not null)
        {
            return schema.Reference;
        }

        if (schema?.Properties is not null &&
            schema.Properties.TryGetValue("file", out var fileProperty) &&
            !string.IsNullOrWhiteSpace(fileProperty.Reference))
        {
            return fileProperty.Reference;
        }

        return null;
    }

    private static string? ExtractCommitImportSchemaToken(CustomersOpenApiSchemaDto? schema)
    {
        if (schema?.AllOf is null || schema.AllOf.Count == 0)
        {
            return null;
        }

        var hasFilePart = schema.AllOf.Any(static item =>
            item.Properties is not null && item.Properties.ContainsKey("file"));
        var hasConflictResolutionPart = schema.AllOf.Any(static item =>
            item.Properties is not null && item.Properties.ContainsKey("conflictResolutions"));

        return hasFilePart && hasConflictResolutionPart
            ? "multipart-object-allOf:file+conflictResolutions"
            : null;
    }

    private static async Task<CustomersOpenApiDocumentDto?> ReadDocument(string documentPath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(documentPath);
        return await JsonSerializer.DeserializeAsync<CustomersOpenApiDocumentDto>(stream, SerializerOptions, cancellationToken);
    }

    private static string GetCanonicalDocumentPath()
        => Path.Combine(GetRepositoryRoot(), "src", "ViajantesTurismo.Admin.Contracts", "OpenApi", "customers.openapi.json");

    private static string GetGeneratedDocumentPath()
        => Path.Combine(GetRepositoryRoot(), "src", "ViajantesTurismo.Admin.Contracts", "OpenApi", ".generated", "ViajantesTurismo.Admin.ApiService_customers.json");

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
