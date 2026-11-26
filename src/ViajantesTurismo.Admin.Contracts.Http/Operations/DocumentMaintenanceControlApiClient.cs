using System.Net;
using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Sends confirmed scheduler controls to the Admin API.</summary>
/// <param name="httpClient">The configured Admin API client.</param>
public sealed class DocumentMaintenanceControlApiClient(HttpClient httpClient) : IDocumentMaintenanceControlApiClient
{
    /// <inheritdoc />
    public Task<HttpOutcome> Apply(DocumentMaintenanceJob job, DocumentMaintenanceControlKind control, CreateDocumentMaintenanceControlDto request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var jobPath = job switch
        {
            DocumentMaintenanceJob.Drafts => "drafts",
            DocumentMaintenanceJob.Audit => "audit",
            DocumentMaintenanceJob.Idempotency => "idempotency",
            _ => throw new ArgumentOutOfRangeException(nameof(job), job, null),
        };
        var controlPath = control switch
        {
            DocumentMaintenanceControlKind.Pause => "pause",
            DocumentMaintenanceControlKind.Resume => "resume",
            DocumentMaintenanceControlKind.Trigger => "trigger",
            DocumentMaintenanceControlKind.Retry => "retry",
            _ => throw new ArgumentOutOfRangeException(nameof(control), control, null),
        };
        var json = DocumentMaintenanceControlApiClientJsonContext.Default;
        var message = new HttpRequestMessage(HttpMethod.Post,
            new Uri($"/api/v1/operations/document-maintenance/{jobPath}/{controlPath}", UriKind.Relative))
        {
            Content = JsonContent.Create(request, json.CreateDocumentMaintenanceControlDto),
        };
        return HttpOutcomeMapper.Send(httpClient, message, HttpStatusCode.NoContent, json.ContractValidationProblemDto, ct);
    }
}
