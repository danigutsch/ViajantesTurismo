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

        document.Paths.Keys.ShouldContain("/tours");
        document.Paths.Keys.ShouldContain("/tours/{id}");
        document.Paths.Keys.ShouldNotContain("/customers");
        document.Paths.Keys.ShouldNotContain("/bookings");
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

        document.Paths.Keys.ShouldContain("/customers");
        document.Paths.Keys.ShouldContain("/customers/{id}");
        document.Paths.Keys.ShouldContain("/customers/import");
        document.Paths.Keys.ShouldContain("/customers/import/commit");
        document.Paths.Keys.ShouldNotContain("/tours");

        var importSchema = AdminOpenApiDocumentRegistrationTestHelpers.GetMultipartSchema(document, "/customers/import/commit");
        importSchema.AllOf.ShouldNotBeNull();
        importSchema.AllOf.ShouldContain(static item => item.Properties?.ContainsKey("file") == true);
        importSchema.AllOf.ShouldContain(static item => item.Properties?.ContainsKey("conflictResolutions") == true);
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

        document.Paths.Keys.ShouldContain("/docs/errors");
        document.Paths.Keys.ShouldContain("/docs/errors/{identifier}");
    }

}
