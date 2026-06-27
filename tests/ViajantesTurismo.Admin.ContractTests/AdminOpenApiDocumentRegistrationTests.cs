using Xunit;

namespace ViajantesTurismo.Admin.ContractTests;

/// <summary>
/// Verifies that Admin named OpenAPI documents expose the intended boundary slices.
/// </summary>
public sealed class AdminOpenApiDocumentRegistrationTests
{
    [Fact]
    public async Task Generates_a_tours_document_containing_only_tours_paths()
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
    public async Task Generates_a_customers_document_including_import_paths()
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
    public async Task Generates_a_v1_document_including_error_documentation_paths()
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
