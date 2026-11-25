using System.Globalization;
using System.Text.Json;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.Http;

namespace ViajantesTurismo.Admin.ApiServiceTests;

[Trait(TestTraitNames.CategoryName, TestTraitValues.EndpointCategory)]
[TestHost(TestHost.TestServer)]
public sealed class DocumentMaintenanceExecutionsEndpointTests
{
    private const string Route = "/api/v1/operations/document-maintenance/drafts/executions";

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Reader", HttpStatusCode.Forbidden)]
    [InlineData("Operator", HttpStatusCode.Forbidden)]
    public async Task Execution_history_requires_operations_read(string? role, HttpStatusCode expected)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        if (role is not null)
        {
            AdminApiTestHost.ConfigureAuthenticatedClient(client, role);
        }

        // Act
        using var response = await client.GetAsync(new Uri(Route, UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(expected);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
    }

    [Theory]
    [InlineData("?limit=0")]
    [InlineData("?limit=101")]
    [InlineData("?beforeStartedAt=2026-11-24T08%3A00%3A00Z")]
    [InlineData("?beforeRunId=00000000-0000-0000-0000-000000000001")]
    [InlineData("?beforeStartedAt=2026-11-24T08%3A00%3A00Z&beforeRunId=00000000-0000-0000-0000-000000000000")]
    public async Task Execution_history_rejects_invalid_bounds_before_reading(string suffix)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");

        // Act
        using var response = await client.GetAsync(new Uri(Route + suffix, UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("0")]
    public async Task Execution_history_rejects_unknown_and_numeric_job_names(string job)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var query = new StubDocumentMaintenanceExecutionsQuery(new GetDocumentMaintenanceExecutionsDto([], null));
        await using var factory = AdminApiTestHost.Create(services => services.AddSingleton<IDocumentMaintenanceExecutionsQuery>(query));
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var path = new Uri($"/api/v1/operations/document-maintenance/{job}/executions", UriKind.Relative);

        // Act
        using var response = await client.GetAsync(path, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        query.Request.ShouldBeNull();
    }

    [Fact]
    public async Task First_execution_page_defaults_to_a_bounded_read_without_a_cursor()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var query = new StubDocumentMaintenanceExecutionsQuery(new GetDocumentMaintenanceExecutionsDto([], null));
        await using var factory = AdminApiTestHost.Create(services => services.AddSingleton<IDocumentMaintenanceExecutionsQuery>(query));
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");

        // Act
        using var response = await client.GetAsync(new Uri(Route, UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        query.Request.ShouldBe((DocumentMaintenanceJob.Drafts, ContractConstants.DefaultDocumentMaintenancePageSize, null));
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("OperationsScheduleController")]
    public async Task Authorized_operators_read_only_metadata_and_the_exact_execution_cursor(string role)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var time = new DateTimeOffset(2026, 11, 24, 8, 0, 0, TimeSpan.Zero);
        var before = new DocumentMaintenanceExecutionCursorDto(time, Guid.CreateVersion7());
        var execution = new DocumentMaintenanceExecutionDto(Guid.CreateVersion7(), time.AddMinutes(-2),
            time.AddMinutes(-3), time.AddMinutes(-1), null, DocumentMaintenanceExecutionStatus.Failed);
        var next = new DocumentMaintenanceExecutionCursorDto(execution.StartedAtUtc, execution.RunId);
        var query = new StubDocumentMaintenanceExecutionsQuery(new GetDocumentMaintenanceExecutionsDto([execution], next));
        await using var factory = AdminApiTestHost.Create(services => services.AddSingleton<IDocumentMaintenanceExecutionsQuery>(query));
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, role);
        var started = Uri.EscapeDataString(before.StartedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        var path = new Uri($"{Route}?beforeStartedAt={started}&beforeRunId={before.RunId:D}", UriKind.Relative);

        // Act
        using var response = await client.GetAsync(path, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        using var document = JsonDocument.Parse(json);
        var row = document.RootElement.GetProperty("executions").EnumerateArray().ShouldHaveSingleItem();
        var fields = row.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
        var typed = await new DocumentMaintenanceApiClient(client).ReadExecutions(DocumentMaintenanceJob.Drafts, before, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        query.Request.ShouldBe((DocumentMaintenanceJob.Drafts, ContractConstants.DefaultDocumentMaintenancePageSize, before));
        fields.ShouldBe(["finishedAtUtc", "retryAtUtc", "runId", "scheduledForUtc", "startedAtUtc", "status"]);
        row.GetProperty("status").GetString().ShouldBe("Failed");
        var page = typed.Value.ShouldNotBeNull();
        page.Executions.ShouldHaveSingleItem().ShouldBe(execution);
        page.Next.ShouldBe(next);
    }
}
