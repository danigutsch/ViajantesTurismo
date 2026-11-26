using SharedKernel.HttpClients;
using SharedKernel.Results;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents.Maintenance;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.Http;

namespace ViajantesTurismo.Admin.ApiServiceTests;

[Trait(TestTraitNames.CategoryName, TestTraitValues.EndpointCategory)]
[TestHost(TestHost.TestServer)]
public sealed class DocumentMaintenanceControlEndpointTests
{
    private const string Endpoint = "/api/v1/operations/document-maintenance";
    private const string ControllerRole = "OperationsScheduleController";
    private const string Reason = "Operator confirmed the maintenance change.";

    [Theory]
    [InlineData("missing", "trigger")]
    [InlineData("0", "trigger")]
    [InlineData("drafts", "missing")]
    [InlineData("drafts", "0")]
    public async Task Unknown_or_numeric_route_names_do_not_dispatch_a_control(string job, string action)
    {
        // Arrange
        var store = new RecordingMaintenanceControlStore(Result.Ok());
        await using var factory = AdminApiTestHost.Create(services => services.AddSingleton<IDocumentMaintenanceControlStore>(store));
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, ControllerRole);
        var input = new CreateDocumentMaintenanceControlDto
        {
            RequestId = Guid.CreateVersion7(),
            Confirmed = true,
            Reason = Reason,
        };

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri($"{Endpoint}/{job}/{action}", UriKind.Relative), input, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        store.Received.ShouldBeNull();
    }

    [Theory]
    [InlineData("Drafts", "PaUsE", DocumentMaintenanceJob.Drafts, DocumentMaintenanceControlAction.Pause, HttpStatusCode.NoContent)]
    [InlineData("audit", "resume", DocumentMaintenanceJob.Audit, DocumentMaintenanceControlAction.Resume, HttpStatusCode.NotFound)]
    [InlineData("idempotency", "retry", DocumentMaintenanceJob.Idempotency, DocumentMaintenanceControlAction.Retry, HttpStatusCode.Conflict)]
    public async Task Confirmed_control_maps_the_store_outcome_and_authenticated_actor(
        string job, string action, DocumentMaintenanceJob expectedJob, DocumentMaintenanceControlAction expectedAction, HttpStatusCode expectedStatus)
    {
        // Arrange
        var result = expectedStatus switch
        {
            HttpStatusCode.NotFound => Result.NotFound("The selected maintenance job is unavailable."),
            HttpStatusCode.Conflict => Result.Conflict("The selected execution has already advanced."),
            _ => Result.Ok(),
        };
        var store = new RecordingMaintenanceControlStore(result);
        await using var factory = AdminApiTestHost.Create(services => services.AddSingleton<IDocumentMaintenanceControlStore>(store));
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, ControllerRole);
        var input = new CreateDocumentMaintenanceControlDto
        {
            RequestId = Guid.CreateVersion7(),
            Confirmed = true,
            Reason = $"  {Reason} ",
            FailedRunId = expectedAction == DocumentMaintenanceControlAction.Retry ? Guid.CreateVersion7() : null,
        };

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri($"{Endpoint}/{job}/{action}", UriKind.Relative), input, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(expectedStatus);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        var received = store.Received.ShouldNotBeNull();
        received.Job.ShouldBe(expectedJob);
        received.Action.ShouldBe(expectedAction);
        received.ActorId.ShouldBe("test-user");
        received.Input.ShouldBe(input);
        store.Reason.ShouldBe(Reason);
    }

    [Fact]
    public async Task Retry_requires_a_nonempty_failed_execution_and_other_controls_reject_it()
    {
        // Arrange
        var store = new RecordingMaintenanceControlStore(Result.Ok());
        await using var factory = AdminApiTestHost.Create(services => services.AddSingleton<IDocumentMaintenanceControlStore>(store));
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, ControllerRole);
        var input = new CreateDocumentMaintenanceControlDto
        {
            RequestId = Guid.CreateVersion7(),
            Confirmed = true,
            Reason = Reason,
        };
        var emptyExecution = input with
        {
            FailedRunId = Guid.Empty,
        };
        var unexpectedExecution = input with
        {
            FailedRunId = Guid.CreateVersion7(),
        };
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var missingTarget = await client.PostAsJsonAsync(new Uri(Endpoint + "/drafts/retry", UriKind.Relative), input, ct);
        using var emptyTarget = await client.PostAsJsonAsync(
            new Uri(Endpoint + "/drafts/retry", UriKind.Relative), emptyExecution, ct);
        using var strayTarget = await client.PostAsJsonAsync(
            new Uri(Endpoint + "/drafts/trigger", UriKind.Relative), unexpectedExecution, ct);

        // Assert
        missingTarget.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        emptyTarget.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        strayTarget.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        store.Received.ShouldBeNull();
    }

    [Theory]
    [InlineData(DocumentMaintenanceJob.Drafts, DocumentMaintenanceControlKind.Pause, DocumentMaintenanceControlAction.Pause)]
    [InlineData(DocumentMaintenanceJob.Audit, DocumentMaintenanceControlKind.Resume, DocumentMaintenanceControlAction.Resume)]
    [InlineData(DocumentMaintenanceJob.Idempotency, DocumentMaintenanceControlKind.Trigger, DocumentMaintenanceControlAction.Trigger)]
    [InlineData(DocumentMaintenanceJob.Drafts, DocumentMaintenanceControlKind.Retry, DocumentMaintenanceControlAction.Retry)]
    public async Task Typed_client_binds_each_confirmed_control_to_its_job_and_action(
        DocumentMaintenanceJob job, DocumentMaintenanceControlKind kind, DocumentMaintenanceControlAction expected)
    {
        // Arrange
        var store = new RecordingMaintenanceControlStore(Result.Ok());
        await using var factory = AdminApiTestHost.Create(services => services.AddSingleton<IDocumentMaintenanceControlStore>(store));
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, ControllerRole);
        var input = new CreateDocumentMaintenanceControlDto
        {
            RequestId = Guid.CreateVersion7(),
            Confirmed = true,
            Reason = Reason,
            FailedRunId = kind == DocumentMaintenanceControlKind.Retry ? Guid.CreateVersion7() : null,
        };
        var controls = new DocumentMaintenanceControlApiClient(client);

        // Act
        var outcome = await controls.Apply(job, kind, input, TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        var received = store.Received.ShouldNotBeNull();
        received.Job.ShouldBe(job);
        received.Action.ShouldBe(expected);
        received.Input.ShouldBe(input);
    }
}
