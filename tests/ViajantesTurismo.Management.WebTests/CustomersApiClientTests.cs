using System.Net;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ContractCommandOutcomeKind = SharedKernel.HttpClients.ContractCommandOutcomeKind;

namespace ViajantesTurismo.Management.WebTests;

[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ApiClientCategory)]
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
    public async Task GetCustomerById_throws_when_admin_api_returns_success_with_null_body()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => CatalogToursApiClientTestsHelpers.JsonResponse("null"));
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);

        // Act
        Func<Task> act = async () => await sut.GetCustomerById(Guid.CreateVersion7(), Xunit.TestContext.Current.CancellationToken);

        // Assert
        var exception = await act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("The customer response body was empty.");
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
        outcome.Kind.ShouldBe(ContractCommandOutcomeKind.Succeeded);
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
        outcome.Kind.ShouldBe(ContractCommandOutcomeKind.Succeeded);
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
        outcome.Kind.ShouldBe(ContractCommandOutcomeKind.ValidationProblem);
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
        outcome.Kind.ShouldBe(ContractCommandOutcomeKind.EmptyBody);
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
        outcome.Kind.ShouldBe(ContractCommandOutcomeKind.MalformedBody);
        outcome.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCustomer_logs_malformed_validation_problem_without_response_body()
    {
        // Arrange
        var logger = new CollectingLogger<CustomersApiClient>();
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ =>
            CatalogToursApiClientTestsHelpers.JsonResponse("not json alice@example.test", HttpStatusCode.BadRequest));
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient, logger);

        // Act
        var outcome = await sut.CreateCustomer(BuildCreateCustomerDto(), Xunit.TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(ContractCommandOutcomeKind.MalformedBody);
        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.LogLevel.ShouldBe(LogLevel.Warning);
        entry.EventId.Id.ShouldBe(1);
        entry.Message.ShouldBe("Customer create returned BadRequest with outcome MalformedBody.");
        entry.Message.Contains("alice@example.test", StringComparison.Ordinal).ShouldBeFalse();
        entry.State.Values.Any(value => value.Contains("alice@example.test", StringComparison.Ordinal)).ShouldBeFalse();
        entry.State["StatusCode"].ShouldBe("BadRequest");
        entry.State["OutcomeKind"].ShouldBe("MalformedBody");
    }

    [Fact]
    public async Task CreateCustomer_tags_failure_activity_without_response_body()
    {
        // Arrange
        List<Activity> stoppedActivities = [];
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "ViajantesTurismo.Admin.Contracts.Clients",
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = stoppedActivities.Add
        };
        ActivitySource.AddActivityListener(listener);
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ =>
            CatalogToursApiClientTestsHelpers.JsonResponse("not json alice@example.test", HttpStatusCode.BadRequest));
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);

        // Act
        var outcome = await sut.CreateCustomer(BuildCreateCustomerDto(), Xunit.TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(ContractCommandOutcomeKind.MalformedBody);
        var activity = stoppedActivities
            .Where(candidate => candidate.DisplayName == "customers.create")
            .ShouldHaveSingleItem();
        activity.DisplayName.ShouldBe("customers.create");
        activity.Kind.ShouldBe(ActivityKind.Client);
        activity.Tags.ShouldContain(new KeyValuePair<string, string?>("viajantes.api_area", "admin"));
        activity.Tags.ShouldContain(new KeyValuePair<string, string?>("viajantes.operation", "customers.create"));
        activity.TagObjects.ShouldContain(new KeyValuePair<string, object?>("http.response.status_code", 400));
        activity.Tags.ShouldContain(new KeyValuePair<string, string?>("viajantes.admin_command.outcome", "MalformedBody"));
        activity.Tags.ShouldNotContain(new KeyValuePair<string, string?>("response.body", "not json alice@example.test"));
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, ContractCommandOutcomeKind.UnexpectedStatus)]
    [InlineData(HttpStatusCode.NoContent, ContractCommandOutcomeKind.UnexpectedStatus)]
    [InlineData(HttpStatusCode.NotFound, ContractCommandOutcomeKind.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized, ContractCommandOutcomeKind.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, ContractCommandOutcomeKind.Forbidden)]
    [InlineData(HttpStatusCode.Conflict, ContractCommandOutcomeKind.Conflict)]
    [InlineData(HttpStatusCode.TooManyRequests, ContractCommandOutcomeKind.UnexpectedStatus)]
    public async Task CreateCustomer_returns_status_outcome_for_non_validation_failures(
        HttpStatusCode statusCode,
        ContractCommandOutcomeKind expectedKind)
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
