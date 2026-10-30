using System.Net;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using HttpOutcomeKind = PandaBear.HttpClients.HttpOutcomeKind;
using CatalogToursApiClientTestsHelpers = PandaBear.Testing.Contracts.ContractHttpClientTestHelper;
using ViajantesTurismo.Admin.Contracts.Http;
using PandaBear.Testing.Logging;

namespace ViajantesTurismo.Admin.ContractTests.ApiClients;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, Infrastructure.TestTraits.ContractCategory)]
public sealed class CustomersApiClientTests
{
    [Fact]
    public void Progress_overload_preserves_existing_client_implementations()
    {
        // Arrange
        var parameterTypes = new[] { typeof(byte[]), typeof(string), typeof(IProgress<long>), typeof(CancellationToken) };

        // Act
        var overload = typeof(ICustomersApiClient).GetMethod(nameof(ICustomersApiClient.ImportCustomers), parameterTypes);

        // Assert
        overload.ShouldNotBeNull();
        overload.IsAbstract.ShouldBeFalse();
    }

    [Fact]
    public async Task GetCustomers_requests_customers_endpoint_and_limits_items()
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
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
        var outcome = await sut.GetCustomers(TestContext.Current.CancellationToken, maxItems: 1);

        // Assert
        requestPath.ShouldBe("/api/v1/customers");
        var customers = outcome.Value.ShouldNotBeNull();
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
        var outcome = await sut.GetCustomers(TestContext.Current.CancellationToken, maxItems: 0);

        // Assert
        var customers = outcome.Value.ShouldNotBeNull();
        customers.ShouldBeEmpty();
        requestCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetCustomers_rejects_an_oversized_list_before_applying_max_items()
    {
        // Arrange
        const int oversizedResponseLength = 2 * 1024 * 1024;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ =>
            CatalogToursApiClientTestsHelpers.JsonResponse($"[{{\"firstName\":\"{new string('x', oversizedResponseLength)}\"}}]"));
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);

        // Act
        var outcome = await sut.GetCustomers(TestContext.Current.CancellationToken, maxItems: 1);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.MalformedBody);
        outcome.StatusCode.ShouldBe(HttpStatusCode.OK);
        outcome.Value.ShouldBeNull();
    }

    [Fact]
    public async Task GetCustomerById_returns_null_when_not_found()
    {
        // Arrange
        var customerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var requestPath = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.AbsolutePath ?? string.Empty;
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);

        // Act
        var outcome = await sut.GetCustomerById(customerId, TestContext.Current.CancellationToken);

        // Assert
        requestPath.ShouldBe($"/api/v1/customers/{customerId}");
        outcome.Kind.ShouldBe(HttpOutcomeKind.NotFound);
        outcome.Value.ShouldBeNull();
    }

    [Fact]
    public async Task GetCustomerById_returns_malformed_outcome_when_admin_API_returns_success_with_null_body()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => CatalogToursApiClientTestsHelpers.JsonResponse("null"));
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);

        // Act
        var outcome = await sut.GetCustomerById(Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.MalformedBody);
        outcome.Value.ShouldBeNull();
    }

    [Fact]
    public async Task CreateCustomer_returns_location_header()
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.AbsolutePath ?? string.Empty;
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Headers = { Location = new Uri("https://management.example/customers/created", UriKind.Absolute) }
            };
        });
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);

        // Act
        var outcome = await sut.CreateCustomer(BuildCreateCustomerDto(), TestContext.Current.CancellationToken);

        // Assert
        requestPath.ShouldBe("/api/v1/customers");
        outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
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
        var outcome = await sut.CreateCustomer(BuildCreateCustomerDto(), TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
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
        var outcome = await sut.CreateCustomer(BuildCreateCustomerDto(), TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.ValidationProblem);
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
        var outcome = await sut.CreateCustomer(BuildCreateCustomerDto(), TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.EmptyBody);
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
        var outcome = await sut.CreateCustomer(BuildCreateCustomerDto(), TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.MalformedBody);
        outcome.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCustomer_logs_malformed_validation_problem_without_response_body()
    {
        // Arrange
        var logger = new RecordingLogger<CustomersApiClient>();
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ =>
            CatalogToursApiClientTestsHelpers.JsonResponse("not json alice@example.test", HttpStatusCode.BadRequest));
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient, logger);

        // Act
        var outcome = await sut.CreateCustomer(BuildCreateCustomerDto(), TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.MalformedBody);
        var entry = logger.Records.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Warning);
        entry.EventId.Id.ShouldBe(1);
        entry.Message.ShouldBe("Customer create returned BadRequest with outcome MalformedBody.");
        entry.Message.Contains("alice@example.test", StringComparison.Ordinal).ShouldBeFalse();
        entry.State.Any(value => value.Value?.ToString()?.Contains("alice@example.test", StringComparison.Ordinal) == true).ShouldBeFalse();
        entry.State.ShouldContain(value => value.Key == "StatusCode" && value.Value?.ToString() == "BadRequest");
        entry.State.ShouldContain(value => value.Key == "OutcomeKind" && value.Value?.ToString() == "MalformedBody");
    }

    [Fact]
    public async Task CreateCustomer_tags_failure_activity_without_response_body()
    {
        // Arrange
        const string sensitiveValue = "alice@example.test";
        var stoppedActivities = new ConcurrentQueue<Activity>();
        using var listener = AdminContractsClientTelemetryTestsHelpers.CreateActivityListener(stoppedActivities);
        using var rootActivity = AdminContractsClientTelemetryTestsHelpers.StartRootActivity();
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ =>
            CatalogToursApiClientTestsHelpers.JsonResponse($"not json {sensitiveValue}", HttpStatusCode.BadRequest));
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);

        // Act
        var outcome = await sut.CreateCustomer(BuildCreateCustomerDto(), TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.MalformedBody);
        var activity = AdminContractsClientTelemetryTestsHelpers.SingleActivity(
            stoppedActivities,
            rootActivity,
            "customers.create");
        activity.DisplayName.ShouldBe("customers.create");
        activity.Kind.ShouldBe(ActivityKind.Client);
        activity.Tags.ShouldContain(new KeyValuePair<string, string?>("viajantes.api_area", "admin"));
        activity.Tags.ShouldContain(new KeyValuePair<string, string?>("viajantes.operation", "customers.create"));
        activity.TagObjects.ShouldContain(new KeyValuePair<string, object?>("http.response.status_code", 400));
        activity.Tags.ShouldContain(new KeyValuePair<string, string?>("viajantes.admin_command.outcome", "MalformedBody"));
        activity.Tags.Any(tag => tag.Value?.Contains(sensitiveValue, StringComparison.Ordinal) == true).ShouldBeFalse();
        activity.Baggage.Any(item => item.Value?.Contains(sensitiveValue, StringComparison.Ordinal) == true).ShouldBeFalse();
        activity.Events
            .SelectMany(@event => @event.Tags)
            .Any(tag => tag.Value?.ToString()?.Contains(sensitiveValue, StringComparison.Ordinal) == true)
            .ShouldBeFalse();
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, HttpOutcomeKind.UnexpectedStatus)]
    [InlineData(HttpStatusCode.NoContent, HttpOutcomeKind.UnexpectedStatus)]
    [InlineData(HttpStatusCode.NotFound, HttpOutcomeKind.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized, HttpOutcomeKind.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, HttpOutcomeKind.Forbidden)]
    [InlineData(HttpStatusCode.Conflict, HttpOutcomeKind.Conflict)]
    [InlineData(HttpStatusCode.TooManyRequests, HttpOutcomeKind.UnexpectedStatus)]
    public async Task CreateCustomer_returns_status_outcome_for_non_validation_failures(
        HttpStatusCode statusCode,
        HttpOutcomeKind expectedKind)
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(statusCode));
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);

        // Act
        var outcome = await sut.CreateCustomer(BuildCreateCustomerDto(), TestContext.Current.CancellationToken);

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
            requestPath = request.RequestUri?.AbsolutePath ?? string.Empty;
            requestMethod = request.Method.Method;
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);

        // Act
        var outcome = await sut.UpdateCustomer(customerId, BuildUpdateCustomerDto(), TestContext.Current.CancellationToken);

        // Assert
        requestPath.ShouldBe($"/api/v1/customers/{customerId}");
        requestMethod.ShouldBe(HttpMethods.Put);
        outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
    }

    [Fact]
    public async Task ImportCustomers_returns_import_result()
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.AbsolutePath ?? string.Empty;
            return CatalogToursApiClientTestsHelpers.JsonResponse("""
                { "successCount": 1, "errorCount": 0 }
                """);
        });
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);

        // Act
        var outcome = await sut.ImportCustomers([1, 2, 3], "customers.csv", TestContext.Current.CancellationToken);

        // Assert
        requestPath.ShouldBe("/api/v1/customers/import");
        var result = outcome.Value.ShouldNotBeNull();
        result.SuccessCount.ShouldBe(1);
    }

    [Fact]
    public async Task CommitImportWithResolutions_returns_import_result()
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.AbsolutePath ?? string.Empty;
            return CatalogToursApiClientTestsHelpers.JsonResponse("""
                { "successCount": 2, "errorCount": 0 }
                """);
        });
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);

        // Act
        var outcome = await sut.CommitImportWithResolutions(
            [1, 2, 3],
            "customers.csv",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["alice@example.test"] = "overwrite" },
            null,
            TestContext.Current.CancellationToken);

        // Assert
        requestPath.ShouldBe("/api/v1/customers/import/commit");
        var result = outcome.Value.ShouldNotBeNull();
        result.SuccessCount.ShouldBe(2);
    }

    [Fact]
    public async Task ImportCustomers_reports_only_file_bytes_written_to_the_transport()
    {
        // Arrange
        var fileContent = new byte[100];
        var reportedBytes = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(async (request, ct) =>
        {
            await request.Content.ShouldNotBeNull().CopyToAsync(Stream.Null, ct);
            return CatalogToursApiClientTestsHelpers.JsonResponse("""
                { "successCount": 1, "errorCount": 0 }
                """);
        });
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);
        var progress = new Progress<long>(bytes => reportedBytes.TrySetResult(bytes));

        // Act
        _ = await sut.ImportCustomers(fileContent, "customers.csv", progress, TestContext.Current.CancellationToken);
        var transferredBytes = await reportedBytes.Task.WaitAsync(TestContext.Current.CancellationToken);

        // Assert
        transferredBytes.ShouldBe(fileContent.LongLength);
    }

    [Fact]
    public async Task ImportCustomers_propagates_cancellation_to_the_http_request()
    {
        // Arrange
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(async (_, ct) =>
        {
            requestStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);
        using var requestCancellation = new CancellationTokenSource();

        // Act
        var import = sut.ImportCustomers([1, 2, 3], "customers.csv", null, requestCancellation.Token);
        await requestStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        await requestCancellation.CancelAsync();
        Func<Task> action = async () => await import;

        // Assert
        _ = await action.ShouldThrow<OperationCanceledException>();
    }

    [Fact]
    public async Task CommitImportWithResolutions_reports_only_file_bytes_written_to_the_transport()
    {
        // Arrange
        var fileContent = new byte[100];
        var reportedBytes = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(async (request, ct) =>
        {
            await request.Content.ShouldNotBeNull().CopyToAsync(Stream.Null, ct);
            return CatalogToursApiClientTestsHelpers.JsonResponse("""
                { "successCount": 1, "errorCount": 0 }
                """);
        });
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);
        var progress = new Progress<long>(bytes => reportedBytes.TrySetResult(bytes));

        // Act
        _ = await sut.CommitImportWithResolutions(
            fileContent,
            "customers.csv",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["alice@example.test"] = "overwrite" },
            progress,
            TestContext.Current.CancellationToken);
        var transferredBytes = await reportedBytes.Task.WaitAsync(TestContext.Current.CancellationToken);

        // Assert
        transferredBytes.ShouldBe(fileContent.LongLength);
    }

    [Fact]
    public async Task CommitImportWithResolutions_propagates_cancellation_to_the_http_request()
    {
        // Arrange
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(async (_, ct) =>
        {
            requestStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var sut = CustomersApiClientTestsHelpers.CreateSut(httpClient);
        using var requestCancellation = new CancellationTokenSource();

        // Act
        var commit = sut.CommitImportWithResolutions(
            [1, 2, 3],
            "customers.csv",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["alice@example.test"] = "overwrite" },
            null,
            requestCancellation.Token);
        await requestStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        await requestCancellation.CancelAsync();
        Func<Task> action = async () => await commit;

        // Assert
        _ = await action.ShouldThrow<OperationCanceledException>();
    }
}
