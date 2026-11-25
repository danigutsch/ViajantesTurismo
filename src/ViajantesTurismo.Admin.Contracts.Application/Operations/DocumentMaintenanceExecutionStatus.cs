using System.Text.Json.Serialization;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Describes the durable evidence available for an execution.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<DocumentMaintenanceExecutionStatus>))]
public enum DocumentMaintenanceExecutionStatus
{
    /// <summary>The scheduler has neither a current lease nor a recorded outcome.</summary>
    Unknown,

    /// <summary>The scheduler observed a live lease.</summary>
    Running,

    /// <summary>The execution completed successfully.</summary>
    Succeeded,

    /// <summary>The execution recorded cancellation.</summary>
    Cancelled,

    /// <summary>The execution failed and can be retried.</summary>
    Failed,

    /// <summary>The scheduler recorded a separate retry request.</summary>
    RetryScheduled,

    /// <summary>The execution failed permanently.</summary>
    TerminalFailure,
}
