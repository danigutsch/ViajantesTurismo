using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiServiceTests;

/// <summary>Captures the exact scheduler query and supplies one execution page.</summary>
internal sealed class StubDocumentMaintenanceExecutionsQuery(GetDocumentMaintenanceExecutionsDto page) : IDocumentMaintenanceExecutionsQuery
{
    /// <summary>Gets the most recent request passed by the route.</summary>
    internal (DocumentMaintenanceJob Job, int PageSize, DocumentMaintenanceExecutionCursorDto? Before)? Request
    {
        get;
        private set;
    }

    /// <inheritdoc />
    public Task<GetDocumentMaintenanceExecutionsDto> ReadExecutions(
        DocumentMaintenanceJob job, int pageSize, DocumentMaintenanceExecutionCursorDto? before, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Request = (job, pageSize, before);
        return Task.FromResult(page);
    }
}
