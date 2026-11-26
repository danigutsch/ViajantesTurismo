namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Reports the persisted scheduling and execution observations for one document maintenance job.</summary>
/// <param name="Job">The application job.</param>
/// <param name="IsPaused">Whether acquisition is paused, or null before the job is registered.</param>
/// <param name="NextDueAtUtc">The next recurring due time when one is scheduled.</param>
/// <param name="FixedDelay">The delay after a recurring execution completes.</param>
/// <param name="ActiveRun">The execution observed with a live lease, if any.</param>
/// <param name="LatestRun">The most recently started execution, if any.</param>
public sealed record DocumentMaintenanceScheduleDto(
    DocumentMaintenanceJob Job,
    bool? IsPaused,
    DateTimeOffset? NextDueAtUtc,
    TimeSpan? FixedDelay,
    DocumentMaintenanceExecutionDto? ActiveRun,
    DocumentMaintenanceExecutionDto? LatestRun)
{
    /// <summary>Gets whether the recurring due time was reached when read without a pause or live lease.</summary>
    public bool IsDue
    {
        get;
        init;
    }
}
