using Microsoft.OpenApi;
using TestTraits = ViajantesTurismo.Admin.ContractTests.Infrastructure.TestTraits;

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
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(PandaBear.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
    public async Task Generates_a_customers_document_including_import_paths()
    {
        // Act
        var document = await AdminOpenApiDocumentFactory.CreateDocument(
            "customers",
            TestContext.Current.CancellationToken,
            "MapCustomerEndpoints",
            "MapCustomerImportEndpoints",
            "MapToursEndpoints");

        var importSchema = AdminOpenApiDocumentRegistrationTestHelpers.GetMultipartSchema(document, "/api/v1/customers/import/commit");
        var commitImportPath = document.Paths["/api/v1/customers/import/commit"];
        var commitImportOperations = commitImportPath.Operations.ShouldNotBeNull();
        commitImportOperations.TryGetValue(HttpMethod.Post, out var commitImportOperation).ShouldBeTrue();
        var commitImport = commitImportOperation.ShouldNotBeNull();
        var commitImportRequestBody = commitImport.RequestBody.ShouldNotBeNull();
        var commitImportContent = commitImportRequestBody.Content.ShouldNotBeNull();
        var commitImportPathParameters = commitImport.Parameters?
            .Where(parameter => parameter.In == ParameterLocation.Path)
            .ToArray() ?? [];
        var commitImportQueryParameters = commitImport.Parameters?
            .Where(parameter => parameter.In == ParameterLocation.Query)
            .ToArray() ?? [];

        // Assert
        document.Paths.Keys.ShouldContain("/api/v1/customers");
        document.Paths.Keys.ShouldContain("/api/v1/customers/{id}");
        document.Paths.Keys.ShouldContain("/api/v1/customers/import");
        document.Paths.Keys.ShouldContain("/api/v1/customers/import/commit");
        document.Paths.Keys.ShouldNotContain("/customers");
        document.Paths.Keys.ShouldNotContain("/api/v1/tours");

        importSchema.AllOf.ShouldBeNull();
        var properties = importSchema.Properties.ShouldNotBeNull();
        properties.ContainsKey("file").ShouldBeTrue();
        properties.ContainsKey("conflictResolutions").ShouldBeTrue();
        commitImportRequestBody.Required.ShouldBeTrue();
        commitImportContent.Count.ShouldBe(1);
        commitImportContent.Keys.ShouldHaveSingleItem().ShouldBe("multipart/form-data");
        commitImportPathParameters.ShouldBeEmpty();
        commitImportQueryParameters.ShouldBeEmpty();
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

    [Theory]
    [InlineData("documents")]
    [InlineData("v1")]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(PandaBear.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
    public async Task Document_draft_commands_describe_the_idempotency_header_as_required(string documentName)
    {
        // Arrange
        var document = await AdminOpenApiDocumentFactory.CreateDocument(
            documentName,
            TestContext.Current.CancellationToken,
            "MapDocumentEndpoints");
        string[] commandPaths =
        [
            "/api/v1/documents/bookings/{bookingId}/contract-drafts",
            "/api/v1/documents/{id}/regenerate"
        ];

        // Act
        var commandOperations = commandPaths
            .Select(path => document.Paths[path].Operations.ShouldNotBeNull()[HttpMethod.Post])
            .ToArray();

        // Assert
        foreach (var operation in commandOperations)
        {
            var parameter = operation.Parameters.ShouldNotBeNull().ShouldHaveSingleItem(
                parameter => parameter.Name == "Idempotency-Key" && parameter.In == ParameterLocation.Header);
            parameter.Required.ShouldBeTrue();
        }
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
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(PandaBear.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
    public void Documents_document_describes_required_route_identity_and_idempotency_headers()
    {
        // Arrange
        var document = AdminOpenApiArtifactDriftGuard.CreateSnapshotSet()
            .GetCanonicalSnapshot("documents")
            .AsObject();
        var paths = document["paths"].ShouldNotBeNull().AsObject();
        (string Path, string Method, string RouteParameterName)[] idempotentOperations =
        [
            ("/api/v1/documents/bookings/{bookingId}/contract-drafts", "post", "bookingId"),
            ("/api/v1/documents/{id}/regenerate", "post", "id")
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
            var routeParameter = parameters
                .Select(parameter => parameter.ShouldNotBeNull().AsObject())
                .ShouldHaveSingleItem(parameter => parameter["name"]?.GetValue<string>() == operation.RouteParameterName);

            // Assert
            routeParameter["in"].ShouldNotBeNull().GetValue<string>().ShouldBe("path");
            routeParameter["required"].ShouldNotBeNull().GetValue<bool>().ShouldBeTrue();
            header["in"].ShouldNotBeNull().GetValue<string>().ShouldBe("header");
            header["required"].ShouldNotBeNull().GetValue<bool>().ShouldBeTrue();
            var schema = header["schema"].ShouldNotBeNull().AsObject();
            schema["type"].ShouldNotBeNull().GetValue<string>().ShouldBe("string");
        }
    }

    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(PandaBear.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
    public void Customer_create_operation_uses_required_JSON_body_and_no_URL_parameters()
    {
        // Arrange
        var document = AdminOpenApiArtifactDriftGuard.CreateSnapshotSet()
            .GetCanonicalSnapshot("customers")
            .AsObject();
        var paths = document["paths"].ShouldNotBeNull().AsObject();
        var customersPath = paths["/api/v1/customers"].ShouldNotBeNull().AsObject();
        var createOperation = customersPath["post"].ShouldNotBeNull().AsObject();
        var createRequestBody = createOperation["requestBody"].ShouldNotBeNull().AsObject();

        // Act
        var createSchema = createRequestBody["content"].ShouldNotBeNull().AsObject()["application/json"]
            .ShouldNotBeNull().AsObject()["schema"].ShouldNotBeNull().AsObject();
        var createParameters = createOperation["parameters"] is { } createParameterNode
            ? createParameterNode.AsArray()
                .Select(parameter => parameter.ShouldNotBeNull().AsObject())
                .ToArray()
            : [];
        var createPathParameters = createParameters
            .Where(parameter => parameter["in"]?.GetValue<string>() == "path")
            .ToArray();
        var createQueryParameters = createParameters
            .Where(parameter => parameter["in"]?.GetValue<string>() == "query")
            .ToArray();

        // Assert
        createOperation["operationId"].ShouldNotBeNull().GetValue<string>().ShouldBe("CreateCustomer");
        createRequestBody["required"].ShouldNotBeNull().GetValue<bool>().ShouldBeTrue();
        createSchema["$ref"].ShouldNotBeNull().GetValue<string>().ShouldBe("#/components/schemas/CreateCustomerDto");
        createPathParameters.ShouldBeEmpty();
        createQueryParameters.ShouldBeEmpty();
    }

    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(PandaBear.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
    public void Customer_update_operation_uses_required_JSON_body_and_only_required_id_path_parameter()
    {
        // Arrange
        var document = AdminOpenApiArtifactDriftGuard.CreateSnapshotSet()
            .GetCanonicalSnapshot("customers")
            .AsObject();
        var paths = document["paths"].ShouldNotBeNull().AsObject();
        var customerByIdPath = paths["/api/v1/customers/{id}"].ShouldNotBeNull().AsObject();
        var updateOperation = customerByIdPath["put"].ShouldNotBeNull().AsObject();
        var updateRequestBody = updateOperation["requestBody"].ShouldNotBeNull().AsObject();

        // Act
        var updateSchema = updateRequestBody["content"].ShouldNotBeNull().AsObject()["application/json"]
            .ShouldNotBeNull().AsObject()["schema"].ShouldNotBeNull().AsObject();
        var updateParameters = updateOperation["parameters"].ShouldNotBeNull().AsArray()
            .Select(parameter => parameter.ShouldNotBeNull().AsObject())
            .ToArray();
        var updatePathParameters = updateParameters
            .Where(parameter => parameter["in"]?.GetValue<string>() == "path")
            .ToArray();
        var updateQueryParameters = updateParameters
            .Where(parameter => parameter["in"]?.GetValue<string>() == "query")
            .ToArray();
        var routeParameter = updatePathParameters.ShouldHaveSingleItem();

        // Assert
        updateOperation["operationId"].ShouldNotBeNull().GetValue<string>().ShouldBe("UpdateCustomer");
        updateRequestBody["required"].ShouldNotBeNull().GetValue<bool>().ShouldBeTrue();
        updateSchema["$ref"].ShouldNotBeNull().GetValue<string>().ShouldBe("#/components/schemas/UpdateCustomerDto");
        routeParameter["name"].ShouldNotBeNull().GetValue<string>().ShouldBe("id");
        routeParameter["in"].ShouldNotBeNull().GetValue<string>().ShouldBe("path");
        routeParameter["required"].ShouldNotBeNull().GetValue<bool>().ShouldBeTrue();
        updateQueryParameters.ShouldBeEmpty();
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
