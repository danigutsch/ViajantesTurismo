using System.Net;
using Microsoft.AspNetCore.Http;

namespace ViajantesTurismo.Management.WebTests;

public sealed class CustomersApiClientTests
{
    [Fact]
    public async Task GetCustomers_requests_customers_endpoint_and_limits_items()
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            return CatalogToursApiClientTestsHelpers.JsonResponse("""
                [
                  {
                    "id":"11111111-1111-1111-1111-111111111111",
                    "firstName":"Alice",
                    "lastName":"Rider",
                    "email":"alice@example.test",
                    "mobile":"+15550000001",
                    "nationality":"Brazilian",
                    "bikeType":1
                  },
                  {
                    "id":"22222222-2222-2222-2222-222222222222",
                    "firstName":"Bob",
                    "lastName":"Rider",
                    "email":"bob@example.test",
                    "mobile":"+15550000002",
                    "nationality":"Brazilian",
                    "bikeType":2
                  }
                ]
                """);
        });
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);

        // Act
        var customers = await sut.GetCustomers(Xunit.TestContext.Current.CancellationToken, maxItems: 1);

        // Assert
        requestPath.ShouldBe("/customers");
        var customer = customers.ShouldHaveSingleItem();
        customer.FirstName.ShouldBe("Alice");
    }

    [Fact]
    public async Task GetCustomers_returns_empty_without_request_when_max_items_is_zero()
    {
        // Arrange
        var requestCount = 0;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ =>
        {
            requestCount++;
            return CatalogToursApiClientTestsHelpers.JsonResponse("[]");
        });
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);

        // Act
        var customers = await sut.GetCustomers(Xunit.TestContext.Current.CancellationToken, maxItems: 0);

        // Assert
        customers.ShouldBeEmpty();
        requestCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetCustomerById_returns_null_when_not_found()
    {
        // Arrange
        var customerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var requestPath = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path;
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);

        // Act
        var customer = await sut.GetCustomerById(customerId, Xunit.TestContext.Current.CancellationToken);

        // Assert
        requestPath.ShouldBe($"/customers/{customerId}");
        customer.ShouldBeNull();
    }

    [Fact]
    public async Task CreateCustomer_returns_location_header()
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path;
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Headers = { Location = new Uri("https://management.example/customers/created", UriKind.Absolute) }
            };
        });
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);

        // Act
        var outcome = await sut.CreateCustomer(BuildCreateCustomerDto(), Xunit.TestContext.Current.CancellationToken);

        // Assert
        requestPath.ShouldBe("/customers");
        outcome.Kind.ShouldBe(CustomerCreateOutcomeKind.Succeeded);
        outcome.StatusCode.ShouldBe(HttpStatusCode.Created);
        outcome.Location.ShouldBe(new Uri("https://management.example/customers/created", UriKind.Absolute));
    }

    [Fact]
    public async Task CreateCustomer_returns_success_when_location_header_is_absent()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(HttpStatusCode.Created));
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);

        // Act
        var outcome = await sut.CreateCustomer(BuildCreateCustomerDto(), Xunit.TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(CustomerCreateOutcomeKind.Succeeded);
        outcome.StatusCode.ShouldBe(HttpStatusCode.Created);
        outcome.Location.ShouldBeNull();
    }

    [Fact]
    public async Task CreateCustomer_returns_validation_problem_for_validation_errors()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ =>
            CatalogToursApiClientTestsHelpers.JsonResponse(
                """
                {
                  "type":"https://tools.ietf.org/html/rfc9110#section-15.5.1",
                  "title":"One or more validation errors occurred.",
                  "status":400,
                  "errors":{"Email":["The Email field is not a valid e-mail address."]}
                }
                """,
                HttpStatusCode.BadRequest));
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);

        // Act
        var outcome = await sut.CreateCustomer(BuildCreateCustomerDto(), Xunit.TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(CustomerCreateOutcomeKind.ValidationProblem);
        outcome.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        outcome.ValidationErrors.ShouldNotBeNull();
        outcome.ValidationErrors.ContainsKey("Email").ShouldBeTrue();
        outcome.ValidationErrors["Email"][0].ShouldContain("not a valid e-mail address", StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateCustomer_returns_empty_body_for_empty_validation_problem()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);

        // Act
        var outcome = await sut.CreateCustomer(BuildCreateCustomerDto(), Xunit.TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(CustomerCreateOutcomeKind.EmptyBody);
        outcome.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{ \"errors\": {} }")]
    public async Task CreateCustomer_returns_malformed_body_for_invalid_validation_problem(string content)
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ =>
            CatalogToursApiClientTestsHelpers.JsonResponse(content, HttpStatusCode.BadRequest));
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);

        // Act
        var outcome = await sut.CreateCustomer(BuildCreateCustomerDto(), Xunit.TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(CustomerCreateOutcomeKind.MalformedBody);
        outcome.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, CustomerCreateOutcomeKind.UnexpectedStatus)]
    [InlineData(HttpStatusCode.NoContent, CustomerCreateOutcomeKind.UnexpectedStatus)]
    [InlineData(HttpStatusCode.NotFound, CustomerCreateOutcomeKind.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized, CustomerCreateOutcomeKind.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, CustomerCreateOutcomeKind.Forbidden)]
    [InlineData(HttpStatusCode.Conflict, CustomerCreateOutcomeKind.Conflict)]
    [InlineData(HttpStatusCode.TooManyRequests, CustomerCreateOutcomeKind.UnexpectedStatus)]
    public async Task CreateCustomer_returns_status_outcome_for_non_validation_failures(
        HttpStatusCode statusCode,
        CustomerCreateOutcomeKind expectedKind)
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(statusCode));
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);

        // Act
        var outcome = await sut.CreateCustomer(BuildCreateCustomerDto(), Xunit.TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(expectedKind);
        outcome.StatusCode.ShouldBe(statusCode);
    }

    [Fact]
    public async Task UpdateCustomer_sends_put_request()
    {
        // Arrange
        var customerId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var requestPath = string.Empty;
        var requestMethod = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path;
            requestMethod = request.Method;
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);

        // Act
        await sut.UpdateCustomer(customerId, BuildUpdateCustomerDto(), Xunit.TestContext.Current.CancellationToken);

        // Assert
        requestPath.ShouldBe($"/customers/{customerId}");
        requestMethod.ShouldBe(HttpMethods.Put);
    }

    [Fact]
    public async Task ImportCustomers_returns_import_result()
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path;
            return CatalogToursApiClientTestsHelpers.JsonResponse("""
                { "successCount": 1, "errorCount": 0 }
                """);
        });
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);

        // Act
        var result = await sut.ImportCustomers([1, 2, 3], "customers.csv", Xunit.TestContext.Current.CancellationToken);

        // Assert
        requestPath.ShouldBe("/customers/import");
        result.SuccessCount.ShouldBe(1);
    }

    [Fact]
    public async Task CommitImportWithResolutions_returns_import_result()
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path;
            return CatalogToursApiClientTestsHelpers.JsonResponse("""
                { "successCount": 2, "errorCount": 0 }
                """);
        });
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);

        // Act
        var result = await sut.CommitImportWithResolutions(
            [1, 2, 3],
            "customers.csv",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["alice@example.test"] = "overwrite" },
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        requestPath.ShouldBe("/customers/import/commit");
        result.SuccessCount.ShouldBe(2);
    }
}
