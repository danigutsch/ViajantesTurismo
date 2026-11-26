using System.Globalization;
using System.Net;
using System.Text.Json.Serialization.Metadata;
using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Reads the document maintenance history through the authenticated Admin API.</summary>
/// <param name="httpClient">The configured Admin API client.</param>
public sealed class DocumentMaintenanceApiClient(HttpClient httpClient) :
    IDocumentMaintenanceApiClient, IDocumentMaintenanceTimelineApiClient, IDocumentMaintenanceExecutionsApiClient
{
    private const string MaintenanceRoute = "/api/v1/operations/document-maintenance";

    /// <inheritdoc />
    public Task<HttpOutcome<DocumentMaintenanceHistoryDto>> Read(CancellationToken ct) =>
        ReadRequiredCollections(MaintenanceRoute, DocumentsApiClientJsonContext.Default.DocumentMaintenanceHistoryDto,
            static history => IsComplete(history.DraftRuns) && IsComplete(history.AuditRuns)
                && IsComplete(history.IdempotencyRuns) && IsComplete(history.Schedules), ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentMaintenanceTimelineDto>> Read(
        DocumentMaintenanceJob job, DocumentMaintenanceRunCursorDto? before, CancellationToken ct)
    {
        var route = $"{MaintenanceRoute}/{GetJobPath(job)}";
        if (before is not null)
        {
            route = AddPosition(route, "beforeOccurredAt", before.OccurredAtUtc, before.RunId);
        }

        return ReadRequiredCollections(route, DocumentsApiClientJsonContext.Default.GetDocumentMaintenanceTimelineDto,
            static timeline => IsComplete(timeline.Runs), ct);
    }

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentMaintenanceExecutionsDto>> ReadExecutions(
        DocumentMaintenanceJob job, DocumentMaintenanceExecutionCursorDto? before, CancellationToken ct)
    {
        var route = $"{MaintenanceRoute}/{GetJobPath(job)}/executions";
        if (before is not null)
        {
            route = AddPosition(route, "beforeStartedAt", before.StartedAtUtc, before.RunId);
        }

        return ReadRequiredCollections(route, DocumentsApiClientJsonContext.Default.GetDocumentMaintenanceExecutionsDto,
            static page => IsComplete(page.Executions), ct);
    }

    private async Task<HttpOutcome<T>> ReadRequiredCollections<T>(string route, JsonTypeInfo<T> typeInfo, Func<T, bool> isComplete, CancellationToken ct)
        where T : class
    {
        var outcome = await HttpOutcomeMapper.Send(httpClient, new HttpRequestMessage(HttpMethod.Get, new Uri(route, UriKind.Relative)),
            HttpStatusCode.OK, typeInfo, DocumentsApiClientJsonContext.Default.ContractValidationProblemDto, ct).ConfigureAwait(false);
        if (outcome.Kind != HttpOutcomeKind.Succeeded)
        {
            return outcome;
        }

        return outcome.Value is { } value && isComplete(value)
            ? outcome
            : HttpOutcomeMapper.Status<T>(HttpOutcomeKind.MalformedBody, HttpStatusCode.OK, "Maintenance response contained a missing or invalid collection.");
    }

    private static bool IsComplete<T>(IReadOnlyList<T>? items) where T : class => items is not null && items.All(static item => item is not null);

    private static string GetJobPath(DocumentMaintenanceJob job) => job switch
    {
        DocumentMaintenanceJob.Drafts => "drafts",
        DocumentMaintenanceJob.Audit => "audit",
        DocumentMaintenanceJob.Idempotency => "idempotency",
        _ => throw new ArgumentOutOfRangeException(nameof(job), job, null),
    };

    private static string AddPosition(string route, string timestampParameter, DateTimeOffset timestamp, Guid runId)
    {
        var formatted = Uri.EscapeDataString(timestamp.ToString("O", CultureInfo.InvariantCulture));
        return $"{route}?{timestampParameter}={formatted}&beforeRunId={runId:D}";
    }
}
