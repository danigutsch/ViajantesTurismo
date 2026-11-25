using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using SharedKernel.AspNetCore.Authentication.Bff;
using SharedKernel.Testing;
using SharedKernel.Testing.Http;
using ViajantesTurismo.Management.Web.Components.Pages.Operations;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Operations;

[TestScope(TestScope.Component)]
public sealed class DocumentMaintenanceTimelineTests : BunitContext
{
    public DocumentMaintenanceTimelineTests()
    {
        AddAuthorization();
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true).SetVoidResult();
        Services.AddHttpClient<IDocumentMaintenanceExecutionsApiClient, DocumentMaintenanceApiClient>(
                client => client.BaseAddress = new Uri("https://admin.example.test"))
            .ConfigurePrimaryHttpMessageHandler(() => new RecordingHttpMessageHandler(_ =>
                HttpResponseFactory.Json("""{"executions":[],"next":null}""", HttpStatusCode.OK)));
        Services.AddHttpClient<IDocumentMaintenanceControlApiClient, DocumentMaintenanceControlApiClient>(
                client => client.BaseAddress = new Uri("https://admin.example.test"))
            .ConfigurePrimaryHttpMessageHandler(() => new RecordingHttpMessageHandler(HttpStatusCode.Forbidden));
    }

    [Fact]
    public void Maintenance_timeline_requires_admin_or_the_dedicated_schedule_controller_role()
    {
        // Arrange
        var type = typeof(DocumentMaintenanceTimeline);

        // Act
        var route = type.GetCustomAttributes(typeof(RouteAttribute), false).OfType<RouteAttribute>().ShouldHaveSingleItem();
        var policy = type.GetCustomAttributes(typeof(AuthorizeAttribute), false).OfType<AuthorizeAttribute>().ShouldHaveSingleItem();

        // Assert
        route.Template.ShouldBe("/operations/document-maintenance/{Job}");
        policy.Roles.ShouldBe("Admin,OperationsScheduleController");
    }

    [Theory]
    [InlineData("true", "Paused")]
    [InlineData("false", "Scheduled")]
    [InlineData("null", "Registration pending")]
    public void Timeline_explains_persisted_schedule_state(string paused, string status)
    {
        // Arrange
        var json = $$$"""
            {"runs":[],"next":null,"schedule":{"job":"Drafts","isPaused":{{{paused}}},"fixedDelay":"1.00:00:00",
            "nextDueAtUtc":"2026-11-25T08:00:00Z","activeRun":null,"latestRun":null}}
            """;
        using var transport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(json, HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IDocumentMaintenanceTimelineApiClient>(new DocumentMaintenanceApiClient(client));

        // Act
        var page = Render<DocumentMaintenanceTimeline>(parameters => parameters.Add(item => item.Job, "drafts"));

        // Assert
        var schedule = page.Find("section[aria-label='Durable schedule']");
        schedule.TextContent.ShouldContain(status, StringComparison.Ordinal);
        schedule.TextContent.ShouldNotContain("polling cycle", StringComparison.Ordinal);
        page.Markup.ShouldContain("No recorded batches.", StringComparison.Ordinal);
        transport.Requests.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Switching_jobs_keeps_the_selected_job_when_an_older_request_finishes(bool tokenUnavailable)
    {
        // Arrange
        var client = new DeferredDraftTimelineApiClient();
        Services.AddSingleton<IDocumentMaintenanceTimelineApiClient>(client);
        var page = Render<DocumentMaintenanceTimeline>(parameters => parameters.Add(item => item.Job, "drafts"));
        page.Find("[role='status']").TextContent.ShouldContain("Loading", StringComparison.Ordinal);

        // Act
        page.Render(parameters => parameters.Add(item => item.Job, "audit"));
        var visibleRenders = page.RenderCount;
        client.CompleteDrafts(tokenUnavailable);
        page.WaitForState(() => page.RenderCount > visibleRenders);

        // Assert
        page.Find("h1").TextContent.ShouldBe("Audit retention timeline");
        page.FindAll("tbody td").Select(cell => cell.TextContent).ShouldBe(["2026-11-24 08:00:00Z", "Succeeded", "31", "0"]);
        page.FindAll("[role='alert']").ShouldBeEmpty();
    }

    [Fact]
    public async Task Earlier_page_uses_only_the_selected_job_and_its_continuation()
    {
        // Arrange
        var runId = Guid.CreateVersion7();
        var first = $$$"""
            {"runs":[{"occurredAtUtc":"2026-11-24T08:00:00Z","purgedCount":20,"heldCount":1,
            "outcome":"Succeeded","actorId":"synthetic-private-actor"}],
            "next":{"occurredAtUtc":"2026-11-24T08:00:00Z","runId":"{{{runId:D}}}"}}
            """;
        const string last = """
            {"runs":[{"occurredAtUtc":"2026-11-23T08:00:00Z","purgedCount":2,"heldCount":1,"outcome":"Refused"}],"next":null}
            """;
        using var transport = new RecordingHttpMessageHandler(request =>
            HttpResponseFactory.Json(string.IsNullOrEmpty(request.Message.RequestUri?.Query) ? first : last, HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IDocumentMaintenanceTimelineApiClient>(new DocumentMaintenanceApiClient(client));

        // Act
        var page = Render<DocumentMaintenanceTimeline>(parameters => parameters.Add(item => item.Job, "drafts"));
        var firstMarkup = page.Markup;
        var firstRow = page.FindAll("tbody td").Select(cell => cell.TextContent).ToArray();
        await page.FindAll("nav button").ShouldHaveSingleItem(button => button.TextContent == "Earlier page").ClickAsync();
        var lastRow = page.FindAll("tbody td").Select(cell => cell.TextContent).ToArray();
        var earlierDisabled = page.FindAll("nav button").ShouldHaveSingleItem(button => button.TextContent == "Earlier page").HasAttribute("disabled");
        await page.FindAll("nav button").ShouldHaveSingleItem(button => button.TextContent == "Latest page").ClickAsync();
        var returnRow = page.FindAll("tbody td").Select(cell => cell.TextContent).ToArray();

        // Assert
        page.Find("h1").TextContent.ShouldBe("Draft retention timeline");
        firstMarkup.ShouldContain("Schedule details are unavailable.", StringComparison.Ordinal);
        firstRow.ShouldBe(["2026-11-24 08:00:00Z", "Succeeded", "20", "1"]);
        lastRow.ShouldBe(["2026-11-23 08:00:00Z", "Refused", "2", "1"]);
        returnRow.ShouldBe(firstRow);
        earlierDisabled.ShouldBeTrue();
        firstMarkup.ShouldNotContain("synthetic-private-actor", StringComparison.Ordinal);
        var requests = transport.Requests.Select(request => request.Message).ToArray();
        requests.Select(request => request.Method).ShouldBe([HttpMethod.Get, HttpMethod.Get, HttpMethod.Get]);
        requests.Select(request => request.RequestUri?.AbsolutePath).Distinct().ShouldHaveSingleItem()
            .ShouldBe("/api/v1/operations/document-maintenance/drafts");
        var continuation = QueryHelpers.ParseQuery(requests[1].RequestUri.ShouldNotBeNull().Query);
        continuation["beforeRunId"].ToString().ShouldBe(runId.ToString("D"));
        continuation["beforeOccurredAt"].ToString().ShouldBe("2026-11-24T08:00:00.0000000+00:00");
    }

    [Fact]
    public async Task Missing_token_keeps_failure_details_private_and_allows_retry()
    {
        // Arrange
        var attempts = 0;
        using var transport = new RecordingHttpMessageHandler(_ => ++attempts == 1
            ? throw new UserTokenUnavailableException("synthetic-private-session")
            : HttpResponseFactory.Json("""{"runs":[],"next":null}""", HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IDocumentMaintenanceTimelineApiClient>(new DocumentMaintenanceApiClient(client));

        // Act
        var page = Render<DocumentMaintenanceTimeline>(parameters => parameters.Add(item => item.Job, "audit"));
        var failure = page.Find("[role='alert']").TextContent;
        var failedMarkup = page.Markup;
        await page.FindAll("nav button").ShouldHaveSingleItem(button => button.TextContent == "Latest page").ClickAsync();

        // Assert
        page.Find("h1").TextContent.ShouldBe("Audit retention timeline");
        failure.ShouldBe("Maintenance history is unavailable.");
        failedMarkup.ShouldNotContain("synthetic-private-session", StringComparison.Ordinal);
        page.Markup.ShouldContain("No recorded batches.", StringComparison.Ordinal);
        transport.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public void Unknown_maintenance_job_does_not_call_the_API()
    {
        // Arrange
        using var transport = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IDocumentMaintenanceTimelineApiClient>(new DocumentMaintenanceApiClient(client));

        // Act
        var page = Render<DocumentMaintenanceTimeline>(parameters => parameters.Add(item => item.Job, "unknown"));

        // Assert
        page.Find("[role='status']").TextContent.ShouldBe("Maintenance job not found.");
        transport.Requests.ShouldBeEmpty();
    }

    [Fact]
    public void Idempotency_timeline_reports_a_failed_admin_response()
    {
        // Arrange
        using var transport = new RecordingHttpMessageHandler(HttpStatusCode.BadGateway);
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IDocumentMaintenanceTimelineApiClient>(new DocumentMaintenanceApiClient(client));

        // Act
        var page = Render<DocumentMaintenanceTimeline>(parameters => parameters.Add(item => item.Job, "idempotency"));

        // Assert
        page.Find("h1").TextContent.ShouldBe("Idempotency retention timeline");
        page.Find("[role='alert']").TextContent.ShouldBe("Maintenance history is unavailable.");
        transport.Requests.ShouldHaveSingleItem().Message.RequestUri.ShouldNotBeNull().AbsolutePath
            .ShouldBe("/api/v1/operations/document-maintenance/idempotency");
    }
}
