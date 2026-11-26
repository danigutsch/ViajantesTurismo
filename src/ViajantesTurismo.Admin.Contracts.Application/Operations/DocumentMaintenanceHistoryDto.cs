namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Durable schedules and recent counts-only evidence from the document retention jobs.</summary>
/// <param name="DraftRuns">Recent draft retention batches, newest first.</param>
/// <param name="AuditRuns">Recent audit retention batches, newest first.</param>
/// <param name="IdempotencyRuns">Recent idempotency retention batches, newest first.</param>
/// <param name="Schedules">The application maintenance jobs and their persisted schedule observations.</param>
public sealed record DocumentMaintenanceHistoryDto(
    IReadOnlyList<DocumentMaintenanceRunDto> DraftRuns,
    IReadOnlyList<DocumentMaintenanceRunDto> AuditRuns,
    IReadOnlyList<DocumentMaintenanceRunDto> IdempotencyRuns,
    IReadOnlyList<DocumentMaintenanceScheduleDto> Schedules);
