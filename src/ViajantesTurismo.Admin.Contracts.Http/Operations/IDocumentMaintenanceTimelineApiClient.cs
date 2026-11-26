using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Reads the retained timeline for one document maintenance worker.</summary>
public interface IDocumentMaintenanceTimelineApiClient
{
    /// <summary>Reads one page of metadata through the authenticated Admin API.</summary>
    /// <param name="job">The maintenance worker whose history to read.</param>
    /// <param name="before">The optional position for earlier runs.</param>
    /// <param name="ct">A token that can cancel the request.</param>
    /// <returns>The authenticated response.</returns>
    Task<HttpOutcome<GetDocumentMaintenanceTimelineDto>> Read(DocumentMaintenanceJob job, DocumentMaintenanceRunCursorDto? before, CancellationToken ct);
}
