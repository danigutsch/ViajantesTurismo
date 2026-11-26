namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>A bounded page of metadata-only scheduler executions for one job.</summary>
/// <param name="Executions">The observed executions ordered newest first.</param>
/// <param name="Next">The continuation for older executions, when available.</param>
public sealed record GetDocumentMaintenanceExecutionsDto(
    IReadOnlyList<DocumentMaintenanceExecutionDto> Executions,
    DocumentMaintenanceExecutionCursorDto? Next);
