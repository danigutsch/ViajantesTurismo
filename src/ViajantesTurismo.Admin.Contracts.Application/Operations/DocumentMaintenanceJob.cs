using System.Text.Json.Serialization;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Identifies an application-owned document maintenance job.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<DocumentMaintenanceJob>))]
public enum DocumentMaintenanceJob
{
    /// <summary>Draft document retention.</summary>
    Drafts,

    /// <summary>Document audit retention.</summary>
    Audit,

    /// <summary>Document idempotency retention.</summary>
    Idempotency,
}
