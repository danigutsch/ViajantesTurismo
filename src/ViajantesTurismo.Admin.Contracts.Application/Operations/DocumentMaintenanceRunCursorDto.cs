namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>The position of the last run returned for a selected maintenance job.</summary>
/// <param name="OccurredAtUtc">The run's recorded timestamp.</param>
/// <param name="RunId">The retained identifier used to break ties between equal timestamps.</param>
public sealed record DocumentMaintenanceRunCursorDto(DateTimeOffset OccurredAtUtc, Guid RunId);
