namespace ViajantesTurismo.Management.Web;

/// <summary>Identifies the Management roles for viewing and controlling document maintenance.</summary>
internal static class DocumentMaintenanceAccess
{
    /// <summary>The trusted role allowed to confirm schedule changes.</summary>
    internal const string ControlRole = "OperationsScheduleController";

    /// <summary>The trusted roles allowed to browse maintenance history and schedules.</summary>
    internal const string ReadRoles = "Admin," + ControlRole;
}
