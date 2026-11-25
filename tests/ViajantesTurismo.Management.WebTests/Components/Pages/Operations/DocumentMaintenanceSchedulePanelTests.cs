using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using SharedKernel.AspNetCore.Authentication.Bff;
using SharedKernel.Testing;
using SharedKernel.Testing.Http;
using ViajantesTurismo.Management.Web.Components.Pages.Operations;
using ContractConstants = ViajantesTurismo.Admin.Contracts.Application.ContractConstants;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Operations;

[TestScope(TestScope.Component)]
public sealed class DocumentMaintenanceSchedulePanelTests : BunitContext
{
    private const string ControllerRole = "OperationsScheduleController";
    private const string ReasonSelector = "#maintenance-control-reason";

    [Theory]
    [InlineData("Admin")]
    [InlineData("Operator")]
    [InlineData("Reader")]
    [InlineData(null)]
    public void Read_only_sessions_see_the_schedule_and_pending_retry_without_controls(string? role)
    {
        // Arrange
        using var scenario = new DocumentMaintenanceScheduleScenario(this, role)
        {
            Executions = """
                {"executions":[{"runId":"11111111-1111-1111-1111-111111111111","startedAtUtc":"2026-11-25T15:00:00Z",
                    "status":"RetryScheduled","retryAtUtc":"2026-11-25T16:00:00Z"}],"next":null}
                """,
        };

        // Act
        var panel = Render<DocumentMaintenanceSchedulePanel>(parameters => parameters.Add(item => item.Schedule, DocumentMaintenanceScheduleScenario.Schedule()));

        // Assert
        panel.Find("section[aria-label='Durable schedule']").TextContent.ShouldContain("Scheduled", StringComparison.Ordinal);
        panel.FindAll("section[aria-label='Durable schedule'] button").ShouldBeEmpty();
        panel.FindAll("table th").Select(heading => heading.TextContent).ShouldNotContain("Action");
        panel.FindAll("table button").ShouldBeEmpty();
        panel.FindAll("tbody td").Select(cell => cell.TextContent).ShouldBe(["—", "2026-11-25 15:00:00Z", "—", "Retry scheduled", "2026-11-25 16:00:00Z"]);
        scenario.Controls.ShouldBeEmpty();
    }

    [Fact]
    public void Controls_wait_until_the_scheduler_registers_the_job()
    {
        // Arrange
        using var scenario = new DocumentMaintenanceScheduleScenario(this, ControllerRole);

        // Act
        var panel = Render<DocumentMaintenanceSchedulePanel>(parameters => parameters.Add(item => item.Schedule, DocumentMaintenanceScheduleScenario.Schedule(null)));

        // Assert
        panel.Find("section[aria-label='Durable schedule']").TextContent.ShouldContain("Registration pending", StringComparison.Ordinal);
        panel.FindAll("section[aria-label='Durable schedule'] button").ShouldBeEmpty();
        scenario.Controls.ShouldBeEmpty();
    }

    [Fact]
    public void Shows_a_due_recurring_time_and_the_older_execution_with_no_recorded_outcome()
    {
        // Arrange
        using var scenario = new DocumentMaintenanceScheduleScenario(this, "Admin");
        var latest = new DocumentMaintenanceExecutionDto(Guid.CreateVersion7(), new DateTimeOffset(2026, 11, 24, 15, 0, 0, TimeSpan.Zero),
            null, null, null, DocumentMaintenanceExecutionStatus.Unknown);
        var schedule = DocumentMaintenanceScheduleScenario.Schedule() with
        {
            IsDue = true,
            LatestRun = latest,
        };

        // Act
        var panel = Render<DocumentMaintenanceSchedulePanel>(parameters => parameters.Add(item => item.Schedule, schedule));

        // Assert
        var state = panel.Find("section[aria-label='Durable schedule']").TextContent;
        state.ShouldContain("The recurring execution is due and waiting for a worker.", StringComparison.Ordinal);
        state.ShouldContain("The latest execution has neither a live lease nor a recorded outcome.", StringComparison.Ordinal);
        state.ShouldContain("Outcome unrecorded · started 2026-11-24 15:00:00Z", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false, "Pause schedule", "pause")]
    [InlineData(true, "Resume schedule", "resume")]
    public async Task A_dedicated_controller_can_confirm_the_applicable_schedule_transition(bool paused, string label, string path)
    {
        // Arrange
        using var scenario = new DocumentMaintenanceScheduleScenario(this, ControllerRole);
        var panel = Render<DocumentMaintenanceSchedulePanel>(parameters => parameters.Add(item => item.Schedule, DocumentMaintenanceScheduleScenario.Schedule(paused)));

        // Act
        await panel.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == label).ClickAsync();
        var requestsBeforeConfirmation = scenario.Controls.Count;
        await panel.Find(ReasonSelector).InputAsync(new ChangeEventArgs { Value = "  Planned maintenance  " });
        await panel.FindAll("fieldset button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Confirm control").ClickAsync();

        // Assert
        requestsBeforeConfirmation.ShouldBe(0);
        var request = scenario.Controls.ShouldHaveSingleItem();
        request.Message.RequestUri.ShouldNotBeNull().AbsolutePath.ShouldBe($"/api/v1/operations/document-maintenance/drafts/{path}");
        using var body = JsonDocument.Parse(request.Body.ShouldNotBeNull());
        body.RootElement.GetProperty("requestId").GetGuid().ShouldNotBe(Guid.Empty);
        body.RootElement.GetProperty("confirmed").GetBoolean().ShouldBeTrue();
        body.RootElement.GetProperty("reason").GetString().ShouldBe("Planned maintenance");
        body.RootElement.GetProperty("failedRunId").ValueKind.ShouldBe(JsonValueKind.Null);
        panel.Markup.ShouldNotContain("Planned maintenance", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invalid_reasons_are_rejected_and_cancel_discards_the_intent()
    {
        // Arrange
        using var scenario = new DocumentMaintenanceScheduleScenario(this, ControllerRole);
        var panel = Render<DocumentMaintenanceSchedulePanel>(parameters => parameters.Add(item => item.Schedule, DocumentMaintenanceScheduleScenario.Schedule()));

        // Act
        await panel.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Queue a run").ClickAsync();
        var limit = panel.Find(ReasonSelector).GetAttribute("maxlength");
        await panel.FindAll("fieldset button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Confirm control").ClickAsync();
        var rejectedBlank = panel.Find("[role='alert']").TextContent;
        await panel.Find(ReasonSelector).InputAsync(new ChangeEventArgs { Value = new string('x', ContractConstants.MaxDocumentMaintenanceControlReasonLength + 1) });
        await panel.FindAll("fieldset button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Confirm control").ClickAsync();
        var rejectedLong = panel.Find("[role='alert']").TextContent;
        await panel.Find(ReasonSelector).InputAsync(new ChangeEventArgs { Value = "Check\0retention" });
        await panel.FindAll("fieldset button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Confirm control").ClickAsync();
        var rejectedNullCharacter = panel.Find("[role='alert']").TextContent;
        await panel.FindAll("fieldset button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Cancel").ClickAsync();

        // Assert
        limit.ShouldBe("500");
        rejectedBlank.ShouldBe("Enter a reason of up to 500 characters.");
        rejectedLong.ShouldBe(rejectedBlank);
        rejectedNullCharacter.ShouldBe(rejectedBlank);
        panel.FindAll(ReasonSelector).ShouldBeEmpty();
        scenario.Controls.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_lost_control_response_resends_the_same_intent_and_a_paused_run_stays_queued()
    {
        // Arrange
        using var scenario = new DocumentMaintenanceScheduleScenario(this, ControllerRole)
        {
            NextControlFailure = new HttpRequestException("synthetic-private-control-detail"),
        };
        var active = new DocumentMaintenanceExecutionDto(Guid.CreateVersion7(), new DateTimeOffset(2026, 11, 25, 14, 0, 0, TimeSpan.Zero),
            null, null, null, DocumentMaintenanceExecutionStatus.Running);
        var schedule = DocumentMaintenanceScheduleScenario.Schedule(paused: true) with
        {
            ActiveRun = active,
        };
        var panel = Render<DocumentMaintenanceSchedulePanel>(parameters => parameters.Add(item => item.Schedule, schedule));

        // Act
        await panel.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Queue a run").ClickAsync();
        await panel.Find(ReasonSelector).InputAsync(new ChangeEventArgs { Value = "Check the retention drain" });
        await panel.FindAll("fieldset button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Confirm control").ClickAsync();
        var failedMessage = panel.Find("[role='alert']").TextContent;
        var failedMarkup = panel.Markup;
        await panel.FindAll("fieldset button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Resend same request").ClickAsync();

        // Assert
        failedMessage.ShouldBe("The request outcome is unknown. You can resend the same confirmed request.");
        failedMarkup.ShouldNotContain("synthetic-private-control-detail", StringComparison.Ordinal);
        failedMarkup.ShouldNotContain("Check the retention drain", StringComparison.Ordinal);
        scenario.Controls.Select(request => request.Message.RequestUri.ShouldNotBeNull().AbsolutePath)
            .ShouldBe(["/api/v1/operations/document-maintenance/drafts/trigger", "/api/v1/operations/document-maintenance/drafts/trigger"]);
        scenario.Controls.Select(request => request.Body).Distinct().ShouldHaveSingleItem().ShouldNotBeNull();
        var scheduleMarkup = panel.Find("section[aria-label='Durable schedule']").TextContent;
        scheduleMarkup.ShouldContain("Paused", StringComparison.Ordinal);
        scheduleMarkup.ShouldContain("Running since 2026-11-25 14:00:00Z", StringComparison.Ordinal);
        scheduleMarkup.ShouldContain("Run queued. Queued executions wait while the schedule is paused.", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task A_denied_response_discards_the_reason_and_disables_further_controls(HttpStatusCode status)
    {
        // Arrange
        using var scenario = new DocumentMaintenanceScheduleScenario(this, ControllerRole) { ControlStatus = status };
        var panel = Render<DocumentMaintenanceSchedulePanel>(parameters => parameters.Add(item => item.Schedule, DocumentMaintenanceScheduleScenario.Schedule()));

        // Act
        await panel.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Pause schedule").ClickAsync();
        await panel.Find(ReasonSelector).InputAsync(new ChangeEventArgs { Value = "Private audit reason" });
        await panel.FindAll("fieldset button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Confirm control").ClickAsync();

        // Assert
        scenario.Controls.ShouldHaveSingleItem();
        panel.Find("[role='alert']").TextContent.ShouldBe("Schedule control is unavailable for this session.");
        panel.FindAll("section[aria-label='Durable schedule'] button").ShouldBeEmpty();
        panel.Markup.ShouldNotContain("Private audit reason", StringComparison.Ordinal);
        panel.Markup.ShouldNotContain("synthetic-private-control-detail", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Missing_control_token_discards_the_confirmation_and_keeps_details_private()
    {
        // Arrange
        using var scenario = new DocumentMaintenanceScheduleScenario(this, ControllerRole)
        {
            NextControlFailure = new UserTokenUnavailableException("synthetic-private-control-token"),
        };
        var panel = Render<DocumentMaintenanceSchedulePanel>(parameters => parameters.Add(item => item.Schedule, DocumentMaintenanceScheduleScenario.Schedule()));

        // Act
        await panel.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Pause schedule").ClickAsync();
        await panel.Find(ReasonSelector).InputAsync(new ChangeEventArgs { Value = "Document retention review" });
        await panel.FindAll("fieldset button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Confirm control").ClickAsync();

        // Assert
        panel.Find("[role='alert']").TextContent.ShouldBe("Schedule control is unavailable for this session.");
        panel.FindAll("section[aria-label='Durable schedule'] button").ShouldBeEmpty();
        panel.Markup.ShouldNotContain("Document retention review", StringComparison.Ordinal);
        panel.Markup.ShouldNotContain("synthetic-private-control-token", StringComparison.Ordinal);
        scenario.Reads.ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Failed_continuation_is_unavailable_and_latest_executions_recover(bool tokenUnavailable)
    {
        // Arrange
        using var scenario = new DocumentMaintenanceScheduleScenario(this, ControllerRole)
        {
            Executions = """
                {"executions":[{"runId":"11111111-1111-1111-1111-111111111111","startedAtUtc":"2026-11-25T15:00:00Z","status":"Succeeded"}],
                 "next":{"startedAtUtc":"2026-11-25T15:00:00Z","runId":"11111111-1111-1111-1111-111111111111"}}
                """,
        };
        var panel = Render<DocumentMaintenanceSchedulePanel>(parameters => parameters.Add(item => item.Schedule, DocumentMaintenanceScheduleScenario.Schedule()));
        scenario.ExecutionStatus = tokenUnavailable ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;
        scenario.NextExecutionFailure = tokenUnavailable ? new UserTokenUnavailableException("synthetic-private-execution-token") : null;

        // Act
        await panel.FindAll("nav button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Earlier executions").ClickAsync();
        var unavailableMessage = panel.Find("[role='alert']").TextContent;
        var unavailableMarkup = panel.Markup;
        var earlierDisabled = panel.FindAll("nav button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Earlier executions").HasAttribute("disabled");
        scenario.ExecutionStatus = HttpStatusCode.OK;
        await panel.FindAll("nav button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Latest executions").ClickAsync();

        // Assert
        unavailableMessage.ShouldBe("Scheduler executions are unavailable.");
        unavailableMarkup.ShouldNotContain("synthetic-private-execution-token", StringComparison.Ordinal);
        unavailableMarkup.ShouldNotContain("No recorded executions.", StringComparison.Ordinal);
        unavailableMarkup.ShouldNotContain("Succeeded", StringComparison.Ordinal);
        earlierDisabled.ShouldBeTrue();
        panel.Find("tbody tr").TextContent.ShouldContain("Succeeded", StringComparison.Ordinal);
        scenario.Reads.Select(request => string.IsNullOrEmpty(request.Message.RequestUri?.Query)).ShouldBe([true, false, true]);
    }

    [Fact]
    public async Task A_control_accepted_during_an_execution_read_does_not_start_a_second_read()
    {
        // Arrange
        using var scenario = new DocumentMaintenanceScheduleScenario(this, ControllerRole);
        using var handler = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json("""{"executions":[],"next":null}""", HttpStatusCode.OK));
        using var gate = new GatedHttpMessageHandler(handler);
        using var http = new HttpClient(gate, disposeHandler: false) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IDocumentMaintenanceExecutionsApiClient>(new DocumentMaintenanceApiClient(http));
        var panel = Render<DocumentMaintenanceSchedulePanel>(parameters => parameters.Add(item => item.Schedule, DocumentMaintenanceScheduleScenario.Schedule()));
        await gate.WaitForRequests(1, Xunit.TestContext.Current.CancellationToken);

        // Act
        var loadingMessage = panel.Find("section[aria-label='Scheduler executions'] [role='status']").TextContent;
        var pagerDisabled = panel.FindAll("nav button").All(button => button.HasAttribute("disabled"));
        await panel.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Queue a run").ClickAsync();
        await panel.Find(ReasonSelector).InputAsync(new ChangeEventArgs { Value = "Check the retention drain" });
        await panel.FindAll("fieldset button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Confirm control").ClickAsync();
        gate.Release();

        // Assert
        loadingMessage.ShouldBe("Loading scheduler executions…");
        pagerDisabled.ShouldBeTrue();
        gate.Requests.ShouldHaveSingleItem();
        scenario.Controls.ShouldHaveSingleItem();
        panel.WaitForAssertion(() => panel.Find("section[aria-label='Scheduler executions']").TextContent.ShouldContain("No recorded executions.", StringComparison.Ordinal));
        panel.Find("section[aria-label='Durable schedule'] [role='status']").TextContent.ShouldContain("Run queued.", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task A_rejected_control_reloads_the_real_timeline_before_a_new_confirmation(HttpStatusCode status)
    {
        // Arrange
        using var scenario = new DocumentMaintenanceScheduleScenario(this, ControllerRole) { ControlStatus = status };
        using var handler = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(scenario.Controls.Count == 1
            ? """{"runs":[],"next":null,"schedule":{"job":"Drafts","isPaused":true}}"""
            : """{"runs":[],"next":null,"schedule":{"job":"Drafts","isPaused":false}}""", HttpStatusCode.OK));
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IDocumentMaintenanceTimelineApiClient>(new DocumentMaintenanceApiClient(http));
        var timeline = Render<DocumentMaintenanceTimeline>(parameters => parameters.Add(item => item.Job, "drafts"));

        // Act
        await timeline.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Pause schedule").ClickAsync();
        await timeline.Find(ReasonSelector).InputAsync(new ChangeEventArgs { Value = "First confirmed reason" });
        await timeline.FindAll("fieldset button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Confirm control").ClickAsync();
        var rejection = timeline.Find("[role='alert']").TextContent;
        var rejectedMarkup = timeline.Markup;
        scenario.ControlStatus = HttpStatusCode.NoContent;
        await timeline.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Resume schedule").ClickAsync();
        await timeline.Find(ReasonSelector).InputAsync(new ChangeEventArgs { Value = "Confirm current state" });
        await timeline.FindAll("fieldset button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Confirm control").ClickAsync();

        // Assert
        rejection.ShouldBe("The schedule could not accept this action. Review the current state before confirming again.");
        rejectedMarkup.ShouldNotContain("First confirmed reason", StringComparison.Ordinal);
        rejectedMarkup.ShouldNotContain("synthetic-private-", StringComparison.Ordinal);
        rejectedMarkup.ShouldNotContain("Resend same request", StringComparison.Ordinal);
        timeline.Find("[role='status']").TextContent.ShouldBe("Resume request accepted.");
        timeline.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Pause schedule");
        handler.Requests.ShouldHaveCount(3);
        scenario.Reads.ShouldHaveCount(3);
        var controls = scenario.Controls;
        controls.Select(request => request.Message.RequestUri.ShouldNotBeNull().AbsolutePath).ShouldBe([
            "/api/v1/operations/document-maintenance/drafts/pause", "/api/v1/operations/document-maintenance/drafts/resume"]);
        using var before = JsonDocument.Parse(controls[0].Body.ShouldNotBeNull());
        using var after = JsonDocument.Parse(controls[1].Body.ShouldNotBeNull());
        before.RootElement.GetProperty("requestId").GetGuid().ShouldNotBe(after.RootElement.GetProperty("requestId").GetGuid());
    }

    [Fact]
    public async Task A_pending_control_does_not_reload_a_different_timeline_after_navigation()
    {
        // Arrange
        using var scenario = new DocumentMaintenanceScheduleScenario(this, ControllerRole);
        using var controlHandler = new RecordingHttpMessageHandler(HttpStatusCode.NoContent);
        using var controlGate = new GatedHttpMessageHandler(controlHandler);
        using var controlHttp = new HttpClient(controlGate, disposeHandler: false) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IDocumentMaintenanceControlApiClient>(new DocumentMaintenanceControlApiClient(controlHttp));
        using var timelineHandler = new RecordingHttpMessageHandler(request => HttpResponseFactory.Json(
            request.Message.RequestUri?.AbsolutePath.EndsWith("/audit", StringComparison.Ordinal) == true
                ? """{"runs":[],"next":null,"schedule":{"job":"Audit","isPaused":false}}"""
                : """{"runs":[],"next":null,"schedule":{"job":"Drafts","isPaused":false}}""", HttpStatusCode.OK));
        using var timelineHttp = new HttpClient(timelineHandler) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IDocumentMaintenanceTimelineApiClient>(new DocumentMaintenanceApiClient(timelineHttp));
        var timeline = Render<DocumentMaintenanceTimeline>(parameters => parameters.Add(item => item.Job, "drafts"));
        await timeline.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Pause schedule").ClickAsync();
        await timeline.Find(ReasonSelector).InputAsync(new ChangeEventArgs { Value = "Review draft retention" });

        // Act
        var confirmation = timeline.FindAll("fieldset button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Confirm control").ClickAsync();
        await controlGate.WaitForRequests(1, Xunit.TestContext.Current.CancellationToken);
        timeline.WaitForState(() => timeline.FindAll("fieldset button").Count(button => button.HasAttribute("disabled")) == 2);
        timeline.Render(parameters => parameters.Add(item => item.Job, "audit"));
        controlGate.Release();
        await confirmation;

        // Assert
        timeline.Find("h1").TextContent.ShouldBe("Audit retention timeline");
        timeline.FindAll("[role='status']").ShouldBeEmpty();
        timelineHandler.Requests.Select(request => request.Message.RequestUri.ShouldNotBeNull().AbsolutePath).ShouldBe([
            "/api/v1/operations/document-maintenance/drafts", "/api/v1/operations/document-maintenance/audit"]);
        controlGate.Requests.ShouldHaveSingleItem().Message.RequestUri.ShouldNotBeNull().AbsolutePath
            .ShouldBe("/api/v1/operations/document-maintenance/drafts/pause");
    }

    [Fact]
    public async Task Retry_uses_the_exact_earlier_failed_execution_after_a_newer_success()
    {
        // Arrange
        const string failedId = "33333333-3333-3333-3333-333333333333";
        using var scenario = new DocumentMaintenanceScheduleScenario(this, ControllerRole)
        {
            Executions = """
                {"executions":[
                    {"runId":"11111111-1111-1111-1111-111111111111","startedAtUtc":"2026-11-25T16:00:00Z","status":"Succeeded"},
                    {"runId":"22222222-2222-2222-2222-222222222222","startedAtUtc":"2026-11-25T15:00:00Z","status":"TerminalFailure"}
                ],"next":{"startedAtUtc":"2026-11-25T15:00:00Z","runId":"22222222-2222-2222-2222-222222222222"}}
                """,
            EarlierExecutions = $$$"""
                {"executions":[{"runId":"{{{failedId}}}","startedAtUtc":"2026-11-24T15:00:00Z","status":"Failed"}],"next":null}
                """,
        };
        var panel = Render<DocumentMaintenanceSchedulePanel>(parameters => parameters.Add(item => item.Schedule, DocumentMaintenanceScheduleScenario.Schedule()));

        // Act
        var initiallyAvailableRetries = panel.FindAll("table button").Count;
        await panel.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Earlier executions").ClickAsync();
        var cursorRequest = scenario.Reads.ShouldHaveSingleItem(request => !string.IsNullOrEmpty(request.Message.RequestUri?.Query));
        await panel.FindAll("table button").ShouldHaveSingleItem().ClickAsync();
        var confirmationTarget = panel.Find("fieldset code").TextContent;
        await panel.Find(ReasonSelector).InputAsync(new ChangeEventArgs { Value = "Retry the failed batch" });
        await panel.FindAll("fieldset button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Confirm control").ClickAsync();

        // Assert
        initiallyAvailableRetries.ShouldBe(0);
        confirmationTarget.ShouldBe(failedId);
        cursorRequest.Message.RequestUri.ShouldNotBeNull().Query.ShouldContain("22222222-2222-2222-2222-222222222222", StringComparison.Ordinal);
        var request = scenario.Controls.ShouldHaveSingleItem();
        request.Message.RequestUri.ShouldNotBeNull().AbsolutePath.ShouldBe("/api/v1/operations/document-maintenance/drafts/retry");
        using var body = JsonDocument.Parse(request.Body.ShouldNotBeNull());
        body.RootElement.GetProperty("failedRunId").GetString().ShouldBe(failedId);
    }
}
