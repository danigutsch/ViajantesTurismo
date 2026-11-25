namespace ViajantesTurismo.Admin.Application.Documents.Maintenance;

/// <summary>Defines the operator actions for an allowlisted document maintenance job.</summary>
public enum DocumentMaintenanceControlAction
{
    /// <summary>Pauses scheduled execution.</summary>
    Pause,

    /// <summary>Resumes scheduled execution.</summary>
    Resume,

    /// <summary>Requests an additional execution.</summary>
    Trigger,

    /// <summary>Requests another attempt of a failed execution.</summary>
    Retry,
}
