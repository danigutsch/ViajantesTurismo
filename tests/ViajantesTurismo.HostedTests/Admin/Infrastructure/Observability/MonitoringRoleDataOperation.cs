namespace ViajantesTurismo.Admin.IntegrationTests.Observability;

/// <summary>Data-changing statements the monitoring role must be denied.</summary>
internal enum MonitoringRoleDataOperation
{
    /// <summary>Insert a row into the sample table.</summary>
    Insert,

    /// <summary>Update a row in the sample table.</summary>
    Update,

    /// <summary>Delete a row from the sample table.</summary>
    Delete,

    /// <summary>Drop the sample table.</summary>
    Drop,
}
