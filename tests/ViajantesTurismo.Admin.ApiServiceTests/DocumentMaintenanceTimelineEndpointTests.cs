using System.Text.Json;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiServiceTests;

[Trait(TestTraitNames.CategoryName, TestTraitValues.EndpointCategory)]
[TestHost(TestHost.TestServer)]
public sealed class DocumentMaintenanceTimelineEndpointTests
{
    private const string Path = "/api/v1/operations/document-maintenance/drafts";

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Reader", HttpStatusCode.Forbidden)]
    [InlineData("Operator", HttpStatusCode.Forbidden)]
    public async Task Maintenance_job_timeline_requires_the_admin_role(string? role, HttpStatusCode expected)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var query = new StubDocumentMaintenanceTimelineQuery(new GetDocumentMaintenanceTimelineDto { Runs = [] });
        await using var factory = AdminApiTestHost.Create(services => services.AddSingleton<IDocumentMaintenanceTimelineQuery>(query));
        using var client = factory.CreateClient();
        if (role is not null)
        {
            AdminApiTestHost.ConfigureAuthenticatedClient(client, role);
        }

        // Act
        using var response = await client.GetAsync(new Uri(Path, UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(expected);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        query.Request.ShouldBeNull();
    }

    [Theory]
    [InlineData("?limit=101", HttpStatusCode.BadRequest)]
    [InlineData("?limit=0", HttpStatusCode.BadRequest)]
    [InlineData("?beforeOccurredAt=2026-11-24T08%3A00%3A00Z", HttpStatusCode.BadRequest)]
    [InlineData("?beforeOccurredAt=2026-11-24T08%3A00%3A00Z&beforeRunId=00000000-0000-0000-0000-000000000000", HttpStatusCode.BadRequest)]
    [InlineData(null, HttpStatusCode.NotFound)]
    public async Task Maintenance_job_timeline_rejects_unsupported_requests_before_querying(string? suffix, HttpStatusCode expected)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var query = new StubDocumentMaintenanceTimelineQuery(new GetDocumentMaintenanceTimelineDto { Runs = [] });
        await using var factory = AdminApiTestHost.Create(services => services.AddSingleton<IDocumentMaintenanceTimelineQuery>(query));
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");

        // Act
        var path = suffix is null ? "/api/v1/operations/document-maintenance/unknown" : Path + suffix;
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(expected);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        query.Request.ShouldBeNull();
    }

    [Fact]
    public async Task Admin_receives_a_counts_only_timeline_page_with_its_continuation()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var timestamp = new DateTimeOffset(2026, 11, 24, 8, 0, 0, TimeSpan.Zero);
        var runId = Guid.CreateVersion7();
        var before = new DocumentMaintenanceRunCursorDto(timestamp, runId);
        var next = new DocumentMaintenanceRunCursorDto(timestamp.AddMinutes(-1), Guid.CreateVersion7());
        var query = new StubDocumentMaintenanceTimelineQuery(new GetDocumentMaintenanceTimelineDto
        {
            Runs = [new DocumentMaintenanceRunDto(next.OccurredAtUtc.UtcDateTime, 2, 1, "Refused")],
            Next = next,
        });
        await using var factory = AdminApiTestHost.Create(services => services.AddSingleton<IDocumentMaintenanceTimelineQuery>(query));
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var route = new Uri($"{Path}?limit=1&beforeOccurredAt=2026-11-24T08%3A00%3A00Z&beforeRunId={runId:D}", UriKind.Relative);

        // Act
        using var response = await client.GetAsync(route, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        using var document = JsonDocument.Parse(json);
        var row = document.RootElement.GetProperty("runs").EnumerateArray().ShouldHaveSingleItem();
        var fields = row.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
        var page = await response.Content.ReadFromJsonAsync<GetDocumentMaintenanceTimelineDto>(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        query.Request.ShouldBe((DocumentMaintenanceJob.Drafts, 1, before));
        fields.ShouldBe(["heldCount", "occurredAtUtc", "outcome", "purgedCount"]);
        page.ShouldNotBeNull().Next.ShouldBe(next);
    }

    [Fact]
    public async Task Timeline_defaults_to_the_first_bounded_page_for_the_requested_job()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var query = new StubDocumentMaintenanceTimelineQuery(new GetDocumentMaintenanceTimelineDto { Runs = [] });
        await using var factory = AdminApiTestHost.Create(services => services.AddSingleton<IDocumentMaintenanceTimelineQuery>(query));
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var route = new Uri("/api/v1/operations/document-maintenance/idempotency", UriKind.Relative);

        // Act
        using var response = await client.GetAsync(route, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        query.Request.ShouldBe((DocumentMaintenanceJob.Idempotency, ContractConstants.DefaultDocumentMaintenancePageSize, null));
    }
}
