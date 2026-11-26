namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Identifies a confirmed control accepted by the Admin document maintenance API.</summary>
public enum DocumentMaintenanceControlKind
{
    /// <summary>Stops new executions from being acquired.</summary>
    Pause,

    /// <summary>Allows executions to be acquired again.</summary>
    Resume,

    /// <summary>Queues a manual execution.</summary>
    Trigger,

    /// <summary>Queues another attempt of a specific failed execution.</summary>
    Retry,
}
