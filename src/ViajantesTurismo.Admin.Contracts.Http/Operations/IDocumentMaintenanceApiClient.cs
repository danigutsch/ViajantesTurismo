using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Reads the Admin operations history of the document retention workers.</summary>
public interface IDocumentMaintenanceApiClient
{
    /// <summary>Reads bounded, counts-only evidence from the Admin API.</summary>
    /// <param name="ct">A token that can cancel the request.</param>
    /// <returns>The authenticated response.</returns>
    Task<HttpOutcome<DocumentMaintenanceHistoryDto>> Read(CancellationToken ct);
}
