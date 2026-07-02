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
        Assert.Equal("/customers", requestPath);
        var customer = Assert.Single(customers);
        Assert.Equal("Alice", customer.FirstName);
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
        Assert.Equal($"/customers/{customerId}", requestPath);
        Assert.Null(customer);
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
        Assert.Equal("/customers", requestPath);
        Assert.Equal(CustomerCreateOutcomeKind.Succeeded, outcome.Kind);
        Assert.Equal(HttpStatusCode.Created, outcome.StatusCode);
        Assert.Equal(new Uri("https://management.example/customers/created", UriKind.Absolute), outcome.Location);
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
        Assert.Equal(CustomerCreateOutcomeKind.Succeeded, outcome.Kind);
        Assert.Equal(HttpStatusCode.Created, outcome.StatusCode);
        Assert.Null(outcome.Location);
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
        Assert.Equal(CustomerCreateOutcomeKind.ValidationProblem, outcome.Kind);
        Assert.Equal(HttpStatusCode.BadRequest, outcome.StatusCode);
        Assert.NotNull(outcome.ValidationErrors);
        Assert.True(outcome.ValidationErrors.ContainsKey("Email"));
        Assert.Contains("not a valid e-mail address", outcome.ValidationErrors["Email"][0], StringComparison.Ordinal);
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
        Assert.Equal(CustomerCreateOutcomeKind.EmptyBody, outcome.Kind);
        Assert.Equal(HttpStatusCode.BadRequest, outcome.StatusCode);
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
        Assert.Equal(CustomerCreateOutcomeKind.MalformedBody, outcome.Kind);
        Assert.Equal(HttpStatusCode.BadRequest, outcome.StatusCode);
    }

    [Theory]
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
        Assert.Equal(expectedKind, outcome.Kind);
        Assert.Equal(statusCode, outcome.StatusCode);
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
        Assert.Equal($"/customers/{customerId}", requestPath);
        Assert.Equal(HttpMethods.Put, requestMethod);
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
        Assert.Equal("/customers/import", requestPath);
        Assert.Equal(1, result.SuccessCount);
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
        Assert.Equal("/customers/import/commit", requestPath);
        Assert.Equal(2, result.SuccessCount);
    }
}
