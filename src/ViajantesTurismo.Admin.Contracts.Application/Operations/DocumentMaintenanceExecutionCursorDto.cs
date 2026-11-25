namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>The exclusive continuation for a page of scheduler executions.</summary>
/// <param name="StartedAtUtc">The last execution's recorded start.</param>
/// <param name="RunId">The last execution's opaque identifier.</param>
public sealed record DocumentMaintenanceExecutionCursorDto(DateTimeOffset StartedAtUtc, Guid RunId);
