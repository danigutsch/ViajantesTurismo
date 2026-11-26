using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Reads the durable execution history for a document maintenance job.</summary>
public interface IDocumentMaintenanceExecutionsApiClient
{
    /// <summary>Reads a bounded execution page through the authenticated Admin API.</summary>
    /// <param name="job">The selected maintenance job.</param>
    /// <param name="before">The exclusive continuation for older executions.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The authenticated response.</returns>
    Task<HttpOutcome<GetDocumentMaintenanceExecutionsDto>> ReadExecutions(
        DocumentMaintenanceJob job, DocumentMaintenanceExecutionCursorDto? before, CancellationToken ct);
}
