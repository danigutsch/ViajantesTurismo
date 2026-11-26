using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiServiceTests;

/// <summary>Captures the requested timeline position and supplies a counts-only page.</summary>
internal sealed class StubDocumentMaintenanceTimelineQuery(GetDocumentMaintenanceTimelineDto result) : IDocumentMaintenanceTimelineQuery
{
    /// <summary>Gets the job, size and continuation read through the configured endpoint.</summary>
    internal (DocumentMaintenanceJob Job, int PageSize, DocumentMaintenanceRunCursorDto? Before)? Request
    {
        get;
        private set;
    }

    /// <inheritdoc />
    public Task<GetDocumentMaintenanceTimelineDto> Read(
        DocumentMaintenanceJob job, int pageSize, DocumentMaintenanceRunCursorDto? before, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Request = (job, pageSize, before);
        return Task.FromResult(result);
    }
}
