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

        document.Paths.Keys.ShouldContain("/api/v1/tours");
        document.Paths.Keys.ShouldContain("/api/v1/tours/{id}");
        document.Paths.Keys.ShouldNotContain("/tours");
        document.Paths.Keys.ShouldNotContain("/api/v1/customers");
        document.Paths.Keys.ShouldNotContain("/api/v1/bookings");
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

        document.Paths.Keys.ShouldContain("/api/v1/customers");
        document.Paths.Keys.ShouldContain("/api/v1/customers/{id}");
        document.Paths.Keys.ShouldContain("/api/v1/customers/import");
        document.Paths.Keys.ShouldContain("/api/v1/customers/import/commit");
        document.Paths.Keys.ShouldNotContain("/customers");
        document.Paths.Keys.ShouldNotContain("/api/v1/tours");

        var importSchema = AdminOpenApiDocumentRegistrationTestHelpers.GetMultipartSchema(document, "/api/v1/customers/import/commit");
        importSchema.AllOf.ShouldBeNull();
        var properties = importSchema.Properties.ShouldNotBeNull();
        properties.ContainsKey("file").ShouldBeTrue();
        properties.ContainsKey("conflictResolutions").ShouldBeTrue();
    }

    [Fact]
    public async Task Generates_a_documents_document_containing_only_document_paths()
    {
        // Act
        var document = await AdminOpenApiDocumentFactory.CreateDocument(
            "documents",
            TestContext.Current.CancellationToken,
            "MapDocumentEndpoints",
            "MapBookingEndpoints",
            "MapCustomerEndpoints");

        // Assert
        document.Paths.Keys.ShouldContain("/api/v1/documents/{id}");
        document.Paths.Keys.ShouldContain("/api/v1/documents/{id}/download");
        document.Paths.Keys.ShouldContain("/api/v1/documents/bookings/{bookingId}/contract-drafts");
        document.Paths.Keys.ShouldNotContain("/api/v1/bookings");
        document.Paths.Keys.ShouldNotContain("/api/v1/customers");
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

        document.Info.Version.ShouldBe("1.0");
        document.Paths.Keys.ShouldContain("/api/v1/tours");
        document.Paths.Keys.ShouldContain("/api/v1/customers");
        document.Paths.Keys.ShouldContain("/api/v1/bookings");
        document.Paths.Keys.ShouldContain("/api/v1/docs/errors");
        document.Paths.Keys.ShouldContain("/api/v1/docs/errors/{identifier}");
        document.Paths.Keys.ShouldNotContain("/docs/errors");
    }

}
