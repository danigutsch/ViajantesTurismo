using System.Net;
using System.Text.Json;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.HostedTests.Admin.Api.Infrastructure;

namespace ViajantesTurismo.HostedTests.Admin.Api.Operations;

[TestScope(TestScope.Integration)]
[TestHost(TestHost.Aspire)]
public sealed class DocumentMaintenanceApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Authorized_history_is_a_metadata_only_response_with_no_store_caching()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var route = new Uri("/api/v1/operations/document-maintenance", UriKind.Relative);

        // Act
        using var response = await fixture.Client.GetAsync(route, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        using var document = JsonDocument.Parse(content);
        var properties = document.RootElement.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
        properties.ShouldBe(["auditRuns", "draftRuns", "idempotencyRuns", "schedules"]);
        document.RootElement.GetProperty("schedules").GetArrayLength().ShouldBe(3);
    }

    [Fact]
    public async Task The_same_admin_reads_and_confirms_an_audited_schedule_control()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var timeline = new Uri("/api/v1/operations/document-maintenance/drafts", UriKind.Relative);
        var control = new Uri("/api/v1/operations/document-maintenance/drafts/resume", UriKind.Relative);
        const string reason = "Confirm the existing active draft retention schedule.";
        var input = new CreateDocumentMaintenanceControlDto
        {
            RequestId = Guid.CreateVersion7(),
            Confirmed = true,
            Reason = $"  {reason}  ",
        };
        var unconfirmedInput = input with
        {
            Confirmed = false,
        };

        // Act
        using var before = await fixture.Client.GetAsync(timeline, ct);
        using var unconfirmed = await fixture.Client.PostAsJsonAsync(control, unconfirmedInput, ct);
        var auditBeforeConfirmation = await fixture.GetMaintenanceControlAudit(input.RequestId, ct);
        using var confirmed = await fixture.Client.PostAsJsonAsync(control, input, ct);
        using var replayed = await fixture.Client.PostAsJsonAsync(control, input, ct);
        using var after = await fixture.Client.GetAsync(timeline, ct);
        var audit = await fixture.GetMaintenanceControlAudit(input.RequestId, ct);

        // Assert
        before.StatusCode.ShouldBe(HttpStatusCode.OK);
        unconfirmed.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        auditBeforeConfirmation.ShouldBeEmpty();
        confirmed.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        replayed.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        after.StatusCode.ShouldBe(HttpStatusCode.OK);
        var recorded = audit.ShouldHaveSingleItem();
        recorded.ActorId.ShouldBe(fixture.ConformanceUserId);
        recorded.Action.ShouldBe("Resume");
        recorded.Reason.ShouldBe(reason);
    }

    [Theory]
    [InlineData("drafts", "Drafts")]
    [InlineData("audit", "Audit")]
    [InlineData("idempotency", "Idempotency")]
    public async Task Authorized_timeline_reports_the_registered_jobs_durable_schedule(string job, string expectedJob)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var route = new Uri($"/api/v1/operations/document-maintenance/{job}", UriKind.Relative);

        // Act
        using var response = await fixture.Client.GetAsync(route, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        using var document = JsonDocument.Parse(content);
        var schedule = document.RootElement.GetProperty("schedule");
        schedule.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray()
            .ShouldBe(["activeRun", "fixedDelay", "isDue", "isPaused", "job", "latestRun", "nextDueAtUtc"]);
        var reportedJob = schedule.GetProperty("job").GetString().ShouldNotBeNull();
        reportedJob.ShouldBe(expectedJob);
        schedule.GetProperty("isPaused").GetBoolean().ShouldBeFalse();
        schedule.GetProperty("fixedDelay").GetString().ShouldBe("1.00:00:00");
    }
}
