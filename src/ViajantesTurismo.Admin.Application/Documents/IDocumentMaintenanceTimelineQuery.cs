using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Reads retained, counts-only batch history for a single maintenance job.</summary>
public interface IDocumentMaintenanceTimelineQuery
{
    /// <summary>Reads a bounded page of a job's stored batch results.</summary>
    /// <param name="job">The maintenance job to inspect.</param>
    /// <param name="pageSize">The requested page size.</param>
    /// <param name="before">The previous page's continuation, if any.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The recorded results and optional continuation.</returns>
    Task<GetDocumentMaintenanceTimelineDto> Read(DocumentMaintenanceJob job, int pageSize, DocumentMaintenanceRunCursorDto? before, CancellationToken ct);
}
