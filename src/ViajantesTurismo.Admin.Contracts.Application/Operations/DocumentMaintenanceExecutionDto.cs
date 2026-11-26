namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Operator-visible metadata for a durable maintenance execution.</summary>
/// <param name="RunId">The opaque execution identifier.</param>
/// <param name="StartedAtUtc">When the scheduler observed the execution start.</param>
/// <param name="ScheduledForUtc">The intended fire time when known.</param>
/// <param name="FinishedAtUtc">When the execution recorded a completion, if any.</param>
/// <param name="RetryAtUtc">The time associated with a separate retry request, if present.</param>
/// <param name="Status">The state supported by the current execution evidence.</param>
public sealed record DocumentMaintenanceExecutionDto(
    Guid RunId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? ScheduledForUtc,
    DateTimeOffset? FinishedAtUtc,
    DateTimeOffset? RetryAtUtc,
    DocumentMaintenanceExecutionStatus Status);
