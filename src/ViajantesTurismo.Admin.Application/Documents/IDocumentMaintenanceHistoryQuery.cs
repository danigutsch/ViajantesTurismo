using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Reads counts-only evidence from the document retention jobs.</summary>
public interface IDocumentMaintenanceHistoryQuery
{
    /// <summary>Reads a bounded timeline for each retention job.</summary>
    /// <param name="ct">A token that can cancel the query.</param>
    /// <returns>The recent completion evidence and the configured polling delay.</returns>
    Task<DocumentMaintenanceHistoryDto> Read(CancellationToken ct);
}
