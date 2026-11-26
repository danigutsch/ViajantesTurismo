using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Submits confirmed document maintenance controls through the authenticated Admin API.</summary>
public interface IDocumentMaintenanceControlApiClient
{
    /// <summary>Applies a confirmed request to a document maintenance job.</summary>
    /// <param name="job">The target job.</param>
    /// <param name="control">The confirmed action.</param>
    /// <param name="request">The stable request identifier, private reason, and retry target when applicable.</param>
    /// <param name="ct">Cancels the caller's request.</param>
    /// <returns>The Admin API outcome.</returns>
    Task<HttpOutcome> Apply(DocumentMaintenanceJob job, DocumentMaintenanceControlKind control, CreateDocumentMaintenanceControlDto request, CancellationToken ct);
}
