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
    public void Documents_document_describes_validation_problems_for_generate_regenerate_and_update()
    {
        // Arrange
        var document = AdminOpenApiArtifactDriftGuard.CreateSnapshotSet()
            .GetCanonicalSnapshot("documents")
            .AsObject();
        var paths = document["paths"].ShouldNotBeNull().AsObject();
        (string Path, string Method)[] validationOperations =
        [
            ("/api/v1/documents/bookings/{bookingId}/contract-drafts", "post"),
            ("/api/v1/documents/{id}/regenerate", "post"),
            ("/api/v1/documents/{id}/fields/{fieldId}", "patch")
        ];

        // Act
        var validationResponses = validationOperations.Select(operation =>
        {
            var path = paths[operation.Path].ShouldNotBeNull().AsObject();
            var endpoint = path[operation.Method].ShouldNotBeNull().AsObject();
            return endpoint["responses"].ShouldNotBeNull().AsObject()["400"].ShouldNotBeNull().AsObject();
        }).ToArray();

        // Assert
        foreach (var response in validationResponses)
        {
            var content = response["content"].ShouldNotBeNull().AsObject();
            var problem = content["application/problem+json"].ShouldNotBeNull().AsObject();
            var schema = problem["schema"].ShouldNotBeNull().AsObject();
            schema["$ref"].ShouldNotBeNull().GetValue<string>()
                .ShouldBe("#/components/schemas/HttpValidationProblemDetails");
        }

        var components = document["components"].ShouldNotBeNull().AsObject();
        var schemas = components["schemas"].ShouldNotBeNull().AsObject();
        var validationProblem = schemas["HttpValidationProblemDetails"].ShouldNotBeNull().AsObject();
        var properties = validationProblem["properties"].ShouldNotBeNull().AsObject();
        properties.ContainsKey("errors").ShouldBeTrue();
    }

    [Fact]
    public void Documents_document_describes_optional_idempotency_headers_for_generate_and_regenerate()
    {
        // Arrange
        var document = AdminOpenApiArtifactDriftGuard.CreateSnapshotSet()
            .GetCanonicalSnapshot("documents")
            .AsObject();
        var paths = document["paths"].ShouldNotBeNull().AsObject();
        (string Path, string Method)[] idempotentOperations =
        [
            ("/api/v1/documents/bookings/{bookingId}/contract-drafts", "post"),
            ("/api/v1/documents/{id}/regenerate", "post")
        ];

        foreach (var operation in idempotentOperations)
        {
            // Act
            var path = paths[operation.Path].ShouldNotBeNull().AsObject();
            var endpoint = path[operation.Method].ShouldNotBeNull().AsObject();
            var parameters = endpoint["parameters"].ShouldNotBeNull().AsArray();
            var idempotencyHeaders = parameters
                .Select(parameter => parameter.ShouldNotBeNull().AsObject())
                .Where(parameter => parameter["name"]?.GetValue<string>() == "Idempotency-Key")
                .ToArray();
            var header = idempotencyHeaders.ShouldHaveSingleItem();

            // Assert
            header["in"].ShouldNotBeNull().GetValue<string>().ShouldBe("header");
            (header["required"]?.GetValue<bool>() ?? false).ShouldBeFalse();
            var schema = header["schema"].ShouldNotBeNull().AsObject();
            schema["type"].ShouldNotBeNull().GetValue<string>().ShouldBe("string");
        }
    }

    [Fact]
    public void Documents_document_describes_html_download_as_binary()
    {
        // Arrange
        var document = AdminOpenApiArtifactDriftGuard.CreateSnapshotSet()
            .GetCanonicalSnapshot("documents")
            .AsObject();
        var paths = document["paths"].ShouldNotBeNull().AsObject();

        // Act
        var path = paths["/api/v1/documents/{id}/download"].ShouldNotBeNull().AsObject();
        var endpoint = path["get"].ShouldNotBeNull().AsObject();
        var responses = endpoint["responses"].ShouldNotBeNull().AsObject();
        var success = responses["200"].ShouldNotBeNull().AsObject();
        var content = success["content"].ShouldNotBeNull().AsObject();
        var html = content["text/html"].ShouldNotBeNull().AsObject();
        var schema = html["schema"].ShouldNotBeNull().AsObject();
        var components = document["components"].ShouldNotBeNull().AsObject();
        var schemas = components["schemas"].ShouldNotBeNull().AsObject();
        var stream = schemas["Stream"].ShouldNotBeNull().AsObject();

        // Assert
        schema["$ref"].ShouldNotBeNull().GetValue<string>().ShouldBe("#/components/schemas/Stream");
        stream["type"].ShouldNotBeNull().GetValue<string>().ShouldBe("string");
        stream["format"].ShouldNotBeNull().GetValue<string>().ShouldBe("binary");
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
