using Microsoft.OpenApi;

namespace ViajantesTurismo.Admin.ContractTests;

internal static class AdminOpenApiDocumentRegistrationTestHelpers
{
    public static OpenApiSchema GetMultipartSchema(OpenApiDocument document, string path)
    {
        if (!document.Paths.TryGetValue(path, out var pathItem) || pathItem.Operations is null)
        {
            throw new InvalidOperationException($"Expected OpenAPI path '{path}' to exist.");
        }

        return !pathItem.Operations.TryGetValue(HttpMethod.Post, out var operation)
            ? throw new InvalidOperationException($"Expected POST operation for '{path}'.")
            : operation.RequestBody?.Content?["multipart/form-data"].Schema as OpenApiSchema
            ?? throw new InvalidOperationException($"Expected multipart schema for '{path}'.");
    }
}
