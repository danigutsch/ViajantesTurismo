using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Management.Web.Components.Pages.Operations;

/// <summary>Provides the operator-facing labels for document maintenance jobs and durable state.</summary>
internal static class DocumentMaintenanceScheduleDisplay
{
    /// <summary>Returns the operator-facing label for one maintenance job.</summary>
    internal static string JobTitle(DocumentMaintenanceJob job) => job switch
    {
        DocumentMaintenanceJob.Drafts => "Draft retention",
        DocumentMaintenanceJob.Audit => "Audit retention",
        _ => "Idempotency retention",
    };

    /// <summary>Describes the durable schedule registration and pause state.</summary>
    internal static string ScheduleState(DocumentMaintenanceScheduleDto schedule) => schedule.IsPaused switch
    {
        true => "Paused",
        false => "Scheduled",
        null => "Registration pending",
    };
}
