using Microsoft.OpenApi;
using Xunit;

namespace ViajantesTurismo.Admin.ContractTests;

/// <summary>
/// Verifies that Admin named OpenAPI documents expose the intended boundary slices.
/// </summary>
public sealed class AdminOpenApiDocumentRegistrationTests
{
    [Fact]
    public async Task Generates_A_Tours_Document_Containing_Only_Tours_Paths()
    {
        var document = await AdminOpenApiDocumentFactory.CreateDocument(
            "tours",
            TestContext.Current.CancellationToken,
            "MapToursEndpoints",
            "MapCustomerEndpoints",
            "MapBookingEndpoints");

        Assert.Contains("/tours", document.Paths.Keys);
        Assert.Contains("/tours/{id}", document.Paths.Keys);
        Assert.DoesNotContain("/customers", document.Paths.Keys);
        Assert.DoesNotContain("/bookings", document.Paths.Keys);
    }

    [Fact]
    public async Task Generates_A_Customers_Document_Including_Import_Paths()
    {
        var document = await AdminOpenApiDocumentFactory.CreateDocument(
            "customers",
            TestContext.Current.CancellationToken,
            "MapCustomerEndpoints",
            "MapCustomerImportEndpoints",
            "MapToursEndpoints");

        Assert.Contains("/customers", document.Paths.Keys);
        Assert.Contains("/customers/{id}", document.Paths.Keys);
        Assert.Contains("/customers/import", document.Paths.Keys);
        Assert.Contains("/customers/import/commit", document.Paths.Keys);
        Assert.DoesNotContain("/tours", document.Paths.Keys);

        var importSchema = AdminOpenApiDocumentRegistrationTestHelpers.GetMultipartSchema(document, "/customers/import/commit");
        Assert.NotNull(importSchema.AllOf);
        Assert.Contains(importSchema.AllOf, static item => item.Properties?.ContainsKey("file") == true);
        Assert.Contains(importSchema.AllOf, static item => item.Properties?.ContainsKey("conflictResolutions") == true);
    }

    [Fact]
    public async Task Generates_A_V1_Document_Including_Error_Documentation_Paths()
    {
        var document = await AdminOpenApiDocumentFactory.CreateDocument(
            "v1",
            TestContext.Current.CancellationToken,
            "MapToursEndpoints",
            "MapCustomerEndpoints",
            "MapCustomerImportEndpoints",
            "MapBookingEndpoints",
            "MapErrorDocumentationEndpoints");

        Assert.Contains("/docs/errors", document.Paths.Keys);
        Assert.Contains("/docs/errors/{identifier}", document.Paths.Keys);
    }

}

file static class AdminOpenApiDocumentRegistrationTestHelpers
{
    public static OpenApiSchema GetMultipartSchema(OpenApiDocument document, string path)
    {
        if (!document.Paths.TryGetValue(path, out var pathItem) || pathItem.Operations is null)
        {
            throw new InvalidOperationException($"Expected OpenAPI path '{path}' to exist.");
        }

        if (!pathItem.Operations.TryGetValue(HttpMethod.Post, out var operation))
        {
            throw new InvalidOperationException($"Expected POST operation for '{path}'.");
        }

        return operation.RequestBody?.Content?["multipart/form-data"].Schema as OpenApiSchema
            ?? throw new InvalidOperationException($"Expected multipart schema for '{path}'.");
    }
}
