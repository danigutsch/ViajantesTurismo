using System.Net;
using SharedKernel.Testing.Http;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Operations;

/// <summary>Registers both authenticated API clients over an inspectable in-memory transport.</summary>
internal sealed class DocumentMaintenanceScheduleScenario : IDisposable
{
    private readonly RecordingHttpMessageHandler _transport;
    private readonly HttpClient _client;

    /// <summary>Registers authenticated clients for this role and captures their requests.</summary>
    public DocumentMaintenanceScheduleScenario(BunitContext context, string? role)
    {
        if (role is not null)
        {
            var authorization = context.AddAuthorization();
            authorization.SetAuthorized("scheduler@example.test", AuthorizationState.Authorized);
            authorization.SetRoles(role);
        }
        _transport = new RecordingHttpMessageHandler(Respond);
        _client = new HttpClient(_transport, disposeHandler: false) { BaseAddress = new Uri("https://admin.example.test") };
        context.Services.AddSingleton<IDocumentMaintenanceExecutionsApiClient>(new DocumentMaintenanceApiClient(_client));
        context.Services.AddSingleton<IDocumentMaintenanceControlApiClient>(new DocumentMaintenanceControlApiClient(_client));
    }

    /// <summary>Creates a schedule in the requested registration and pause state.</summary>
    public static DocumentMaintenanceScheduleDto Schedule(bool? paused = false) =>
        new(DocumentMaintenanceJob.Drafts, paused, paused.HasValue ? new DateTimeOffset(2026, 11, 25, 15, 0, 0, TimeSpan.Zero) : null,
            paused.HasValue ? TimeSpan.FromDays(1) : null, null, null);

    /// <summary>Gets or sets the first page of executions returned by the API.</summary>
    public string Executions
    {
        get;
        set;
    } = """{"executions":[],"next":null}""";

    /// <summary>Gets or sets the execution page returned for a continuation request.</summary>
    public string? EarlierExecutions
    {
        get;
        set;
    }

    /// <summary>Gets or sets the execution request response status.</summary>
    public HttpStatusCode ExecutionStatus
    {
        get;
        set;
    } = HttpStatusCode.OK;

    /// <summary>Gets or sets a failure that occurs on the next execution read.</summary>
    public Exception? NextExecutionFailure
    {
        get;
        set;
    }

    /// <summary>Gets or sets the API response status for a control.</summary>
    public HttpStatusCode ControlStatus
    {
        get;
        set;
    } = HttpStatusCode.NoContent;

    /// <summary>Gets or sets a failure that occurs on the next control request.</summary>
    public Exception? NextControlFailure
    {
        get;
        set;
    }

    /// <summary>Gets the captured schedule control requests.</summary>
    public IReadOnlyList<CapturedHttpRequest> Controls => [.. _transport.Requests.Where(request => request.Method == HttpMethod.Post)];

    /// <summary>Gets the captured execution page requests.</summary>
    public IReadOnlyList<CapturedHttpRequest> Reads => [.. _transport.Requests.Where(request => request.Method == HttpMethod.Get)];

    /// <inheritdoc />
    public void Dispose()
    {
        _client.Dispose();
        _transport.Dispose();
    }

    private HttpResponseMessage Respond(CapturedHttpRequest request)
    {
        if (request.Method == HttpMethod.Get)
        {
            if (NextExecutionFailure is { } executionFailure)
            {
                NextExecutionFailure = null;
                throw executionFailure;
            }

            var payload = string.IsNullOrEmpty(request.Message.RequestUri?.Query) ? Executions : EarlierExecutions ?? Executions;
            return HttpResponseFactory.Json(payload, ExecutionStatus);
        }

        if (NextControlFailure is { } controlFailure)
        {
            NextControlFailure = null;
            throw controlFailure;
        }

        return ControlStatus == HttpStatusCode.NoContent ? new HttpResponseMessage(ControlStatus)
            : HttpResponseFactory.Json(
                """{"detail":"synthetic-private-control-detail","errors":{"reason":["synthetic-private-validation-detail"]}}""",
                ControlStatus);
    }
}
