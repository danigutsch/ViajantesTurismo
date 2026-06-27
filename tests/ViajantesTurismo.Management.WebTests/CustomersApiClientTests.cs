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
        var location = await sut.CreateCustomer(BuildCreateCustomerDto(), Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("/customers", requestPath);
        Assert.Equal(new Uri("https://management.example/customers/created", UriKind.Absolute), location);
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
