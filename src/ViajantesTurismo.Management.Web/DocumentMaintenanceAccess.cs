namespace ViajantesTurismo.Management.Web;

/// <summary>Identifies the Management roles for viewing and controlling document maintenance.</summary>
internal static class DocumentMaintenanceAccess
{
    /// <summary>The trusted administrator role for document maintenance.</summary>
    internal const string AdminRole = "Admin";

    /// <summary>The trusted role for delegated schedule controls.</summary>
    internal const string ControlRole = "OperationsScheduleController";

    /// <summary>The trusted roles allowed to browse maintenance history and schedules.</summary>
    internal const string ReadRoles = AdminRole + "," + ControlRole;
}
