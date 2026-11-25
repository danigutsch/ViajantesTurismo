using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Reads durable execution history for an app-owned schedule.</summary>
public interface IDocumentMaintenanceExecutionsQuery
{
    /// <summary>Reads a bounded page of observed executions for a maintenance job.</summary>
    /// <param name="job">The maintenance job to inspect.</param>
    /// <param name="pageSize">The requested page size.</param>
    /// <param name="before">The preceding page's exclusive continuation.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The observed executions and their optional continuation.</returns>
    Task<GetDocumentMaintenanceExecutionsDto> ReadExecutions(
        DocumentMaintenanceJob job, int pageSize, DocumentMaintenanceExecutionCursorDto? before, CancellationToken ct);
}
